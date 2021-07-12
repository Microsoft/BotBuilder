using Newtonsoft.Json;
using System;
using System.Collections.Generic;

#nullable enable

namespace IssueNotificationBot.Models
{
    // Model used for preparing an Adaptive Card Template for the PR Card.
    public class PRCardTemplate
    {
        [JsonProperty(PropertyName = "SinglePRs")]
        public List<GitHubPRForCardTemplate>? SinglePRs;

        [JsonProperty(PropertyName = "GroupPRs")]
        public List<GitHubPRForCardTemplate>? GroupPRs;

        [JsonProperty(PropertyName = "prQueryUrl")]
        public string? PRQueryUrl;
    }

    public class GitHubPRForCardTemplate
    {
        [JsonProperty(PropertyName = "title")]
        public string? Title;

        [JsonProperty(PropertyName = "repository")]
        public string? Repository;

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime? CreatedAt;

        [JsonProperty(PropertyName = "url")]
        public string? Url;

        [JsonProperty(PropertyName = "highlight")]
        public bool? Highlight;

        [JsonProperty(PropertyName = "group")]
        public string? Group;
    }
}
