// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace IssueNotificationBot.Models
{
    // Adaptive cards contain a `data` property that gets submitted to the bot's Activity.Value when the user submits.
    // This model allows us to create objects from that `data`.
    public class AdaptiveCardIssueSubmitAction
    {
        public string action;
    }
}
