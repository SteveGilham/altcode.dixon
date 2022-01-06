# altcode.dixon
FxCop rule extensions and related code.  A project named for that well known police constable of yesteryear.  Intended for use with F#, since we don't have Roslyn analyzers there.

## Features
### FxCop for `netstandard2.0`

An executable, `DixonCmd.exe` that injects a `netstandard2.0` compatible platform definition into the `FxCopCmd` process and launches an analysis session.

### FxCop extra rules
* `JustifySuppressionRule` (`Dixon.Design#DX0001`), the "Hello, World!" of FxCop rules.  A port of the well travelled example rule to ensure that `SuppressMessage` attributes include a justification.  See e.g. http://www.binarycoder.net/fxcop/html/ex_specifysuppressmessagejustification.html
* `ReraiseCorrectlyRule` (`Dixon.Design#DX0002`) : only `throw` an exception you've just created

## Never mind the fluff -- How do I use this?

[Here's the recipe I use](https://github.com/SteveGilham/altcode.dixon/wiki).

## Developer/Contributor info

### Build process from trunk as per `appveyor.yml`

Assumes VS2022 build environment

* dotnet tool restore
* dotnet fake run .\Build\setup.fsx
* dotnet fake run .\Build\build.fsx

### Direction
The F# focus will include making rule variants that are more F# aware, to separate out the compiler generated clutter from the code the developer can affect -- avoiding smothering the code in `[<SuppressMessage>]`, or throwing rules out for poor signal to noise.  But there will inevitably be some more originals.

### Badges
* [![Nuget](https://buildstats.info/nuget/altcode.dixon?includePreReleases=true)](https://www.nuget.org/packages/altcode.dixon)
* [![Build status](https://img.shields.io/appveyor/ci/SteveGilham/altcode-dixon.svg)](https://ci.appveyor.com/project/SteveGilham/altcode-dixon)
* ![Build history](https://buildstats.info/appveyor/chart/SteveGilham/altcode-dixon)
* [![CI](https://github.com/SteveGilham/altcode.dixon/workflows/CI/badge.svg)](https://github.com/SteveGilham/altcode.dixon/actions?query=workflow%3ACI)
* ![Build history](https://buildstats.info/github/chart/SteveGilham/altcode.dixon?branch=master)]

