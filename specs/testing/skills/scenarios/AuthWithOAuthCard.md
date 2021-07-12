# The skill needs to authenticate the user with an OAuthCard

> A consumer bot starts a multi turn interaction with a skill (e.g.: how does my day look like) and the skill renders an OAuthPrompt to allow the user to log in, once the skill obtains a token it performs an operation, returns a response to the user and logs it out.

## Variations

- The Skill sends a proactive OAuthPrompt because a user token has expired.
- TODO: we may also need to consider negative/edge scenarios like:
  - The user has never authorized the application
  - The user has previously authorized the application
  - The login fails
  - Timeouts

## Testing matrix

- Skill: [OAuthSkill](../SkillsFunctionalTesting.md#oauth-skill)
- Topology: [Simple](../SkillsFunctionalTesting.md#simple)

![Test matrix](../media/Simple.jpg)

## Variables

- Auth context: Public Cloud, Gov Cloud, Sandboxed
- Delivery mode: Normal, ExpectReplies
- Sub-Scenarios: vanilla, teams, webchat

## Total test cases

96 (not including variations)
