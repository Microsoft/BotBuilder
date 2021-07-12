// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;

namespace IssueNotificationBot.Models
{
    // Model for all of a user's data that gets stored in persistent storage, keyed on their GitHub username.
    public class TrackedUser
    {
        public NotificationSettings NotificationSettings = new NotificationSettings();
        public TeamsChannelAccount TeamsUserInfo;
        public GitHubUserResponse GitHubDetails;
        public ConversationReference ConversationReference;

        public TrackedUser(GitHubUserResponse gitHubUserResponse)
        {
            GitHubDetails = gitHubUserResponse;
        }
    }

    public class NotificationSettings
    {
        // Global enabling of all notifications for a user.
        public bool AllEnabled = true;

        // For setting user-defined notification settings. Currently, all users use default and cannot change.
        public TimePeriodNotification[] TimePeriodNotifications =
        {
            // Issues that expire after 3 days.
            new TimePeriodNotification(
                24 * 3,
                "72h",
                24,
                6
                ),
            // Issues that expire after 30 days.
            new TimePeriodNotification(
                24 * 30,
                "30d",
                24 * 3,
                24
                ),
            // Issues that expire after 90 days.
            new TimePeriodNotification(
                24 * 90,
                "90d",
                24 * 7,
                24 * 3
                ),
            // PRs that expire after 3 days.
            new TimePeriodNotification(
                24 * 3,
                "PR Notification",
                0,
                23
                )
        };
    }

    public class TimePeriodNotification
    {
        public string Name;
        public bool Enabled = true;
        public int ExpireHours;
        public int NotifyPriorToExpiryHours;
        public int NotificationFrequency;

        public TimePeriodNotification(int _expireHours, string name, int notifyPriorToExpiryHours, int notificationFrequencyHours, bool enabled = true)
        {
            ExpireHours = _expireHours;
            Name = name;
            NotifyPriorToExpiryHours = notifyPriorToExpiryHours;
            NotificationFrequency = notificationFrequencyHours;
            Enabled = enabled;
        }
    }
}
