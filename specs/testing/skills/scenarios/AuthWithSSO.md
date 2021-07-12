# The consumer authenticates the user and passes OAuth credentials to the skill using SSO

> A consumer bot starts a multi turn interaction with a skill (e.g.: how does my day look like) and the skill renders an OAuthPrompt to allow the user to log in, once the skill obtains a token it performs an operation, returns a response to the user and logs it out.

## Variations

- TODO

## Testing matrix

- Skill: TBD
- Topology: [Simple](../SkillsFunctionalTesting.md#simple)

![Test matrix](../media/Simple.jpg)

## Variables

- Auth context: Public Cloud, Gov Cloud, Sandboxed
- Delivery mode: Normal, ExpectReplies
- Scenarios: webchat, bot to bot, teams

## Total test cases

96 (not including variations)
