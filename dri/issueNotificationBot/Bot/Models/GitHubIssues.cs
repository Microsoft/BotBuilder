// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

#nullable enable

namespace IssueNotificationBot.Models
{
    // Model for issues submitted to the bot on `api/issues` from the Azure Function.
    public class GitHubIssues
    {
        [JsonProperty(PropertyName = "reportedNotReplied")]
        public readonly GitHubIssue[] ReportedNotReplied;

        [JsonProperty(PropertyName = "reportedAndReplied")]
        public readonly GitHubIssue[] ReportedAndReplied;

        [JsonProperty(PropertyName = "reportedNotRepliedNoAssignee")]
        public readonly GitHubIssue[] ReportedNotRepliedNoAssignee;

        [JsonProperty(PropertyName = "reportedAndRepliedNoAssignee")]
        public readonly GitHubIssue[] ReportedAndRepliedNoAssignee;
    }

    public class GitHubIssue
    {
        [JsonProperty(PropertyName = "number")]
        public readonly int Number;

        [JsonProperty(PropertyName = "repository")]
        public readonly GitHubRepository Repository;

        [JsonProperty(PropertyName = "title")]
        public readonly string Title;

        [JsonProperty(PropertyName = "labels")]
        public readonly GitHubLabel[] Labels;

        [JsonProperty(PropertyName = "assignees")]
        public readonly GitHubBasicUserInfo[] Assignees;

        [JsonProperty(PropertyName = "author")]
        public readonly GitHubBasicUserInfo Author;

        [JsonProperty(PropertyName = "body")]
        public readonly string Body;

        [JsonProperty(PropertyName = "createdAt")]
        public readonly DateTime CreatedAt;

        [JsonProperty(PropertyName = "url")]
        public readonly Uri Url;
    }

    public class GitHubRepository
    {
        [JsonProperty(PropertyName = "name")]
        public readonly string Name;

        [JsonProperty(PropertyName = "url")]
        public readonly Uri Url;
    }

    public class GitHubLabel
    {
        [JsonProperty(PropertyName = "name")]
        public readonly string Name;

        [JsonProperty(PropertyName = "updatedAt")]
        public readonly DateTime UpdatedAt;

        [JsonProperty(PropertyName = "url")]
        public readonly Uri Url;
    }

    public class GitHubBasicUserInfo
    {
        [JsonProperty(PropertyName = "login")]
        public readonly string Login;

        [JsonProperty(PropertyName = "name")]
        public readonly string? Name;

        [JsonProperty(PropertyName = "url")]
        public readonly Uri Url;
    }
}
