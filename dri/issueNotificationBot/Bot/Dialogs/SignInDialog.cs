// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using IssueNotificationBot.Models;
using IssueNotificationBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace IssueNotificationBot
{
    // Dialog for getting user information via GitHub OAuth.
    public class SignInDialog : InterruptDialog
    {
        protected readonly ILogger Logger;
        protected readonly NotificationHelper NotificationHelper;
        protected readonly UserStorage UserStorage;

        public SignInDialog(IConfiguration configuration, ILogger<SignInDialog> logger, NotificationHelper notificationHelper, UserStorage userStorage)
            : base(nameof(SignInDialog), configuration["ConnectionName"], logger)
        {
            Logger = logger;
            NotificationHelper = notificationHelper;
            UserStorage = userStorage;

            AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = ConnectionName,
                    Text = "Please Sign In Through GitHub. This allows me to customize your experience to your GitHub profile.",
                    Title = "Sign In",
                    Timeout = 300000, // User has 5 minutes to login (1000 * 60 * 5)
                }));

            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptStepAsync,
                LoginStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the token from the previous step. Note that we could also have gotten the
            // token directly from the prompt itself. There is an example of this in the next method.
            var tokenResponse = (TokenResponse)stepContext.Result;
            if (tokenResponse != null)
            {
                Logger.LogInformation($"{stepContext.Context.Activity.From?.Name} has logged in");

                // Get the user's GitHub information with their token
                var client = new GitHubClient(tokenResponse.Token);
                var user = new TrackedUser(await client.GetAuthenticatedUser())
                {
                    ConversationReference = stepContext.Context.Activity.GetConversationReference()
                };

                var card = TemplateCardHelper.GetUserWelcomeCard(user.GitHubDetails.Avatar_url, user.GitHubDetails.Login, user.GitHubDetails.Name, NotificationHelper.Maintainer);

                await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(card), cancellationToken);

                // Get the user's Teams information
                try
                {
                    user.TeamsUserInfo = await TeamsInfo.GetMemberAsync(stepContext.Context, stepContext.Context.Activity.From.Id);
                }
                catch (ErrorResponseException e)
                {
                    Logger.LogError($"Unable to get TeamsUserInfo for {stepContext.Context.Activity.From.Name}: {e.Message}");
                }

                // Add the user to persistent storage
                await UserStorage.AddGitHubUser(user);
                await UserStorage.AddTeamsUserToGitHubUserMap(new TeamsUserToGitHubMap(user.TeamsUserInfo.Id, user.GitHubDetails.Login));
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Login was not successful please try again."), cancellationToken);
                Logger.LogWarning($"Unsuccessful login for {stepContext.Context.Activity.From?.Name}");
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
