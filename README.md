# altcode.dixon
FxCop rule extensions and related code.  A project named for that well known police constable of yesteryear.

### Badges
* [![Nuget](https://buildstats.info/nuget/altcode.gendarme?includePreReleases=true) Command-line tool](https://www.nuget.org/packages/altcode.dixon)
* [![Build status](https://img.shields.io/appveyor/ci/SteveGilham/Gendarme.svg)](https://ci.appveyor.com/project/SteveGilham/altcode.dixon)
* ![Build history](https://buildstats.info/appveyor/chart/SteveGilham/altcode.dixon)

## Build process from trunk as per `appveyor.yml`

Assumes VS2019 build environment

* dotnet tool restore
* dotnet fake run .\Build\setup.fsx
* dotnet fake run .\Build\build.fsx

The `build` stage can be done in Visual Studio with the Debug configuration to run the unit tests

## Direction
As usual, the focus is on F#, since we don't have Roslyn analyzers there.  This will include making rule variants that are more F# aware, to separate out the compiler generated clutter from the code the developer can affect -- avoiding smothering the code in `[<SuppressMessage>]`, or throwing rules out for poor signal to noise.  But there will inevitably be some originals.

## Features
* `JustifySuppressionRule` (`Dixon.Design#DX0001`), the "Hello, World!" of FxCop rules.  A port of the well travelled example rule to ensure that `SuppressMessage` attributes include a justification.  See e.g. http://www.binarycoder.net/fxcop/html/ex_specifysuppressmessagejustification.html