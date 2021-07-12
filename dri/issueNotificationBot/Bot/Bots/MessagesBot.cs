// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using IssueNotificationBot.Models;
using IssueNotificationBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IssueNotificationBot
{
    // Handle all of the non-Teams-specific tasks, mainly message routing and Adaptive Card submissions.
    public class MessagesBot<TUserDialog, TMaintainerDialog> : TeamsActivityHandler
        where TUserDialog : Dialog
        where TMaintainerDialog : Dialog
    {
        protected readonly BotState ConversationState;
        protected readonly Dialog UserDialog;
        protected readonly Dialog MaintainerDialog;
        protected readonly ILogger Logger;
        protected readonly BotState UserState;
        protected readonly MessageBroadcaster MessageBroadcaster;
        protected readonly NotificationHelper NotificationHelper;
        protected readonly UserStorage UserStorage;

        public MessagesBot(
            ConversationState conversationState,
            UserState userState,
            TUserDialog userDialog,
            TMaintainerDialog maintainerDialog,
            ILogger<MessagesBot<TUserDialog, TMaintainerDialog>> logger,
            UserStorage userStorage,
            MessageBroadcaster messageBroadcaster,
            NotificationHelper notificationHelper)
        {
            ConversationState = conversationState;
            UserState = userState;

            UserDialog = userDialog;
            MaintainerDialog = maintainerDialog;

            Logger = logger;
            MessageBroadcaster = messageBroadcaster;
            NotificationHelper = notificationHelper;
            UserStorage = userStorage;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            turnContext.Activity.RemoveMentionText(turnContext.Activity.Recipient.Id);

            // Handle maintainer commands
            if (MessageIsFromMaintainer(turnContext))
            {
                await MaintainerDialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("MaintainerDialogState"), cancellationToken);
            }

            // Handle users that haven't provided GitHub info
            else if (await UserNeedsToLogIn(turnContext))
            {
                // We don't want to send the OAuth card to a group conversation.
                if (turnContext.Activity.Conversation.ConversationType == Constants.PersonalConversationType)
                {
                    Logger.LogInformation($"Running SignInDialog for {turnContext.Activity.From.Name}");

                    await UserDialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                }
                else
                {
                    Logger.LogInformation($"{turnContext.Activity.From.Name} sent the bot a message from a Group channel and we don't have their information.");

                    await turnContext.SendActivityAsync(Constants.NoOAuthInGroupConversationsResponse);
                }
            }

            // Check for and catch Adaptive Card Submit actions
            else if (IsAdaptiveCardSubmitAction(turnContext))
            {
                await HandleAdaptiveCardSubmitActions(turnContext, cancellationToken);
            }

            // Bot doesn't know how to handle anything else
            else
            {
                Logger.LogInformation($"Unable to handle message: {turnContext.Activity.Text} from: {turnContext.Activity.From.Name}");
                await turnContext.SendActivityAsync(Constants.NoConversationResponse);

                var maintainer = NotificationHelper.Maintainer;

                await turnContext.SendActivityAsync($"If you need help with this bot, please contact {maintainer.TeamsUserInfo.Name} ({maintainer.TeamsUserInfo.Email}).");
            }
        }

        private async Task<bool> UserNeedsToLogIn(ITurnContext<IMessageActivity> turnContext)
        {
            // User requested to login.
            var sentLoginCommand = string.Equals(turnContext.Activity.Text, Constants.LoginCommand, StringComparison.InvariantCultureIgnoreCase);

            // We need user's details to do anything, so always force them to login if we don't have their info.
            var needUserDetails = !await UserStorage.HaveUserDetails(turnContext.Activity.From.Id);

            // Maintainer may want to send the bot a command before trying to log in.
            var userIsNotMaintainer = !MessageIsFromMaintainer(turnContext);

            return sentLoginCommand || (needUserDetails && userIsNotMaintainer);
        }

        private bool MessageIsFromMaintainer(ITurnContext<IMessageActivity> turnContext)
        {
            // The TeamsInfo.Id can change between bots (like dev and prod), but AadObjectId stays the same. 
            return turnContext.Activity.From.AadObjectId == NotificationHelper.Maintainer?.TeamsUserInfo.AadObjectId;
        }

        private bool IsAdaptiveCardSubmitAction(ITurnContext<IMessageActivity> turnContext)
        {
            // It's highly likely this is an Adaptive Card Submit action if Activity.Value is populated and Activity.Text is not
            return turnContext.Activity.Value != null && string.IsNullOrEmpty(turnContext.Activity.Text);
        }

        private async Task HandleAdaptiveCardSubmitActions(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var data = ((JObject)turnContext.Activity.Value).ToObject<AdaptiveCardIssueSubmitAction>();
                Logger.LogInformation($"Received AdaptiveCard data: {data.action}");

                // Currently, we only have one Adaptive Card with a Submit action. Add more cases if more submit actions are added.
                switch (data.action)
                {
                    case Constants.HideIssueNotificationAction:
                        await turnContext.DeleteActivityAsync(turnContext.Activity.ReplyToId, cancellationToken);
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Unable to cast to AdaptiveCardSubmitAction.\n{e.Message}");
            }
        }
    }
}
