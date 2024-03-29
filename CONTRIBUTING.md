# Contributing to the LaunchDarkly .NET Test Helpers

LaunchDarkly has published an [SDK contributor's guide](https://docs.launchdarkly.com/docs/sdk-contributors-guide) that provides a detailed explanation of how our SDKs work. See below for additional information on how to contribute to this project.

## Submitting bug reports and feature requests

The LaunchDarkly SDK team monitors the [issue tracker](https://github.com/launchdarkly/dotnet-test-helpers/issues) in this repository. Bug reports and feature requests specific to this package should be filed in this issue tracker. The SDK team will respond to all newly filed issues within two business days.
 
## Submitting pull requests
 
We encourage pull requests and other contributions from the community. Before submitting pull requests, ensure that all temporary or unintended code is removed. Don't worry about adding reviewers to the pull request; the LaunchDarkly SDK team will add themselves. The SDK team will acknowledge all pull requests within two business days.
 
## Build instructions
 
### Prerequisites

To set up your SDK build time environment, you must [download .NET development tools and follow the instructions](https://dotnet.microsoft.com/download). .NET 6.0 is preferred, since the .NET 6.0 tools are able to build for all supported target platforms.

### Building
 
To install all required packages:

```
dotnet restore
```

To build all targets of the project without running any tests:

```
dotnet build src/LaunchDarkly.TestHelpers
```

Or, to build only the .NET Standard 2.0 target:

```
dotnet build src/LaunchDarkly.TestHelpers -f netstandard2.0
```
 
### Testing
 
To run all unit tests, for all targets:

```
dotnet test test/LaunchDarkly.TestHelpers.Tests
```

Or, to run tests only for the .NET Standard 2.0 target (using the .NET Core 3.1 runtime):

```
dotnet test test/LaunchDarkly.TestHelpers.Tests -f netcoreapp3.1
```
