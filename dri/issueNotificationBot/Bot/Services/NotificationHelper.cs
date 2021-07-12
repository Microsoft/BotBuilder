// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using IssueNotificationBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace IssueNotificationBot.Services
{
    public class NotificationHelper
    {
        /// <summary>
        /// Dictionary of activities, keyed by <Repo>-<IssueNumber> for tracking when we send a notification for an issue, per user.
        /// </summary>
        public readonly ConcurrentDictionary<string, MappedIssue> IssueActivityMap = new ConcurrentDictionary<string, MappedIssue>();
        /// <summary>
        /// Determines whether or not to notify the Maintainer of bot errors. Can be changed via <see cref="MaintainerCommands"/>.
        /// </summary>
        public bool NotifyMaintainer = true;
        private readonly IBotFrameworkHttpAdapter Adapter;
        private readonly IConfiguration Configuration;
        private readonly ILogger Logger;
        private readonly UserStorage UserStorage;
        private TrackedUser _Maintainer;

        public TrackedUser Maintainer
        {
            get
            {
                return _Maintainer ??= (UserStorage.GetTrackedUserFromGitHubUserId(Constants.MaintainerGitHubId)).GetAwaiter().GetResult();
            }
        }

        public NotificationHelper(IBotFrameworkHttpAdapter adapter, IConfiguration configuration, ILogger<NotificationHelper> logger, UserStorage userStorage)
        {
            Adapter = adapter;
            Configuration = configuration;
            Logger = logger;
            UserStorage = userStorage;

            // If the channel is the Emulator, and authentication is not in use,
            // the AppId will be null.  We generate a random AppId for this case only.
            // This is not required for production, since the AppId will have a value.
            if (string.IsNullOrEmpty(Configuration["MicrosoftAppId"]))
            {
                Configuration["MicrosoftAppId"] = Guid.NewGuid().ToString(); // Ff no AppId, use a random GUID.
            }

            // Notify the maintainer of this bot of any errors via Teams.
            // We need to do this here and not in AdapterWithErrorHandler to avoid circular dependencies.
            var originalOnTurnError = (adapter as AdapterWithErrorHandler)?.OnTurnError;
            (adapter as AdapterWithErrorHandler)!.OnTurnError = async (turnContext, exception) =>
            {
                if (NotifyMaintainer)
                {
                    if (Maintainer != null && turnContext.Activity.From?.Name != Maintainer.TeamsUserInfo.Name)
                    {
                        var errorMessage = $"Error occurred for {turnContext?.Activity?.From?.Name}:\n{exception.Message}\n{exception.StackTrace}\n{turnContext?.Activity}";
                        Logger.LogError(errorMessage);
                        await SendProactiveNotificationToUserAsync(Maintainer, MessageFactory.Text(errorMessage));
                    }

                    await turnContext.SendActivityAsync("I've notified the maintainer of this bot about this error.");
                }
                else
                {
                    await originalOnTurnError(turnContext, exception);
                }
            };
        }

        public async Task GreetNewTeamMember(ChannelAccount member, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation($"Greeting new member: { member.Name }");

            var conversationReference = turnContext.Activity.GetConversationReference();
            conversationReference.User = member;

            // TODO: Eventually, it would be nice to begin the SignInDialog here, proactively.
            // However, I believe the user has to have sent a message, first, before the OAuthPrompt can be sent, otherwise results in a 403.
            await CreatePersonalConversationAsync(conversationReference, async (turnContext2, cancellationToken2) =>
            {
                var activity = MessageFactory.Text($"Hello! I am {Constants.UserAgent} and I can notify you about your GitHub issues and PRs in the Bot Framework repositories that are about to \"expire\".\n" +
                        "An \"expired\" issue is one with the `customer-reported` tag, and is nearing or past:\n" +
                        "* 72 hours with no `customer-replied` tag\n" +
                        "* 30 days and still open\n" +
                        "* 90 days and still open\n\n" +
                        "To get started, type \"login\" so that I can get your GitHub information.");
                activity.TeamsNotifyUser();
                await turnContext2.SendActivityAsync(activity, cancellationToken2);
            }, cancellationToken);
        }

        /// <summary>
        /// Helper for sending proactive messages to a user.
        /// </summary>
        /// <returns>activityId of the message sent.</returns>
        public async Task<ResourceResponse> SendProactiveNotificationToUserAsync(TrackedUser user, IActivity activity, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<ResourceResponse>();
            await CreatePersonalConversationAsync(user.ConversationReference, async (turnContext, cancellationToken2) =>
            {
                var activityId = await turnContext.SendActivityAsync(activity, cancellationToken2);
                tcs.SetResult(activityId);
            }, cancellationToken);

            return tcs.Task.GetAwaiter().GetResult();
        }

        public async Task SendIssueNotificationToUserAsync(TrackedUser user, GitHubIssue issue, string nearingOrExpiredMessage, DateTime expires, string action, CancellationToken cancellationToken = default)
        {
            Logger.LogInformation($"Sending Issue notification to {user.TeamsUserInfo.Name} for {issue.Number}");

            var card = TemplateCardHelper.GetPersonalIssueCard(issue, nearingOrExpiredMessage, expires, action, Maintainer);

            var activity = MessageFactory.Attachment(card);
            activity.TeamsNotifyUser();

            var activityId = await SendProactiveNotificationToUserAsync(user, activity, cancellationToken);

            // Store information about the activity so that we can avoid sending duplicate notifications within time frames specified by TimePeriodNotificaiton.
            var repoAndIssueNumber = GetRepoIssueNumberString(issue);
            StoreIssueCardActivityId(activityId.Id, repoAndIssueNumber, user.TeamsUserInfo.Id);
        }

        public async Task SendPRNotificationToUserAsync(TrackedUser user, PRCardTemplate prs, CancellationToken cancellationToken = default)
        {
            Logger.LogInformation($"Sending PR notification to {user.TeamsUserInfo.Name} with {prs.SinglePRs.Count} single and {prs.GroupPRs.Count} group");

            var card = TemplateCardHelper.GetPersonalPRCard(prs, Maintainer);

            var activity = MessageFactory.Attachment(card);
            activity.TeamsNotifyUser();

            await SendProactiveNotificationToUserAsync(user, activity, cancellationToken);
        }

        /// <summary>
        /// We store when we send notifications for each user as well as an array of all users we sent it to.
        /// This checks to see if we've sent a notification for this issue and returns the user that sent it, or null if they did not.
        /// </summary>
        /// <param name="repoAndIssueNumber">String with format: RepoName-IssueNumber</param>
        /// <returns>MappedActivityUser, which stores data about when the user was sent a notification for the issue.</returns>
        public MappedActivityUser GetMappedActivityFromIssueAndUser(string repoAndIssueNumber, string teamsUserId)
        {
            var mappedIssue = GetMappedIssue(repoAndIssueNumber);
            if (mappedIssue != null && mappedIssue.Users.TryGetValue(teamsUserId, out MappedActivityUser mappedUser))
            {
                return mappedUser;
            }
            return null;
        }

        /// <summary>
        /// Helper for generating the key needed to look into MappedIssues
        /// </summary>
        public static string GetRepoIssueNumberString(GitHubIssue issue)
        {
            return $"{issue.Repository.Name}-{issue.Number}";
        }

        private async Task CreatePersonalConversationAsync(ConversationReference conversationReference, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            Logger.LogInformation($"Creating personal conversation for {conversationReference.User.Name}");

            var serviceUrl = conversationReference.ServiceUrl;
            var credentials = new MicrosoftAppCredentials(Configuration["MicrosoftAppId"], Configuration["MicrosoftAppPassword"]);

            var conversationParameters = new ConversationParameters
            {
                IsGroup = false,
                Members = new ChannelAccount[]
                {
                        conversationReference.User
                },
                TenantId = conversationReference.Conversation.TenantId,
                Bot = conversationReference.Bot
            };

            AppCredentials.TrustServiceUrl(serviceUrl);

            await ((BotFrameworkAdapter)Adapter).CreateConversationAsync(
                Channels.Msteams,
                serviceUrl,
                credentials,
                conversationParameters,
                async (turnContext1, cancellationToken1) =>
                {
                    Logger.LogInformation($"Continuing conversation for {conversationReference.User.Name}");

                    var conversationReference2 = turnContext1.Activity.GetConversationReference();
                    conversationReference2.User = conversationReference.User;

                    try
                    {
                        await ((BotFrameworkAdapter)Adapter).ContinueConversationAsync(
                            Configuration["MicrosoftAppId"],
                            conversationReference2,
                            async (turnContext2, cancellationToken2) => await callback(turnContext2, cancellationToken2),
                            cancellationToken1);
                    }
                    catch (ErrorResponseException e)
                    {
                        // Catch 403 error. They indicate the user has blocked the bot, so we'll also remove them from persistent storage.
                        if (e.Message.Contains("Forbidden"))
                        {
                            await UserStorage.RemoveUser(conversationReference.User.Id);
                            var errorMessage = $"{conversationReference.User.Name} has blocked the bot. Removing from persistent storage.";
                            Logger.LogError(errorMessage);
                            await SendProactiveNotificationToUserAsync(Maintainer, MessageFactory.Text(errorMessage));
                        }

                        throw;
                    }
                },
                cancellationToken);
        }

        private void StoreIssueCardActivityId(string activityId, string repoAndIssueNumber, string teamsUserId)
        {
            Logger.LogInformation($"Storing IssueCard {repoAndIssueNumber} for activityId {activityId}");
            var newMappedActivity = new MappedIssue(activityId, teamsUserId);
            IssueActivityMap.AddOrUpdate(repoAndIssueNumber, newMappedActivity, (_, oldValue) =>
            {
                oldValue.Users[teamsUserId] = new MappedActivityUser(activityId);
                return oldValue;
            });
        }

        private MappedIssue GetMappedIssue(string repoAndIssueNumber)
        {
            if (IssueActivityMap.TryGetValue(repoAndIssueNumber, out MappedIssue mappedIssue))
            {
                return mappedIssue;
            }
            return null;
        }
    }

    public class MappedIssue
    {
        public readonly ConcurrentDictionary<string, MappedActivityUser> Users = new ConcurrentDictionary<string, MappedActivityUser>();

        public MappedIssue(string activityId, string teamsUserId)
        {
            Users[teamsUserId] = new MappedActivityUser(activityId);
        }
    }

    public class MappedActivityUser
    {
        public bool Hidden;
        public readonly string ActivityId;
        public readonly DateTime SentAt = DateTime.Now;

        public MappedActivityUser(string activityId)
        {
            ActivityId = activityId;
        }
    }
}
