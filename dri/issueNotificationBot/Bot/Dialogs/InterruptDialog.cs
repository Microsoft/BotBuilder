// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace IssueNotificationBot
{
    // This is more or less a root dialog so we can handle interruptions.
    public class InterruptDialog : ComponentDialog
    {
        private readonly ILogger Logger;

        public InterruptDialog(string id, string connectionName, ILogger<InterruptDialog> logger)
            : base(id)
        {
            ConnectionName = connectionName;
            Logger = logger;
        }

        protected string ConnectionName { get; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            return result ?? await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            return result ?? await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<DialogTurnResult> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                var text = innerDc.Context.Activity.Text?.ToLowerInvariant();

                // Currently, this dialog only handles the "logout" command. Update with a switch statement if more interruptions are needed.
                if (text == "logout")
                {
                    var botAdapter = (BotFrameworkAdapter)innerDc.Context.Adapter;

                    await botAdapter.SignOutUserAsync(innerDc.Context, ConnectionName, null, cancellationToken);

                    await innerDc.Context.SendActivityAsync(MessageFactory.Text("You have been signed out."), cancellationToken);
                    Logger.LogInformation($"{innerDc.Context.Activity.From.Name} has logged out");

                    return await innerDc.CancelAllDialogsAsync(cancellationToken);
                }
            }

            return null;
        }
    }
}
