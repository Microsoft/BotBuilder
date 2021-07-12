using IssueNotificationBot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IssueNotificationBot.Services
{
    public class GitHubPRProcessor : GitHubDataProcessor
    {
        public GitHubPRProcessor(UserStorage userStorage, NotificationHelper notificationHelper, IConfiguration configuration, ILogger<GitHubIssueProcessor> logger)
            : base(userStorage, notificationHelper, configuration, logger)
        { }

        public async Task ProcessPRs(Dictionary<string, GitHubPRReviewer> prs)
        {
            Logger.LogInformation("Processing data");
            TrackedUsers = await UserStorage.GetGitHubUsers();

            await SendNotificationsForStalePRs(prs);
        }

        public async Task SendNotificationsForStalePRs(Dictionary<string, GitHubPRReviewer> prs)
        {
            var now = DateTime.Now;

            foreach (var user in TrackedUsers)
            {
                // If we're tracking the user and they have PRs they're a reviewer for.
                if (prs.TryGetValue(user.Value.GitHubDetails.Login, out GitHubPRReviewer reviewer))
                {
                    var notificationSetting = user.Value.NotificationSettings.TimePeriodNotifications.Single(setting => setting.Name == "PR Notification");

                    if (!user.Value.NotificationSettings.AllEnabled || !notificationSetting.Enabled)
                    {
                        continue;
                    }

                    var cardTemplate = new PRCardTemplate
                    {
                        SinglePRs = new List<GitHubPRForCardTemplate>(),
                        GroupPRs = new List<GitHubPRForCardTemplate>(),
                        // This is an ugly URL, but links directly to a GitHub query that lists all of the user's PRs.
                        PRQueryUrl = $"https://github.com/search?q=is%3Aopen+review-requested%3A{user.Value.GitHubDetails.Login}+sort%3Acreated-asc+repo%3AMicrosoft%2Fbotbuilder-azure+repo%3AMicrosoft%2Fbotbuilder-cognitiveservices+repo%3AMicrosoft%2Fbotbuilder-dotnet+repo%3AMicrosoft%2Fbotbuilder-java+repo%3AMicrosoft%2Fbotbuilder-js+repo%3AMicrosoft%2Fbotbuilder-python+repo%3AMicrosoft%2Fbotbuilder-samples+repo%3AMicrosoft%2Fbotbuilder-tools+repo%3AMicrosoft%2Fbotbuilder-v3+repo%3AMicrosoft%2Fbotframework-emulator+repo%3AMicrosoft%2Fbotframework-directlinejs+repo%3AMicrosoft%2Fbotframework-solutions+repo%3AMicrosoft%2Fbotframework-services+repo%3AMicrosoft%2Fbotframework-sdk+repo%3AMicrosoft%2Fbotframework-composer+repo%3AMicrosoft%2Fbotframework-cli+repo%3AMicrosoft%2Fbotframework-webchat+repo%3AMicrosoftDocs%2Fbot-docs+is%3Apr&type=Issues"
                    };

                    // Order oldest to newest and highlight > 3 day old PRs in Adaptive Card.
                    // Highlighting is handled by the Adaptive Card, based on the pr.Highlight property.
                    foreach (var pr in reviewer.Single.OrderBy(pr => pr.CreatedAt))
                    {
                        var expires = GetExpiration(pr, notificationSetting, now);
                        if (now > expires)
                        {
                            pr.Highlight = true;
                        }

                        var prTemplate = ConvertPRtoTemplate(pr);

                        cardTemplate.SinglePRs.Add(prTemplate);
                    }
                    // Order oldest to newest and highlight Group PRs.
                    foreach (var pr in reviewer.Group.OrderBy(pr => pr.CreatedAt))
                    {
                        var expires = GetExpiration(pr, notificationSetting, now);
                        if (now > expires)
                        {
                            pr.Highlight = true;
                        }

                        var prTemplate = ConvertPRtoTemplate(pr);

                        cardTemplate.GroupPRs.Add(prTemplate);
                    }

                    // Only send PR card if user has at least one group or single.
                    if (cardTemplate.GroupPRs.Count > 0 || cardTemplate.SinglePRs.Count > 0)
                    {
                        try
                        {
                            await NotificationHelper.SendPRNotificationToUserAsync(user.Value, cardTemplate);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError($"Error sending PR card for {user.Value.TeamsUserInfo.Name}:\n{e}");
                        }
                    }
                }
            }
        }

        private static GitHubPRForCardTemplate ConvertPRtoTemplate(GitHubPR pr)
        {
            // Keep only the last part of the group name (composer vs. bf-composer).
            var group = !string.IsNullOrEmpty(pr.ReviewingForGroup) ? pr.ReviewingForGroup : "";
            const string groupReplacePattern = "(bf-)|(bb-)";
            group = Regex.Replace(group, groupReplacePattern, "", RegexOptions.IgnoreCase);

            // Keep only the last part of the repo name (composer vs. botframework-composer).
            var repo = pr.Repository.Name;
            const string repoReplacePattern = "(botframework-)|(botbuilder-)";
            repo = Regex.Replace(repo, repoReplacePattern, "", RegexOptions.IgnoreCase);

            return new GitHubPRForCardTemplate
            {
                CreatedAt = pr.CreatedAt,
                Group = group,
                Highlight = pr.Highlight,
                Repository = repo,
                Title = pr.Title,
                Url = pr.Url.ToString()
            };
        }
    }
}
