open System
open System.IO
open System.Xml
open System.Xml.Linq

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.DotNet.NuGet.Restore
open Fake.IO
open Fake.IO.FileSystemOperators

open Microsoft.Win32

let consoleBefore =
  (Console.ForegroundColor, Console.BackgroundColor)

// Really bootstrap
let dotnetPath =
  "dotnet"
  |> Fake.Core.ProcessUtils.tryFindFileOnPath

let dotnetOptions (o: DotNet.Options) =
  match dotnetPath with
  | Some f -> { o with DotNetCliPath = f }
  | None -> o

DotNet.restore
  (fun o ->
    { o with
        Packages = [ "./packages" ]
        Common = dotnetOptions o.Common })
  "./Build/NuGet.csproj"

let toolPackages =
    let xml =
      "./Directory.Packages.props"
      |> Path.getFullName
      |> XDocument.Load

    xml.Descendants()
    |> Seq.filter (fun x -> x.Attribute(XName.Get("Include")) |> isNull |> not)
    |> Seq.map (fun x ->
      (x.Attribute(XName.Get("Include")).Value, x.Attribute(XName.Get("Version")).Value))
    |> Map.ofSeq

let packageVersion (p: string) =
  p.ToLowerInvariant() + "/" + (toolPackages.Item p)

let nuget =
  ("./packages/"
   + (packageVersion "NuGet.CommandLine")
   + "/tools/NuGet.exe")
  |> Path.getFullName

let restore (o: RestorePackageParams) = { o with ToolPath = nuget }

let fxcop =
  BlackFox.VsWhere.VsInstances.getAll ()
  |> Seq.filter (fun i -> System.Version(i.InstallationVersion).Major > 15) // 2019 or later
  |> Seq.sortByDescending (fun i -> System.Version(i.InstallationVersion))
  |> Seq.map (fun i ->
    i.InstallationPath
    @@ "Team Tools/Static Analysis Tools/FxCop")
  |> Seq.filter Directory.Exists
  |> Seq.head

let _Target s f =
  Target.description s
  Target.create s f

let resetColours _ =
  Console.ForegroundColor <- consoleBefore |> fst
  Console.BackgroundColor <- consoleBefore |> snd

// Restore the NuGet packages used by the build and the Framework version

let Preparation =
  (fun _ ->
    RestoreMSSolutionPackages restore "./altcode.dixon.sln"
    Directory.ensure "./packages/fxcop/"

    let target =
      Path.getFullName "./packages/fxcop/"

    let prefix = fxcop.Length

    let check (f: string) =
      let destination =
        target @@ (f.Substring prefix)

      destination |> File.Exists |> not

    Shell.copyDir target fxcop check)

let initTargets () =
  Target.description "ResetConsoleColours"
  Target.createFinal "ResetConsoleColours" resetColours
  Target.activateFinal "ResetConsoleColours"

  _Target "Preparation" Preparation


let defaultTarget () =
  resetColours ()
  "Preparation"

[<EntryPoint>]
let private main argv =
  use c =
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "setup.fsx"

  c
  |> Context.RuntimeContext.Fake
  |> Context.setExecutionContext

  initTargets ()
  Target.runOrDefault <| defaultTarget ()

  0 // return an integer exit code