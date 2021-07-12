# GitHub PR Comment Task

This task has the functionality to receive plain text files and use the content to create a GitHub comment on a Pull Request.

## Configuration

### User Token

[GitHub Personal Access Token](https://help.github.com/en/articles/creating-a-personal-access-token-for-the-command-line) (it must have *Repo* permissions).

### Repository

RepositoryOwner/Repository name. Use  `$(Build.Repository.Name)` if you want to run on the build's repository.

### Pull Request Number

Number of the pull request you want to comment. Use `$(System.PullRequest.PullRequestNumber)` if you want to run on the build's PR.

### Path to the file/s

Path to the file/s that you want to get the PR comments from. If you specify a folder, it will pick all the files that have the specified extension inside that folder.

### Use sub-folders

Checking this option will enable to search for all the files of the specified extensions in all the folders and sub-folders contained in the previously selected path.

### Basic Example

We prepared a simple example using the task by creating a pull request from a branch containing a message example. When the PR is created, the Azure Pipelines build is triggered, and the GitHub PR Comment task picks up the plain text file from the PR branch and creates a message on the PR with its contents.

## Azure Pipelines Setup

![PipelineSettings1](./media/guide-01.png)

![PipelineSettings2](./media/guide-02.png)

![PipelineSettings3](./media/guide-03.png)

![PipelineResults](./media/guide-04.png)
