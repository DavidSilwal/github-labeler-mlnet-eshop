﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Octokit;

namespace GitHubLabeler
{
    internal class Labeler
    {
        private readonly GitHubClient _client;
        private readonly string _repoOwner;
        private readonly string _repoName;

        public Labeler(string repoOwner, string repoName, string accessToken)
        {
            _repoOwner = repoOwner;
            _repoName = repoName;
            var productInformation = new ProductHeaderValue("MLGitHubLabeler");
            _client = new GitHubClient(productInformation)
            {
                Credentials = new Credentials(accessToken)
            };
        }

        // Label all issues that are not labeled yet
        public async Task LabelAllNewIssues()
        {
            var newIssues = await GetNewIssues();
            foreach (var issue in newIssues.Where(issue => !issue.Labels.Any()))
            {
                var label = await PredictLabel(issue);
                ApplyLabel(issue, label);
            }
        }

        private async Task<IReadOnlyList<Issue>> GetNewIssues()
        {
            var issueRequest = new RepositoryIssueRequest
            {
                State = ItemStateFilter.Open,
                Filter = IssueFilter.All,                
                Since = DateTime.Now.AddDays(-7)
            };

            var allIssues = await _client.Issue.GetAllForRepository(_repoOwner, _repoName, issueRequest);

            //var allIssues = await _client.Issue.GetAllForRepository(27453006,
            //                                                        issueRequest);

            //var allIssues = await _client.Issue.GetAllForRepository("dotnet-architecture", "eShopOnContainers", issueRequest);

            //(CDLTLL)
            // Filter out pull requests 
            return allIssues.Where(i => !i.HtmlUrl.Contains("/pull/"))
                            .ToList();
        }

        private async Task<string> PredictLabel(Issue issue)
        {
            var corefxIssue = new GitHubIssue
            {
                ID = issue.Number.ToString(),
                Title = issue.Title,
                Description = issue.Body
            };

            var predictedLabel = await Predictor.PredictAsync(corefxIssue);

            return predictedLabel;
        }

    private void ApplyLabel(Issue issue, string label)
        {
            var issueUpdate = new IssueUpdate();
            issueUpdate.AddLabel(label);
            
            _client.Issue.Update(_repoOwner, _repoName, issue.Number, issueUpdate);

            Console.WriteLine($"Issue {issue.Number} : \"{issue.Title}\" \t was labeled as: {label}");
        }
    }
}