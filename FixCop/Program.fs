﻿open System
open System.IO
open System.Reflection
open Microsoft.VisualStudio.CodeAnalysis
open Microsoft.VisualStudio.CodeAnalysis.Common
open System.Collections.Generic

[<EntryPoint>]
let main argv =
  let plat =
    argv |> Seq.find (fun a -> a.StartsWith("/plat"))

  let platformPath =
    plat.Substring(plat.IndexOf(':') + 1)

  let here =
    Assembly.GetExecutingAssembly().Location
    |> Path.GetDirectoryName

  let files =
    here
    |> Directory.GetFiles
    |> Seq.filter (fun f -> f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
    |> Seq.sortDescending
    |> Seq.map (fun n -> Path.Combine(here, n) |> Assembly.LoadFile)

  // tracing
  let ca =
    Path.Combine(here, "Microsoft.VisualStudio.CodeAnalysis.dll")
    |> Assembly.LoadFile

  let catrace =
    ca.GetType("Microsoft.VisualStudio.CodeAnalysis.Diagnostics.CATrace")

  let verbose =
    System.Diagnostics.TraceLevel.Verbose

  catrace
    .GetProperty("TraceLevel")
    .SetValue(null, verbose)

  // interop info
  let cainterop =
    Path.Combine(here, "Microsoft.VisualStudio.CodeAnalysis.Interop.dll")
    |> Assembly.LoadFile

  let info =
    cainterop.GetType("Microsoft.VisualStudio.CodeAnalysis.Common.AssemblyInfo")

  let getInfo =
    info.GetMethod("GetAssemblyInfo", BindingFlags.Public ||| BindingFlags.Static)

  let props =
    info.GetProperties(
      BindingFlags.Public
      ||| BindingFlags.NonPublic
      ||| BindingFlags.Instance
      ||| BindingFlags.Static
    )

  // key assembly
  let cacommon =
    Path.Combine(here, "Microsoft.VisualStudio.CodeAnalysis.Common.dll")
    |> Assembly.LoadFile

  // interesting types
  let platform =
    cacommon.GetType("Microsoft.VisualStudio.CodeAnalysis.Common.Platform")

  let unification =
    cacommon.GetType(
      "Microsoft.VisualStudio.CodeAnalysis.Common.UnificationAssemblyNameMap"
    )

  let ptype =
    typeof<PlatformInfo>.Assembly
      .GetType("Microsoft.VisualStudio.CodeAnalysis.PlatformType")

  let unknown = Enum.Parse(ptype, "Unknown")

  // interesting calls
  let makeUnify =
    unification.GetConstructor(
      BindingFlags.Public ||| BindingFlags.Instance,
      null,
      [||],
      null
    )

  let makePlatform =
    platform.GetConstructor(
      BindingFlags.NonPublic ||| BindingFlags.Instance,
      null,
      [| ptype
         typeof<AssemblyName>
         typeof<IAssemblyNameMap>
         typeof<IEnumerable<string>>
         typeof<string> |],
      null
    )

  let adder =
    unification.GetMethod("AddMapping", BindingFlags.Public ||| BindingFlags.Instance)

  let platforms =
    platform
      .GetField("s_platforms", BindingFlags.Static ||| BindingFlags.NonPublic)
      .GetValue(null)
    :?> System.Collections.IList

  let alt =
    platform.GetField(
      "m_alternatePlatform",
      BindingFlags.NonPublic ||| BindingFlags.Instance
    )

  // interesting platform assemblies
  let netstd21 =
    Path.Combine(here, "netstandard.dll")

  let netstdlib21 =
    netstd21 |> AssemblyName.GetAssemblyName

  let netstd20 =
    Path.Combine(platformPath, "netstandard.dll")

  let netstdlib20 =
    netstd20 |> AssemblyName.GetAssemblyName

  let netinfo =
    getInfo.Invoke(null, [| netstd21 :> obj |])

  let uMap =
    Convert.ChangeType(makeUnify.Invoke([||]), unification)

  adder.Invoke(
    uMap,
    [| netstdlib20 :> obj
       netstdlib21 :> obj |]
  )
  |> ignore

  adder.Invoke(
    uMap,
    [| netstdlib21 :> obj
       netstdlib20 :> obj |]
  )
  |> ignore

  let add =
    makePlatform.Invoke(
      [| unknown
         netstdlib21
         uMap
         [ here ]
         netstd21 |]
    )

  let add3 =
    makePlatform.Invoke(
      [| unknown
         netstdlib20
         uMap
         List.empty<string>
         netstd20 |]
    )

  let nextv =
    platform.GetField(
      "m_nextPlatformVersion",
      BindingFlags.NonPublic ||| BindingFlags.Instance
    )

  nextv.SetValue(add3, add)

  let pi =
    platform.GetProperty("PlatformInfo")

  let pi1 = pi.GetValue(add) :?> PlatformInfo
  let pi3 = pi.GetValue(add3) :?> PlatformInfo

  let pin =
    typeof<PlatformInfo>
      .GetProperty("PlatformType", BindingFlags.NonPublic ||| BindingFlags.Instance)

  pin.SetValue(pi3, pin.GetValue(pi1))

  let piv =
    typeof<PlatformInfo>
      .GetProperty("PlatformVersion", BindingFlags.Public ||| BindingFlags.Instance)

  piv.SetValue(pi3, Version(2, 0, 0, 0))

  alt.SetValue(add, add3)
  alt.SetValue(add3, add)

  platforms.Add add |> ignore
  platforms.Add add3 |> ignore

  let fxcop =
    Path.Combine(here, "FxCopCmd.exe")

  let driven = fxcop |> Assembly.LoadFile

  let command =
    driven.GetType("Microsoft.FxCop.Command.FxCopCommand")

  let main =
    command.GetMethod("Main", BindingFlags.Static ||| BindingFlags.Public)

  let r = main.Invoke(null, [| argv :> obj |])

  r :?> int