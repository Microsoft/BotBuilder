// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveCards;
using AdaptiveCards.Templating;
using IssueNotificationBot.Models;
using Microsoft.Bot.Schema;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using System;
using System.IO;

namespace IssueNotificationBot.Services
{
    public static class TemplateCardHelper
    {
        public static Attachment GetUserWelcomeCard(string avatar_url, string login, string name, TrackedUser maintainer)
        {
            // User may not have their name set in their profile
            name ??= "GitHub User";

            var templateData = new
            {
                avatar_url,
                login,
                name,
                maintainerName = maintainer?.TeamsUserInfo?.Name,
                maintainerEmail = maintainer?.TeamsUserInfo?.Email
            };

            return CreateTemplateCard("userWelcomeCard.json", templateData);
        }

        public static Attachment GetPersonalIssueCard(GitHubIssue issue, string nearingOrExpiredMessage, DateTime expires, string action, TrackedUser maintainer)
        {
            var templateData = new
            {
                nearingOrExpiredMessage,
                issueTitle = issue.Title,
                issueCreated = issue.CreatedAt,
                issueExpires = expires,
                body = issue.Body,
                issueUrl = issue.Url,
                issueNumber = issue.Number,
                action,
                maintainerName = maintainer?.TeamsUserInfo?.Name,
                maintainerEmail = maintainer?.TeamsUserInfo?.Email
            };

            return CreateTemplateCard("personalIssueCard.json", templateData);
        }

        public static Attachment GetPersonalPRCard(PRCardTemplate prs, TrackedUser maintainer)
        {
            var templateData = new
            {
                singlePRs = prs.SinglePRs,
                groupPRs = prs.GroupPRs,
                prQueryUrl = prs.PRQueryUrl,
                maintainerName = maintainer?.TeamsUserInfo?.Name,
                maintainerEmail = maintainer?.TeamsUserInfo?.Email
            };

            return CreateTemplateCard("personalPRCard.json", templateData);
        }

        // Helper for creating Adaptive Card Templates
        private static Attachment CreateTemplateCard(string pathInResources, object templateData)
        {
            var paths = new[] { ".", "Resources", pathInResources };
            var adaptiveCardJson = File.ReadAllText(Path.Combine(paths));

            var templateCard = new AdaptiveCardTemplate(adaptiveCardJson);

            var serializedData = SafeJsonConvert.SerializeObject(templateData);

            var cardJson = templateCard.Expand(templateData);

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject<AdaptiveCard>(cardJson),
            };
        }
    }
}
