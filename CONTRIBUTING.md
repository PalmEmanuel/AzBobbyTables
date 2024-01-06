# Contributing to AzBobbyTables

You are more than welcome to contribute to the module, whether it is [Pull Requests](#pull-requests), [Feature Suggestions](#feature-suggestions) or [Bug Reports](#bug-reports)!

## Getting Started

- Fork this repository (see [this forking guide](https://guides.github.com/activities/forking/) for more information).
- Checkout the repository locally with `git clone git@github.com:{your_username}/AzBobbyTables.git`.
- If you haven't already, you will need the [platyPS](https://github.com/PowerShell/platyPS) PowerShell Module to generate command help and docs.

## Structure

The repository is organized as below:

- **Docs** (`docs/help`): Help documentation for the module. Used by `platyPS` to generate help files.
- **Tests**: Test files used for [Pester](https://github.com/pester/Pester) tests, also utilizing [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=npm) for integration tests.
- **AzBobbyTables.Core** (`Source/AzBobbyTables.Core`): The assembly which wraps the SDK and provides logic and functionality.
- **AzBobbyTables.PS** (`Source/AzBobbyTables.PS`): The compiled PowerShell module with commands and parameters.
- **build.ps1**: The script that builds the module from source, generates documentation and runs the `Pester` tests.

### Building the module

```powershell
.\build.ps1
```

- Import the module:

```powershell
Import-Module .\out\AzBobbyTables.psd1
```

### platyPS

[platyPS](https://github.com/PowerShell/platyPS) is used to write the external help in markdown. When contributing, always make sure that the changes are added to the help file.

#### Quickstart

- Install the `platyPS` module from the [PowerShell Gallery](https://www.powershellgallery.com/):

```powershell
Install-Module -Name platyPS -Scope CurrentUser
Import-Module platyPS
```

- Create markdown help files for the module (this will only create help files for new commands, existing files will not be overwritten):

```powershell
# you need the module imported in the session
Import-Module .\out\AzBobbyTables.psd1
New-MarkdownHelp -Module AzBobbyTables -OutputFolder .\docs\help
```

Edit the new markdown files in the `.\docs\help` folder and replace `{{ ... }}` placeholders with missing help content.

- Run the build script to update the documentation.

```powershell
.\build.ps1
```

- If you've made changes to the commands in the module, you can easily update the markdown files with:

```powershell
# re-import your module with latest changes
Import-Module .\out\AzBobbyTables.psd1 -Force
Update-MarkdownHelp .\docs\help
```

### Pester

[Pester](https://github.com/pester/Pester) is the ubiquitous test and mock framework for PowerShell. AzBobbyTables uses it for automatic testing of the different commands within the module, as well as integration tests with [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=npm) for local storage testing.

## Pull Requests

If you like to start contributing, please make sure that there is a related issue to link to your PR.

- Make sure that the issue is tagged in the PR.
- Write a short but informative commit message.

## Feature Suggestions

- Please first search [Open Issues](https://github.com/PalmEmanuel/AzBobbyTables/issues) before opening an issue to check whether your feature has already been suggested. If it has, feel free to add your own comments to the existing issue.
- Ensure you have included a "What?" - what your feature entails, being as specific as possible, and giving mocked-up syntax examples where possible.
- Ensure you have included a "Why?" - what the benefit of including this feature will be.

## Bug Reports

- Please first search [Open Issues](https://github.com/PalmEmanuel/AzBobbyTables/issues) before opening an issue, to see if it has already been reported.
- Try to be as specific as possible, including the version of the module, PowerShell version and OS used to reproduce the issue, and any example files or snippets of code needed to reproduce it.
