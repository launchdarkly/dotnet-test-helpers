# LaunchDarkly .NET Test Helpers

[![NuGet](https://img.shields.io/nuget/v/LaunchDarkly.TestHelpers.svg?style=flat-square)](https://www.nuget.org/packages/LaunchDarkly.TestHelpers/)
[![Quality control](https://github.com/launchdarkly/dotnet-test-helpers/actions/workflows/ci.yml/badge.svg)](https://github.com/launchdarkly/dotnet-test-helpers/actions/workflows/ci.yml)
[![Documentation](https://img.shields.io/static/v1?label=GitHub+Pages&message=API+reference&color=00add8)](https://launchdarkly.github.io/dotnet-test-helpers)

This project centralizes some test support code that is used by LaunchDarkly's .NET and Xamarin SDKs and related components, and that may be useful in other .NET projects.

See [API documentation](https://launchdarkly.github.io/dotnet-test-helpers) for full details.

## Compatibility

This version of the project is built for the following target frameworks:

* .NET Standard 2.0 (usable in Xamarin).
* .NET Core 3.1.
* .NET Framework 4.6.2 (usable in all higher versions of .NET Framework).
* .NET 6.0 (usable in all higher versions of .NET).

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
