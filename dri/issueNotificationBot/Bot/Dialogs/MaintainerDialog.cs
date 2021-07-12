// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using IssueNotificationBot.Models;
using IssueNotificationBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IssueNotificationBot
{
    // Handle various commands from the Maintainer. Some need dialogs and some do not,
    // but they're all handled here just to separate the code out of Bots/<ActivityHandler>
    public class MaintainerDialog : ComponentDialog
    {
        private readonly ILogger Logger;
        private readonly NotificationHelper NotificationHelper;
        private readonly MessageBroadcaster MessageBroadcaster;
        private readonly UserStorage UserStorage;
        private const string CommandKey = "Command";

        public MaintainerDialog(ILogger<InterruptDialog> logger, MessageBroadcaster messageBroadcaster, NotificationHelper notificationHelper, UserStorage userStorage, SignInDialog dialog)
            : base(nameof(MaintainerDialog))
        {
            Logger = logger;
            MessageBroadcaster = messageBroadcaster;
            NotificationHelper = notificationHelper;
            UserStorage = userStorage;

            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(dialog);

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                CheckIfCommandNeedsDialog,
                PromptStepAsync,
                FinalizeStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        // Some commands need a dialogs and some do not.
        private async Task<DialogTurnResult> CheckIfCommandNeedsDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Store the command in stepContext so we can reference it later in the flow.
            stepContext.Values[CommandKey] = stepContext.Context.Activity.Text;

            // Check to see if Maintainer is trying to view command list. If so, adjust their text to the ShowCommands command.
            var showCommandsRegex = new Regex(@"^(show)?( )?command(s)?(?!\:)", RegexOptions.IgnoreCase);
            if (showCommandsRegex.IsMatch(stepContext.Context.Activity.Text))
            {
                stepContext.Context.Activity.Text = MaintainerCommands.ShowCommands;
            }

            var commandsNeedingMaintainerDialog = new HashSet<string>()
            {
                MaintainerCommands.UpdateUserNotificationSettings,
                MaintainerCommands.SetBroadcastMessage,
                MaintainerCommands.SendBroadcastMessage,
                MaintainerCommands.ResendGrettings
            };

            if (MessageContainsCommandPrefix(stepContext.Context.Activity.Text))
            {
                if (commandsNeedingMaintainerDialog.Contains(stepContext.Context.Activity.Text))
                {
                    return await stepContext.NextAsync();
                }
                else
                {
                    // These commands don't need a dialog, so we'll just handle them and end the dialog.
                    await HandleNonDialogCommands(stepContext, cancellationToken);
                    return await stepContext.EndDialogAsync();
                }
            }
            else if (stepContext.Context.Activity.Text == Constants.LoginCommand)
            {
                return await stepContext.BeginDialogAsync(nameof(SignInDialog), null, cancellationToken);
            }

            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Store the command in stepContext so we can reference it later in the flow.
            stepContext.Values[CommandKey] = stepContext.Context.Activity.Text;

            // Adjust the prompt sent based on the command received.
            switch (stepContext.Context.Activity.Text)
            {
                case MaintainerCommands.SetBroadcastMessage:
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("What message do you want to broadcast?")
                    });

                case MaintainerCommands.SendBroadcastMessage:
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text($"Are you sure you want to send:\n\n{MessageBroadcaster.BroadcastMessage.Text}")
                    });

                case MaintainerCommands.UpdateUserNotificationSettings:
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Are you sure you want to send update all user notification settings?")
                    });

                case MaintainerCommands.ResendGrettings:
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Are you sure you want to resend all user greetings? **Be sure you sent this command from a Team!**")
                    });

                default:
                    await stepContext.Context.SendActivityAsync($"Maintainer Dialog failed because I didn't understand {stepContext.Context.Activity.Text}");
                    return await stepContext.CancelAllDialogsAsync();
            }
        }

        private async Task<DialogTurnResult> FinalizeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = stepContext.Result;

            // Adjust how to handle the result based on the original command sent.
            switch (stepContext.Values[CommandKey])
            {
                case MaintainerCommands.SetBroadcastMessage:
                    MessageBroadcaster.BroadcastMessage = MessageFactory.Text(stepContext.Result as string);
                    await stepContext.Context.SendActivityAsync($"Broadcast message set to:\n\n{MessageBroadcaster.BroadcastMessage.Text}");
                    break;

                case MaintainerCommands.SendBroadcastMessage:
                    if ((bool)result)
                    {
                        await stepContext.Context.SendActivityAsync($"Broadcasting:\n\n{MessageBroadcaster.BroadcastMessage.Text}");
                        await MessageBroadcaster.SendBroadcastMessage();
                    }
                    break;

                case MaintainerCommands.UpdateUserNotificationSettings:
                    await UserStorage.OverwriteNotificationSettingsForAllUsers();
                    await stepContext.Context.SendActivityAsync("All users have default notification settings");
                    break;

                case MaintainerCommands.ResendGrettings:
                    await ResendGreetings(stepContext.Context, cancellationToken);
                    break;

                default:
                    await stepContext.Context.SendActivityAsync($"Maintainer Dialog failed because I didn't understand {stepContext.Result}");
                    return await stepContext.CancelAllDialogsAsync();
            }

            return await stepContext.EndDialogAsync();
        }

        private static bool MessageContainsCommandPrefix(string text)
        {
            return text.StartsWith(MaintainerCommands.CommandPrefix);
        }

        private async Task HandleNonDialogCommands(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var turnContext = stepContext.Context;

            switch (turnContext.Activity.Text)
            {
                case MaintainerCommands.ShowCommands:
                    await SendMaintainerCommands(turnContext, cancellationToken);
                    break;

                case MaintainerCommands.EnableMaintainerNotifications:
                    Logger.LogInformation("Enabling Maintainer Notifications");
                    NotificationHelper.NotifyMaintainer = true;
                    await turnContext.SendActivityAsync("Maintainer Notifications Enabled");
                    break;

                case MaintainerCommands.DisableMaintainerNotifications:
                    Logger.LogInformation("Disabling Maintainer Notifications");
                    NotificationHelper.NotifyMaintainer = false;
                    await turnContext.SendActivityAsync("Maintainer Notifications Disabled");
                    break;

                case MaintainerCommands.TestCards:
                    await SendMaintainerTestCards(turnContext);
                    break;

                case MaintainerCommands.ViewBroadcastMessage:
                    await turnContext.SendActivityAsync(MessageBroadcaster.BroadcastMessage ?? MessageFactory.Text("**No Broadcast Message Set**"));
                    break;

                default:
                    await turnContext.SendActivityAsync("unknown command");
                    break;
            }
        }

        // Send the Maintainer a card containing buttons for activating all available Maintainer Commands.
        private static async Task SendMaintainerCommands(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Use Reflection to get the names of the commands from the MaintainerCommands constants.
            var commands = typeof(MaintainerCommands)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(string))
                .ToArray();

            var actions = new List<CardAction>();

            // Teams limits cards to 6 buttons/actions, so we need to send cards, each containing 6 commands, until we've sent them all.
            var itemIndex = 0;
            var cardNum = 1;
            foreach (var command in commands)
            {
                var commandString = (string)command.GetValue(null);

                // Some of the MaintainerCommands constants should not be included.
                var ignoreCommands = new HashSet<string>()
                {
                    MaintainerCommands.CommandPrefix,
                    MaintainerCommands.ShowCommands
                };

                if (!ignoreCommands.Contains(commandString))
                {
                    var title = commandString.Clone().ToString().Split(":")[1];
                    actions.Add(new CardAction
                    {
                        Title = title,
                        Value = commandString,
                        Type = ActionTypes.ImBack
                    });

                    // Teams doesn't support SuggestedActions and only allows 6 actions per card, without scrolling, so we need to split them up.
                    if (actions.Count == 6 || itemIndex == commands.Length - 1)
                    {
                        var card = new HeroCard($"Maintainer Commands - {cardNum} of {Math.Ceiling((double)(commands.Length) / 6)}", null, null, null, actions, null);
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(card.ToAttachment()), cancellationToken);
                        actions.Clear();
                        cardNum++;
                    }
                }

                itemIndex++;
            }
        }

        // Generate and send samples of all cards in this bot.
        private async Task SendMaintainerTestCards(ITurnContext turnContext)
        {
            var maintainer = NotificationHelper.Maintainer;

            var welcomeCard = TemplateCardHelper.GetUserWelcomeCard(
                        maintainer.GitHubDetails.Avatar_url,
                        maintainer.GitHubDetails.Login,
                        maintainer.GitHubDetails.Name,
                        maintainer);

            var fakeIssue = new
            {
                Title = "Test Title",
                CreatedAt = new DateTime(),
                Body = "Test Body",
                Url = "www.contoso.com",
                Number = 999
            };

            var issueCard = TemplateCardHelper.GetPersonalIssueCard(
                JsonConvert.DeserializeObject<GitHubIssue>(JsonConvert.SerializeObject(fakeIssue)),
                "TEST EXPIRE MESSAGE",
                new DateTime(),
                "TEST ACTION",
                maintainer);

            var fakePRs = GetFakePRs();

            var prCard = TemplateCardHelper.GetPersonalPRCard(
                JsonConvert.DeserializeObject<PRCardTemplate>(JsonConvert.SerializeObject(fakePRs)),
                maintainer
            );

            await turnContext.SendActivitiesAsync(new IActivity[] {
                MessageFactory.Attachment(welcomeCard),
                MessageFactory.Attachment(issueCard),
                MessageFactory.Attachment(prCard)
            });
        }

        private async Task ResendGreetings(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Resending Greetings");

            var members = await TeamsInfo.GetTeamMembersAsync(turnContext);
            foreach (var member in members)
            {
                try
                {
                    if (!await UserStorage.HaveUserDetails(member.Id))
                    {
                        await NotificationHelper.GreetNewTeamMember(member, turnContext, cancellationToken);
                    }
                    else
                    {
                        Logger.LogInformation($"Already have login info for: { member.Name }");
                    }
                }
                // Users that block the bot throw Forbidden errors. We'll catch all exceptions in case
                // unforeseen errors occur; we want to message as many members as possible.
                catch (Exception e)
                {
                    Logger.LogError(new EventId(1), e, $"Something went wrong when greeting { member.Name }");
                }
            }
        }

        private static object GetFakePRs()
        {
            var template = new
            {
                SinglePRs = new List<object>(),
                GroupPRs = new List<object>()
            };

            for (int i = 0; i < 10; i++)
            {
                var pr = new
                {
                    Title = $"{i}FAKE PR TITLE DO SOME THINGS",
                    Repository = $"{i}FAKE REPOSITORY",
                    CreatedAt = DateTime.Now.AddDays(-(10 - i)),
                    URL = $"www.{i}.com",
                    Highlight = i < 2,
                    Group = "fakeGroup"
                };

                template.SinglePRs.Add(pr);
                template.GroupPRs.Add(pr);
            }

            return template;
        }
    }
}
