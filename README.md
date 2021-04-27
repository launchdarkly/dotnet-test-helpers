# LaunchDarkly .NET Test Helpers

[![NuGet](https://img.shields.io/nuget/v/LaunchDarkly.TestHelpers.svg?style=flat-square)](https://www.nuget.org/packages/LaunchDarkly.TestHelpers/)
[![CircleCI](https://circleci.com/gh/launchdarkly/dotnet-test-helpers.svg?style=shield)](https://circleci.com/gh/launchdarkly/dotnet-test-helpers)
[![Documentation](https://img.shields.io/static/v1?label=GitHub+Pages&message=API+reference&color=00add8)](https://launchdarkly.github.io/dotnet-testhelpers)

This project centralizes some test support code that is used by LaunchDarkly's .NET and Xamarin SDKs and related components, and that may be useful in other .NET projects.

While this code may be useful in other projects, it is primarily geared toward LaunchDarkly's own development needs and is not meant to provide a large general-purpose framework. It is meant for unit test code and should not be used as a runtime dependency.

## Compatibility

This version of the project is built for two target frameworks:

* .NET Standard 2.0: Usable in .NET Core 2+, .NET 5+, and Xamarin.
* .NET Framework 4.5.2: Usable in .NET Framework 4.5.2 and above.

## Contents

The NuGet package name is `LaunchDarkly.TestHelpers`.

The namespace `LaunchDarkly.TestHelpers.HttpTest` provides a simple abstraction for setting up embedded HTTP test servers that return programmed responses, and verifying that the expected requests have been made in tests. (The underlying implementation is `System.Net.HttpListener`. There are other HTTP mock server libraries for .NET, most of which also use `System.Net.HttpListener` either directly or indirectly, but they were generally not suitable for LaunchDarkly's testing needs due to either platform compatibility limitations or their API design.)

## Contributing

We encourage pull requests and other contributions from the community. Check out our [contributing guidelines](CONTRIBUTING.md) for instructions on how to contribute to this project.

## About LaunchDarkly

* LaunchDarkly is a continuous delivery platform that provides feature flags as a service and allows developers to iterate quickly and safely. We allow you to easily flag your features and manage them from the LaunchDarkly dashboard.  With LaunchDarkly, you can:
    * Roll out a new feature to a subset of your users (like a group of users who opt-in to a beta tester group), gathering feedback and bug reports from real-world use cases.
    * Gradually roll out a feature to an increasing percentage of users, and track the effect that the feature has on key metrics (for instance, how likely is a user to complete a purchase if they have feature A versus feature B?).
    * Turn off a feature that you realize is causing performance problems in production, without needing to re-deploy, or even restart the application with a changed configuration file.
    * Grant access to certain features based on user attributes, like payment plan (eg: users on the ‘gold’ plan get access to more features than users in the ‘silver’ plan). Disable parts of your application to facilitate maintenance, without taking everything offline.
* LaunchDarkly provides feature flag SDKs for a wide variety of languages and technologies. Check out [our documentation](https://docs.launchdarkly.com/docs) for a complete list.
* Explore LaunchDarkly
    * [launchdarkly.com](https://www.launchdarkly.com/ "LaunchDarkly Main Website") for more information
    * [docs.launchdarkly.com](https://docs.launchdarkly.com/  "LaunchDarkly Documentation") for our documentation and SDK reference guides
    * [apidocs.launchdarkly.com](https://apidocs.launchdarkly.com/  "LaunchDarkly API Documentation") for our API documentation
    * [blog.launchdarkly.com](https://blog.launchdarkly.com/  "LaunchDarkly Blog Documentation") for the latest product updates
