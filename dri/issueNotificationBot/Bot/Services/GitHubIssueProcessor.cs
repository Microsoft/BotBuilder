// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using IssueNotificationBot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IssueNotificationBot.Services
{
    public class GitHubIssueProcessor : GitHubDataProcessor
    {
        public GitHubIssueProcessor(UserStorage userStorage, NotificationHelper notificationHelper, IConfiguration configuration, ILogger<GitHubIssueProcessor> logger)
            : base(userStorage, notificationHelper, configuration, logger)
        { }

        public async Task ProcessIssues(GitHubIssues issues)
        {
            Logger.LogInformation("Processing data");
            TrackedUsers = await UserStorage.GetGitHubUsers();

            await ProcessReportedNotReplied(issues.ReportedNotReplied);
        }

        private async Task ProcessReportedNotReplied(GitHubIssue[] issues)
        {
            Logger.LogInformation("Processing ProcessReportedNotReplied");
            foreach (var issue in issues)
            {
                foreach (var assignee in GetAssigneeUserData(issue))
                {
                    await NotifyAssigneeAsNecessary(assignee, issue);
                }
            }
        }

        private List<TrackedUser> GetAssigneeUserData(GitHubIssue issue)
        {
            var users = new List<TrackedUser>();
            foreach (var assignee in issue.Assignees)
            {
                if (TrackedUsers.TryGetValue(assignee.Login, out TrackedUser user))
                {
                    users.Add(user);
                }
            }
            return users;
        }

        private async Task NotifyAssigneeAsNecessary(TrackedUser user, GitHubIssue issue)
        {
            var now = DateTime.UtcNow;
            if (user.NotificationSettings.AllEnabled)
            {
                // Check each time period from largest to smallest
                foreach (TimePeriodNotification timePeriod in user.NotificationSettings.TimePeriodNotifications.OrderByDescending(item => item.ExpireHours).ToList())
                {
                    // Stop checking if we've already sent the notification
                    if (UserNotifiedWithinWindow(timePeriod, now, issue, user.TeamsUserInfo.Id))
                    {
                        return;
                    }

                    // Adjust the message we send to the user
                    var action = "Respond";
                    if (IssueExpiredNeedsResolve(timePeriod))
                    {
                        action = "Resolve";
                    }

                    string nearingOrExpiredMessage = null;
                    var expires = GetExpiration(issue, timePeriod, now);

                    // TESTING ONLY: This allows us to set up a separate repository and some issues to test that the bot works.
                    // Here, we can manually set the issue created time to mock an expired issue.
                    if (Configuration?["EnableTestMode"] == "true" && issue.Repository.Name == Constants.TestRepo)
                    {
                        expires = new DateTime(2020, 1, 1);
                    }

                    // Adjust some of the card text based on whether the issue has expired, or is only nearing expiration.
                    if (IssueExpiredNeedsResponse(expires, now))
                    {
                        nearingOrExpiredMessage = Constants.PassedExpirationMessage;
                    }
                    else if (IssueNearingExpirationNeedsResponse(timePeriod, expires, now))
                    {
                        nearingOrExpiredMessage = Constants.NearingExpirationMessage;
                    }

                    if (!string.IsNullOrEmpty(nearingOrExpiredMessage))
                    {
                        await NotificationHelper.SendIssueNotificationToUserAsync(user, issue, nearingOrExpiredMessage, expires, action);
                    }
                }
            }
        }

        private static bool IssueExpiredNeedsResolve(TimePeriodNotification timePeriod)
        {
            return timePeriod.ExpireHours > Constants.IssueNeedsResolutionHours;
        }

        private static bool IssueExpiredNeedsResponse(DateTime expires, DateTime now)
        {
            return now >= expires;
        }

        private static bool IssueNearingExpirationNeedsResponse(TimePeriodNotification timePeriod, DateTime expires, DateTime now)
        {
            return now >= expires.AddHours(-timePeriod.NotifyPriorToExpiryHours);
        }

        private bool UserNotifiedWithinWindow(TimePeriodNotification timePeriod, DateTime now, GitHubIssue issue, string teamsUserId)
        {
            var repoAndIssueNumber = NotificationHelper.GetRepoIssueNumberString(issue);
            var mappedUser = NotificationHelper.GetMappedActivityFromIssueAndUser(repoAndIssueNumber, teamsUserId);
            if (mappedUser != null)
            {
                return mappedUser.SentAt.AddHours(timePeriod.NotificationFrequency) >= now;
            }
            return false;
        }
    }
}
