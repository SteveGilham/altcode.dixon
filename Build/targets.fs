namespace AltCode.Dixon

module Targets =

  open System
  open System.IO
  open System.Xml.Linq

  open AltCode.Fake.DotNet
  open AltCoverFake.DotNet.Testing

  open Fake.Core
  open Fake.Core.TargetOperators
  open Fake.DotNet
  open Fake.DotNet.NuGet.NuGet
  open Fake.Testing
  open Fake.DotNet.Testing
  open Fake.IO
  open Fake.IO.FileSystemOperators
  open Fake.IO.Globbing.Operators
  open Fake.Tools.Git

  open NUnit.Framework

  let Copyright = ref String.Empty
  let Version = ref String.Empty

  let consoleBefore =
    (Console.ForegroundColor, Console.BackgroundColor)

  let programFiles =
    Environment.environVar "ProgramFiles"

  let programFiles86 =
    Environment.environVar "ProgramFiles(x86)"

  let dotnetPath =
    "dotnet"
    |> Fake.Core.ProcessUtils.tryFindFileOnPath

  let AltCoverFilter (p: Primitive.PrepareOptions) =
    { p with
        //MethodFilter = "WaitForExitCustom" :: (p.MethodFilter |> Seq.toList)
        AssemblyExcludeFilter =
          @"NUnit3\."
          :: (@"\.tests"
              :: (p.AssemblyExcludeFilter |> Seq.toList))
        AssemblyFilter = @"\.testdata" :: (p.AssemblyFilter |> Seq.toList)
        LocalSource = true
        TypeFilter =
          [ @"System\."; "Microsoft" ]
          @ (p.TypeFilter |> Seq.toList) }

  let dotnetOptions (o: DotNet.Options) =
    match dotnetPath with
    | Some f -> { o with DotNetCliPath = f }
    | None -> o

  let nugetCache =
    Path.Combine(
      Environment.GetFolderPath Environment.SpecialFolder.UserProfile,
      ".nuget/packages"
    )

  let fxcop =
    Path.getFullName "./packages/fxcop/FxCopCmd.exe"

  let cliArguments =
    { MSBuild.CliArguments.Create() with
        ConsoleLogParameters = []
        DistributedLoggers = None
        DisableInternalBinLog = true }

  let withWorkingDirectoryVM dir o =
    { dotnetOptions o with
        WorkingDirectory = Path.getFullName dir
        Verbosity = Some DotNet.Verbosity.Minimal }

  let withWorkingDirectoryOnly dir o =
    { dotnetOptions o with
        WorkingDirectory = Path.getFullName dir }

  let withCLIArgs (o: Fake.DotNet.DotNet.TestOptions) =
    { o with MSBuildParams = cliArguments }

  let withMSBuildParams (o: Fake.DotNet.DotNet.BuildOptions) =
    { o with MSBuildParams = cliArguments }

  let currentBranch =
    "."
    |> Path.getFullName
    |> Information.getBranchName

  let badge =
    if
      currentBranch.StartsWith("release/", StringComparison.Ordinal)
      && (currentBranch.Contains("pre-release") |> not)
    then
      String.Empty
    else
      "-pre"

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

  let nunitConsole =
    ("./packages/"
     + (packageVersion "NUnit.ConsoleRunner")
     + "/tools/nunit3-console.exe")
    |> Path.getFullName

  let altcover =
    ("./packages/"
     + (packageVersion "altcover")
     + "/tools/net472/AltCover.exe")
    |> Path.getFullName

  let frameworkAltcover =
    Fake.DotNet.ToolType.CreateFullFramework()

  let misses = ref 0

  let uncovered (path: string) =
    misses.Value <- 0

    !!path
    |> Seq.collect (fun f ->
      let xml = XDocument.Load f

      xml.Descendants(XName.Get("Uncoveredlines"))
      |> Seq.filter (fun x ->
        match String.IsNullOrWhiteSpace x.Value with
        | false -> true
        | _ ->
          sprintf "No coverage from '%s'" f
          |> Trace.traceImportant

          misses.Value <- 1 + misses.Value
          false)
      |> Seq.map (fun e ->
        let coverage = e.Value

        match Int32.TryParse coverage with
        | (false, _) ->
          printfn "%A" xml

          Assert.Fail(
            "Could not parse uncovered line value '"
            + coverage
            + "'"
          )

          0
        | (_, numeric) ->
          printfn "%s : %A" (f |> Path.GetDirectoryName |> Path.GetFileName) numeric
          numeric))
    |> Seq.toList

  let msbuildRelease proj =
    MSBuild.build
      (fun p ->
        { p with
            Verbosity = Some MSBuildVerbosity.Normal
            ConsoleLogParameters = []
            DistributedLoggers = None
            DisableInternalBinLog = true
            DoRestore = true
            Properties =
              [ "Configuration", "Release"
                "DebugSymbols", "True" ] })
      proj

  let msbuildDebug proj =
    MSBuild.build
      (fun p ->
        { p with
            Verbosity = Some MSBuildVerbosity.Normal
            ConsoleLogParameters = []
            DistributedLoggers = None
            DisableInternalBinLog = true
            DoRestore = true
            Properties =
              [ "Configuration", "Debug"
                "DebugSymbols", "True" ] })
      proj

  let dumpSuppressions (report: String) =
    let x = XDocument.Load report

    let messages =
      x.Descendants(XName.Get "Message")

    messages
    |> Seq.iter (fun m ->
      let mpp = m.Parent.Parent
      let target = mpp.Name.LocalName

      let tname =
        mpp.Attribute(XName.Get "Name").Value

      let (text, fqn) =
        match target with
        | "Namespace" -> ("namespace", tname)
        | "Resource" -> ("resource", tname)
        | "File"
        | "Module" -> ("module", String.Empty)
        | "Type" ->
          let spp = mpp.Parent.Parent

          ("type",
           spp.Attribute(XName.Get "Name").Value
           + "."
           + tname)
        | _ ->
          let spp = mpp.Parent.Parent
          let sp4 = spp.Parent.Parent

          ("member",
           sp4.Attribute(XName.Get "Name").Value
           + "."
           + spp.Attribute(XName.Get "Name").Value
           + "."
           + tname)

      let text2 = "[<assembly: SuppressMessage("

      let id = m.Attribute(XName.Get "Id")

      let text3 =
        (if id |> isNull |> not then
           ","
           + Environment.NewLine
           + "  MessageId=\""
           + id.Value
           + "\""
         else
           String.Empty)
        + ", Justification=\"\")>]"

      let category =
        m.Attribute(XName.Get "Category").Value

      let checkId =
        m.Attribute(XName.Get "CheckId").Value

      let name =
        m.Attribute(XName.Get "TypeName").Value

      let finish t t2 =
        let t5 =
          t2
          + "\""
          + category
          + "\", \""
          + checkId
          + ":"
          + name
          + "\""

        if t |> isNull || t = "module" then
          t5 + text3
        else
          t5
          + ","
          + Environment.NewLine
          + "  Scope=\""
          + t
          + "\", Target=\""
          + fqn
          + "\""
          + text3

      printfn
        "// %s : %s"
        checkId
        (String.Join(
          "; ",
          m.Descendants(XName.Get "Issue")
          |> Seq.map (fun i -> i.Value)
        ))

      printfn "%s" (finish text text2))

  let _Target s f =
    let doTarget s f =
      let banner x =
        printfn ""
        printfn " ****************** %s ******************" s
        f x

      Target.create s banner

    Target.description s
    doTarget s f

    let s2 = "Replay" + s
    Target.description s2
    doTarget s2 f

  // Preparation
  //_Target "Preparation" ignore

  let Clean =
    (fun _ ->
      printfn "Cleaning the build and deploy folders for %A" currentBranch
      Actions.Clean())

  let SetVersion =
    (fun _ ->
      let now = DateTime.Now

      let time =
        now
          .ToString("HHmmss")
          .Substring(0, 5)
          .TrimStart('0')

      let y0 = now.Year
      let m0 = now.Month
      let d0 = now.Day
      let y = y0.ToString()
      let m = m0.ToString()
      let d = d0.ToString()
      Version.Value <- y + "." + m + "." + d + "." + time

      let copy =
        sprintf "© 2010-%d by Steve Gilham <SteveGilham@users.noreply.github.com>" y0

      Copyright.Value <- "Copyright " + copy
      Directory.ensure "./_Generated"

      let v' = Version.Value

      [ "./_Generated/AssemblyVersion.fs"
        "./_Generated/AssemblyVersion.cs" ]
      |> List.iter (fun file ->
        AssemblyInfoFile.create
          file
          [ AssemblyInfo.Product "altcode.dixon"
            AssemblyInfo.Version v'
            AssemblyInfo.FileVersion v'
            AssemblyInfo.Company "Steve Gilham"
            AssemblyInfo.Trademark ""
            AssemblyInfo.Copyright copy ]
          (Some AssemblyInfoFileConfig.Default))

      let hack =
        """namespace AltCover
  module SolutionRoot =
    let location = """
        + "\"\"\""
        + (Path.getFullName ".")
        + "\"\"\""

      let path = "_Generated/SolutionRoot.fs"

      // Update the file only if it would change
      let old =
        if File.Exists(path) then
          File.ReadAllText(path)
        else
          String.Empty

      if not (old.Equals(hack)) then
        File.WriteAllText(path, hack))

  // Basic compilation

  //_Target "Compilation" ignore

  let Restore =
    (fun _ ->
      (!! "./**/*.*proj")
      |> Seq.iter (fun f ->
        let dir = Path.GetDirectoryName f
        let proj = Path.GetFileName f

        DotNet.restore
          (fun o ->
            let tmp =
              o.WithCommon(withWorkingDirectoryVM dir)

            let mparams =
              { tmp.MSBuildParams with
                  ConsoleLogParameters = []
                  DistributedLoggers = None
                  DisableInternalBinLog = true
                  Properties = tmp.MSBuildParams.Properties }

            { tmp with MSBuildParams = mparams })
          proj))

  let BuildRelease =
    (fun _ -> "./altcode.dixon.sln" |> msbuildRelease)

  let BuildDebug =
    (fun _ -> "./altcode.dixon.sln" |> msbuildDebug)

  // Code Analysis

  //_Target "Analysis" ignore

  let Lint =
    (fun _ ->
      let cfg =
        Path.getFullName "./fsharplint.json"

      let doLint f =
        CreateProcess.fromRawCommand "dotnet" [ "fsharplint"; "lint"; "-l"; cfg; f ]
        |> CreateProcess.ensureExitCodeWithMessage "Lint issues were found"
        |> Proc.run

      let doLintAsync f = async { return (doLint f).ExitCode }

      let throttle x =
        Async.Parallel(x, System.Environment.ProcessorCount)

      let failOnIssuesFound (issuesFound: bool) =
        Assert.That(issuesFound, Is.False, "Lint issues were found")

      [ !! "./**/*.fsproj"
        |> Seq.sortBy (Path.GetFileName)
        !! "./Build/*.fsx" |> Seq.map Path.GetFullPath ]
      |> Seq.concat
      |> Seq.map doLintAsync
      |> throttle
      |> Async.RunSynchronously
      |> Seq.exists (fun x -> x <> 0)
      |> failOnIssuesFound)

  let Gendarme =
    (fun _ -> // Needs debug because release is compiled --standalone which contaminates everything

      Directory.ensure "./_Reports"

      let rules =
        Path.getFullName "./Build/common-rules.xml"

      [ (rules, [ "_Binaries/AltCode.Dixon/Debug+x86/net472/AltCode.Dixon.dll" ])
        (Path.getFullName "./Build/build-rules.xml",
         [ Path.GetFullPath "./$Binaries/Build/Debug+AnyCPU/net7.0/Build.dll"
           Path.GetFullPath "./$Binaries/Setup/Debug+AnyCPU/net7.0/Setup.dll" ]) ]
      |> Seq.iter (fun (ruleset, files) ->
        Gendarme.run
          { Gendarme.Params.Create() with
              WorkingDirectory = "."
              Severity = Gendarme.Severity.All
              Confidence = Gendarme.Confidence.All
              Configuration = ruleset
              Console = true
              Log = "./_Reports/gendarme.html"
              LogKind = Gendarme.LogKind.Html
              Targets = files
              ToolType = ToolType.CreateLocalTool()
              FailBuildOnDefect = true }))

  let FxCop =
    (fun _ -> // Needs debug because release is compiled --standalone which contaminates everything

      let nonFsharpRules =
        [ "-Microsoft.Design#CA1006" // small namespaces
          "-Microsoft.Design#CA1020" // nested classes being visible
          "-Microsoft.Design#CA1034" // nested classes being visible
          "-Microsoft.Design#CA1062" // null checks,  In F#!
          "-Microsoft.Usage#CA2235" ]

      Shell.copyFile // self-test
        ((Path.GetDirectoryName fxcop)
         @@ "Rules/AltCode.Dixon.dll")
        (Path.getFullName "_Binaries/AltCode.Dixon/Release+x86/net472/AltCode.Dixon.dll")

      Directory.ensure "./_Reports"

      [ ([ Path.getFullName "_Binaries/AltCode.Dixon/Debug+x86/net472/AltCode.Dixon.dll" ],
         [],
         nonFsharpRules) ]
      |> Seq.iter (fun (files, types, ruleset) ->
        try
          files
          |> FxCop.run
            { FxCop.Params.Create() with
                WorkingDirectory = "."
                ToolPath = fxcop
                UseGAC = true
                Verbose = false
                ReportFileName = "_Reports/FxCopReport.xml"
                Types = types
                Rules = ruleset
                FailOnError = FxCop.ErrorLevel.Warning
                IgnoreGeneratedCode = true }
        with _ ->
          dumpSuppressions "_Reports/FxCopReport.xml"
          reraise ()))

  // Unit Test

  let UnitTest =
    (fun _ ->
      let numbers =
        (@"_Reports/_Unit*/Summary.xml") |> uncovered

      let omitted = numbers |> List.sum
      // if omitted > 1 then
      //   omitted
      //   |> (sprintf "%d uncovered lines -- coverage too low")
      //   |> Assert.Fail)
      printfn "%d uncovered lines" omitted)

  let JustUnitTest =
    (fun _ ->
      Directory.ensure "./_Reports"

      try
        let recArgs =
          [ "--noheader"
            "--x86"
            "--work=."
            "--result=./_Reports/JustUnitTestReport.xml"
            Path.getFullName
              "_Binaries/altcode.dixon.tests/Debug+AnyCPU/net472/altcode.dixon.tests.dll" ]

        Actions.Run (nunitConsole, ".", recArgs) "Recorder NUnit failed"
      with x ->
        printfn "%A" x
        reraise ())

  let UnitTestWithAltCover =
    (fun _ ->
      Directory.ensure "./_Reports/_UnitTestWithAltCover"

      let keyfile =
        Path.getFullName "Build/SelfTest.snk"

      let reports = Path.getFullName "./_Reports"

      let inputs =
        !!(@"_Binaries/*tests/Debug+AnyCPU/net4*/*tests.dll")
        |> Seq.toArray

      let indir =
        inputs |> Array.map Path.GetDirectoryName

      let outdir =
        indir
        |> Array.map (fun d -> d @@ "__UnitTestWithAltCover")

      let altReport =
        reports @@ "UnitTestWithAltCover.xml"

      let prep =
        AltCover.PrepareOptions.Primitive(
          { Primitive.PrepareOptions.Create() with
              Report = altReport
              InputDirectories = indir
              OutputDirectories = outdir
              StrongNameKey = keyfile
              InPlace = false
              Save = false }
          |> AltCoverFilter
        )
        |> AltCoverCommand.Prepare

      { AltCoverCommand.Options.Create prep with
          ToolPath = altcover
          ToolType = frameworkAltcover
          WorkingDirectory = "." }
      |> AltCoverCommand.run

      printfn "Unit test the instrumented code"

      try
        let baseArgs =
          [ "--noheader"
            "--work=."
            "--x86"
            "--result=./_Reports/UnitTestWithAltCoverReport.xml"
            // TODO generalise
            Path.getFullName
              "_Binaries/altcode.dixon.tests/Debug+AnyCPU/net472/__UnitTestWithAltCover/altcode.dixon.tests.dll" ]

        Actions.Run (nunitConsole, ".", baseArgs) "Main NUnit failed"

      with x ->
        printfn "%A" x
        reraise ()

      ReportGenerator.generateReports
        (fun p ->
          { p with
              ToolType = ToolType.CreateLocalTool()
              ReportTypes =
                [ ReportGenerator.ReportType.Html
                  ReportGenerator.ReportType.XmlSummary ]
              TargetDir = "_Reports/_UnitTestWithAltCover" })
        [ altReport ]

      uncovered @"_Reports/_UnitTestWithAltCover/Summary.xml"
      |> printfn "%A uncovered lines"

    )
  // Pure OperationalTests

  //_Target "OperationalTest" ignore

  // Packaging

  let Packaging =
    (fun _ ->
      let productDir =
        Path.getFullName "_Binaries/AltCode.Dixon/Release+x86/net472"

      let toolDir =
        Path.getFullName "_Binaries/FixCop/Release+x86/net472"

      let productFiles =
        [ (productDir @@ "AltCode.Dixon.dll", Some "tools/Rules", None)
          (toolDir @@ "DixonCmd.exe", Some "tools", None)
          (toolDir @@ "FSharp.Core.dll", Some "tools", None)
          (toolDir @@ "netstandard.dll", Some "tools", None)
          (Path.getFullName "./LICENS*", Some "", None)
          (Path.getFullName "./Dixon_128.*g", Some "", None)
          (Path.getFullName "./README.md", Some "", None)
          (Path.getFullName "./_Binaries/README.html", Some "", None) ]

      [ (productFiles,
         "_Packaging",
         "./Build/altcode.dixon.nuspec",
         "AltCode.Dixon",
         "FxCop extensions",
         []) ]
      |> List.iter (fun (files, output, nuspec, project, description, dependencies) ->
        let outputPath = "./" + output
        let workingDir = "./_Binaries/" + output
        Directory.ensure workingDir
        Directory.ensure outputPath

        NuGet
          (fun p ->
            { p with
                Authors = [ "Steve Gilham" ]
                Project = project
                Description = description
                OutputPath = outputPath
                WorkingDir = workingDir
                Files = files
                Dependencies = dependencies
                Version = (Version.Value + badge)
                Copyright = (Copyright.Value).Replace("©", "(c)")
                Publish = false
                ReleaseNotes =
                  Path.getFullName "ReleaseNotes.md"
                  |> File.ReadAllText
                ToolPath =
                  ("./packages/"
                   + (packageVersion "NuGet.CommandLine")
                   + "/tools/NuGet.exe")
                  |> Path.getFullName })
          nuspec))

  //_Target "PrepareFrameworkBuild" ignore

  //_Target "PrepareDotNetBuild" ignore

  let PrepareReadMe =
    (fun _ ->
      Actions.PrepareReadMe(
        (Copyright.Value)
          .Replace("©", "&#xa9;")
          .Replace("<", "&lt;")
          .Replace(">", "&gt;")
      ))

  //_Target "Deployment" ignore

  let Unpack =
    (fun _ ->
      let unpack = Path.getFullName "./_Unpack"
      let config = unpack @@ ".config"
      Directory.ensure unpack
      Shell.cleanDir (unpack)
      Directory.ensure config

      let text =
        File.ReadAllText "./Build/dotnet-tools.json"

      let newtext =
        text.Replace("{0}", (Version.Value + badge))

      File.WriteAllText((config @@ "dotnet-tools.json"), newtext)

      let packroot =
        Path.GetFullPath "./_Packaging"

      let config =
        XDocument.Load "./Build/NuGet.config.dotnettest"

      let repo =
        config.Descendants(XName.Get("add")) |> Seq.head

      repo.SetAttributeValue(XName.Get "value", packroot)
      config.Save(unpack @@ "NuGet.config")

      let csproj =
        XDocument.Load "./Build/unpack.xml"

      let p =
        csproj.Descendants(XName.Get("PackageReference"))
        |> Seq.head

      p.Attribute(XName.Get "VersionOverride").Value <- (Version.Value + badge)
      let proj = unpack @@ "unpack.csproj"
      csproj.Save proj

      DotNet.restore
        (fun o ->
          let tmp =
            o.WithCommon(withWorkingDirectoryVM unpack)

          let mparams =
            { tmp.MSBuildParams with
                ConsoleLogParameters = []
                DistributedLoggers = None
                DisableInternalBinLog = true
                Properties = tmp.MSBuildParams.Properties }

          { tmp with
              Packages = [ "./packages" ]
              MSBuildParams = mparams })
        proj

      let vname = Version.Value + badge

      let from =
        (Path.getFullName @"_Unpack\packages\altcode.dixon\")
        @@ vname

      printfn "Copying from %A to %A" from unpack
      Shell.copyDir unpack from (fun _ -> true)

    // TODO
    )

  // AOB

  let BulkReport =
    (fun _ ->
      printfn "Overall coverage reporting"
      Directory.ensure "./_Reports/_BulkReport"

      !! "./_Reports/*.xml"
      |> Seq.filter (fun f ->
        not
        <| f.EndsWith("Report.xml", StringComparison.OrdinalIgnoreCase))
      |> Seq.toList
      |> ReportGenerator.generateReports (fun p ->
        { p with
            ToolType = ToolType.CreateLocalTool()
            ReportTypes = [ ReportGenerator.ReportType.Html ]
            TargetDir = "_Reports/_BulkReport" }))

  let All =
    (fun _ ->
      if
        Environment.isWindows
        && currentBranch.StartsWith "release/"
        && "NUGET_API_TOKEN"
           |> Environment.environVar
           |> String.IsNullOrWhiteSpace
           |> not
      then
        (!! "./_Packagin*/*.nupkg")
        |> Seq.iter (fun f ->
          printfn "Publishing %A from %A" f currentBranch

          Actions.Run
            ("dotnet",
             ".",
             [ "nuget"
               "push"
               f
               "--api-key"
               Environment.environVar "NUGET_API_TOKEN"
               "--source"
               "https://api.nuget.org/v3/index.json" ])
            ("NuGet upload failed " + f)))

  let resetColours _ =
    Console.ForegroundColor <- consoleBefore |> fst
    Console.BackgroundColor <- consoleBefore |> snd

  Target.description "ResetConsoleColours"
  Target.createFinal "ResetConsoleColours" resetColours
  Target.activateFinal "ResetConsoleColours"

  let initTargets () =
    _Target "Preparation" ignore
    _Target "Clean" Clean
    _Target "SetVersion" SetVersion
    _Target "Compilation" ignore
    _Target "Restore" Restore
    _Target "BuildRelease" BuildRelease
    _Target "BuildDebug" BuildDebug
    _Target "Analysis" ignore
    _Target "Lint" Lint
    _Target "Gendarme" Gendarme
    _Target "FxCop" FxCop
    _Target "UnitTest" UnitTest
    _Target "JustUnitTest" JustUnitTest
    _Target "UnitTestWithAltCover" UnitTestWithAltCover
    _Target "OperationalTest" ignore
    _Target "Packaging" Packaging
    _Target "PrepareFrameworkBuild" ignore
    _Target "PrepareDotNetBuild" ignore
    _Target "PrepareReadMe" PrepareReadMe
    _Target "Deployment" ignore
    _Target "Unpack" Unpack
    _Target "BulkReport" BulkReport
    _Target "All" All

  // Dependencies
  "Clean" ==> "SetVersion" ==> "Preparation"
  |> ignore

  "Preparation"
  ==> "Restore"
  ==> "BuildDebug"
  ==> "Compilation"
  |> ignore

  "Preparation"
  ==> "Restore"
  ==> "BuildRelease"
  ==> "Compilation"
  |> ignore

  "BuildDebug" ==> "Lint" ==> "Analysis" |> ignore

  "Compilation" ?=> "Analysis" |> ignore

  "BuildDebug" ==> "FxCop" ==> "Analysis" |> ignore

  "BuildRelease" ==> "FxCop" ==> "Analysis"
  |> ignore

  "BuildDebug" ==> "Gendarme" ==> "Analysis"
  |> ignore

  "Compilation" ?=> "UnitTest" |> ignore

  "Compilation" ==> "JustUnitTest" ==> "UnitTest"
  |> ignore

  "JustUnitTest"
  ==> "UnitTestWithAltCover"
  ==> "UnitTest"
  |> ignore

  "Compilation" ?=> "OperationalTest" |> ignore

  "Compilation" ?=> "Packaging" |> ignore

  "Compilation"
  ==> "PrepareFrameworkBuild"
  ==> "Packaging"
  |> ignore

  "Compilation"
  ==> "PrepareDotNetBuild"
  ==> "Packaging"
  |> ignore

  "Compilation"
  ==> "PrepareReadMe"
  ==> "Packaging"
  ==> "Deployment"
  |> ignore

  "Analysis" ==> "All" |> ignore

  "UnitTest" ==> "All" |> ignore

  "OperationalTest" ==> "All" |> ignore

  "Deployment" ==> "BulkReport" ==> "All" |> ignore

  "Packaging" ==> "Unpack" ==> "OperationalTest"
  |> ignore

  let defaultTarget () =
    resetColours ()
    "All"