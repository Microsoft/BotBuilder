// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace IssueNotificationBot
{
    public static class Constants
    {
        /// <summary>
        /// The GitHub ID of the maintainer of this bot. Used for handling <see cref="MaintainerCommands"/> and
        /// adding contact information to some messages sent by the bot.
        /// </summary>
        public const string MaintainerGitHubId = "mdrichardson";

        public const string GitHubUserStorageKey = "GitHubUsers";
        public const string TeamsIdToGitHubUserMapStorageKey = "TeamsIdToGitHubUser";

        public const string GitHubApiBaseUrl = "https://api.github.com";
        public const string GitHubApiAuthenticatedUserPath = "/user";
        public const string UserAgent = "IssueNotificationBot";

        /// <summary>
        /// SLA hours for when an issue must be closed.
        /// </summary>
        public const int IssueNeedsResolutionHours = 24 * 30;

        public const bool ExcludeWeekendsFromExpireHours = true;

        public const string PassedExpirationMessage = "has passed";
        public const string NearingExpirationMessage = "is nearing";

        public const string NoActivityId = "NoActivityId";
        public const string PersonalConversationType = "personal";

        public const string HideIssueNotificationAction = "hideIssueNotification";

        public const string NoConversationResponse = "Sorry. I'm mostly a notification-only bot don't know how to respond to this.";
        public const string NoOAuthInGroupConversationsResponse = "Please sign in via the 1:1 conversation that I sent you previously. If you need me to send it again, please type \"login\" **in our 1:1 conversation**";

        public const string LoginCommand = "login";

        /// <summary>
        /// Used for testing purposes. Represents the name of the repo to check for manually-created test issues.
        /// </summary>
        public const string TestRepo = "testRepoForIssueNotificationBot";
    }

    /// <summary>
    /// All commands that can only be called by the maintainer.
    /// </summary>
    static public class MaintainerCommands
    {
        /// <summary>
        /// Maintainer commands must start with this prefix.
        /// </summary>
        public const string CommandPrefix = "command:";

        /// <summary>
        /// Has the bot send a card containing all available Maintainer Commands.
        /// </summary>
        public const string ShowCommands = CommandPrefix + "showCommands";

        /// <summary>
        /// Enable sending bot errors to the maintainer.
        /// </summary>
        public const string EnableMaintainerNotifications = CommandPrefix + "enableNotifications";

        /// <summary>
        /// Disable sending bot errors to the maintainer.
        /// </summary>
        public const string DisableMaintainerNotifications = CommandPrefix + "disableNotifications";

        /// <summary>
        /// Send examples of all Adaptive Cards to the Maintainer.
        /// </summary>
        public const string TestCards = CommandPrefix + "testCards";

        /// <summary>
        /// Resend greetings to all users on the Teams team from which this command gets sent,
        /// if we don't already have their information.
        /// </summary>
        public const string ResendGrettings = CommandPrefix + "resendGreetings";

        /// <summary>
        /// Result changes based on whatever is specified to happen in <see cref="OverwriteNotificationSettingsForAllUsers"/>.
        /// However, this should generally be used to either add a new <see cref="TimePeriodNotification"/> for all users or to set all users to default notification settings.
        /// </summary>
        public const string UpdateUserNotificationSettings = CommandPrefix + "updateNotificationSettings";

        /// <summary>
        /// Set the message to broadcast to all users whose info we have.
        /// </summary>
        public const string SetBroadcastMessage = CommandPrefix + "setBroadcastMessage";

        /// <summary>
        /// View the message to broadcast to all users whose info we have.
        /// </summary>
        public const string ViewBroadcastMessage = CommandPrefix + "viewBroadcastMessage";

        /// <summary>
        /// Send the message to broadcast to all users whose info we have.
        /// </summary>
        public const string SendBroadcastMessage = CommandPrefix + "sendBroadcastMessage";
    }
}
