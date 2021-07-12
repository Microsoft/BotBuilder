// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;

#nullable enable

namespace IssueNotificationBot.Models
{
    // Model for the user data returned after querying the GitHub API for user data.
    public class GitHubUserResponse
    {
        [JsonProperty(PropertyName = "login")]
        public string? Login;

        [JsonProperty(PropertyName = "id")]
        public int? Id;

        [JsonProperty(PropertyName = "node_id")]
        public string? Node_id;

        [JsonProperty(PropertyName = "avatar_url")]
        public string? Avatar_url;

        [JsonProperty(PropertyName = "garvatar_id")]
        public string? Garvatar_id;

        [JsonProperty(PropertyName = "url")]
        public Uri? Url;

        [JsonProperty(PropertyName = "html_url")]
        public Uri? Html_url;

        [JsonProperty(PropertyName = "followers_url")]
        public Uri? Followers_url;

        [JsonProperty(PropertyName = "following_url")]
        public Uri? Following_url;

        [JsonProperty(PropertyName = "gists_url")]
        public Uri? Gists_url;

        [JsonProperty(PropertyName = "starred_url")]
        public Uri? Starred_url;

        [JsonProperty(PropertyName = "subscriptions_url")]
        public Uri? Subscriptions_url;

        [JsonProperty(PropertyName = "organizations_url")]
        public Uri? Organizations_url;

        [JsonProperty(PropertyName = "repos_url")]
        public Uri? Repos_url;

        [JsonProperty(PropertyName = "events_url")]
        public Uri? Events_url;

        [JsonProperty(PropertyName = "received_events_url")]
        public Uri? Received_events_url;

        [JsonProperty(PropertyName = "type")]
        public string? Type;

        [JsonProperty(PropertyName = "site_admin")]
        public bool? Site_admin;

        [JsonProperty(PropertyName = "name")]
        public string? Name;

        [JsonProperty(PropertyName = "company")]
        public string? Company;

        [JsonProperty(PropertyName = "blog")]
        public string? Blog;

        [JsonProperty(PropertyName = "location")]
        public string? Location;

        [JsonProperty(PropertyName = "email")]
        public string? Email;

        [JsonProperty(PropertyName = "hireable")]
        public bool? Hireable;

        [JsonProperty(PropertyName = "bio")]
        public string? Bio;

        [JsonProperty(PropertyName = "twitter_username")]
        public string? Twitter_username;

        [JsonProperty(PropertyName = "public_repos")]
        public int? Public_repos;

        [JsonProperty(PropertyName = "public_gists")]
        public int? Public_gists;

        [JsonProperty(PropertyName = "followers")]
        public int? Followers;

        [JsonProperty(PropertyName = "following")]
        public int? Following;

        [JsonProperty(PropertyName = "created_at")]
        public DateTime? Created_at;

        [JsonProperty(PropertyName = "updated_at")]
        public DateTime? Updated_at;

        [JsonProperty(PropertyName = "private_gists")]
        public int? Private_gists;

        [JsonProperty(PropertyName = "total_private_repos")]
        public int? Total_private_repos;

        [JsonProperty(PropertyName = "owned_private_repos")]
        public int? Owned_private_repos;

        [JsonProperty(PropertyName = "disk_usage")]
        public int? Disk_usage;

        [JsonProperty(PropertyName = "collaborators")]
        public int? Collaborators;

        [JsonProperty(PropertyName = "two_factor_authentication")]
        public bool? Two_factor_authentication;

        [JsonProperty(PropertyName = "plan")]
        public GitHubUserPlan? Plan;
    }

    public class GitHubUserPlan
    {
        [JsonProperty(PropertyName = "name")]
        public string? Name;

        [JsonProperty(PropertyName = "space")]
        public int? Space;

        [JsonProperty(PropertyName = "collaborators")]
        public int? Collaborators;

        [JsonProperty(PropertyName = "private_repos")]
        public int? Private_repos;
    }
}
