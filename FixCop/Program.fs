open System
open System.IO
open System.Reflection
open Microsoft.VisualStudio.CodeAnalysis
open Microsoft.VisualStudio.CodeAnalysis.Common
open System.Collections.Generic

[<EntryPoint>]
let main argv =
  let here = Assembly.GetExecutingAssembly().Location

  let files =
    here
    |> Path.GetDirectoryName
    |> Directory.GetFiles
    |> Seq.filter (fun f -> f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
    |> Seq.sortDescending
    |> Seq.map
         (fun n ->
           Path.Combine(here |> Path.GetDirectoryName, n)
           |> Assembly.LoadFile)

  // tracing
  let ca =
    Path.Combine(here |> Path.GetDirectoryName, "Microsoft.VisualStudio.CodeAnalysis.dll")
    |> Assembly.LoadFile

  let catrace =
    ca.GetType("Microsoft.VisualStudio.CodeAnalysis.Diagnostics.CATrace")

  let verbose = System.Diagnostics.TraceLevel.Verbose

  catrace
    .GetProperty("TraceLevel")
    .SetValue(null, verbose)

  // interop info
  let cainterop =
    Path.Combine(
      here |> Path.GetDirectoryName,
      "Microsoft.VisualStudio.CodeAnalysis.Interop.dll"
    )
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
    Path.Combine(
      here |> Path.GetDirectoryName,
      "Microsoft.VisualStudio.CodeAnalysis.Common.dll"
    )
    |> Assembly.LoadFile

  // interesting types
  let platform =
    cacommon.GetType("Microsoft.VisualStudio.CodeAnalysis.Common.Platform")

  let unification =
    cacommon.GetType(
      "Microsoft.VisualStudio.CodeAnalysis.Common.UnificationAssemblyNameMap"
    )

  let ptype =
    typeof<PlatformInfo>.Assembly.GetType
      ("Microsoft.VisualStudio.CodeAnalysis.PlatformType")

  let midori = Enum.Parse(ptype, "Midori")
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
  let corelib =
    AssemblyName(
      "System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
    )

  let netstdlib =
    AssemblyName(
      "netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51"
    )

  // TODO -- environment names
  let printInfo i =
    props
    |> Array.iter (fun p -> printfn "%s : %A" p.Name (p.GetValue(i, null)))

  let netstd2 =
    """C:\Program Files\dotnet\shared\Microsoft.NETCore.App\2.0.9\netstandard.dll"""

  let netinfo =
    getInfo.Invoke(null, [| netstd2 :> obj |])

  printInfo netinfo

  let refs =
    (props
     |> Array.find (fun p -> p.Name = "AssemblyReferences"))
      .GetValue(netinfo, null)
    :?> IList<AssemblyName>

  // moniker ".NETStandard,Version=v2.0 "
  let core =
    """C:\Program Files\dotnet\shared\Microsoft.NETCore.App\2.0.9\System.Private.CoreLib.dll"""

  let dirpath = netstd2 |> Path.GetDirectoryName

  let refpaths =
    Directory.GetFiles(
      @"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\2.0.9",
      "*.dll"
    )

  let uMap =
    Convert.ChangeType(makeUnify.Invoke([||]), unification)

  refs
  |> Seq.iter (fun r -> adder.Invoke(uMap, [| r; r |]) |> ignore)
  //adder.Invoke(uMap, [| corelib :> obj; netstdlib :> obj|]) |> ignore
  //adder.Invoke(uMap, [| netstdlib :> obj; corelib :> obj|]) |> ignore

  //let add = makePlatform.Invoke([| unknown; netstdlib; uMap; [dirpath] ; core |])
  //platforms.Add add |> ignore

  //refpaths
  //|> Seq.map (fun f -> try
  //                        Some ((f |> Assembly.LoadFile).GetName())
  //                     with
  //                     | _ -> None)
  //|> Seq.choose id
  //|> Seq.iter (fun n -> let add2 = makePlatform.Invoke([| unknown; n; uMap; [dirpath] ; dirpath |])
  //                      alt.SetValue(add, add2)
  //                      alt.SetValue(add2, add)
  //                      add2
  //                      |> platforms.Add
  //                      |> ignore)

  let add =
    makePlatform.Invoke(
      [| unknown
         netstdlib
         uMap
         refpaths
         netstd2 |]
    )

  let add2 =
    makePlatform.Invoke(
      [| unknown
         corelib
         uMap
         [ dirpath ]
         core |]
    )

  let pi = platform.GetProperty("PlatformInfo")
  let pi1 = pi.GetValue(add) :?> PlatformInfo
  let pi2 = pi.GetValue(add2) :?> PlatformInfo

  let pin =
    typeof<PlatformInfo>.GetProperty
      ("PlatformType", BindingFlags.NonPublic ||| BindingFlags.Instance)

  pin.SetValue(pi2, pin.GetValue(pi1))

  let piv =
    typeof<PlatformInfo>.GetProperty
      ("PlatformVersion", BindingFlags.Public ||| BindingFlags.Instance)

  piv.SetValue(pi2, piv.GetValue(pi1))

  alt.SetValue(add, add2)
  alt.SetValue(add2, add)

  platforms.Add add |> ignore
  platforms.Add add2 |> ignore

  let fxcop =
    Path.Combine(here |> Path.GetDirectoryName, "FxCopCmd.exe")

  let driven = fxcop |> Assembly.LoadFile

  let command =
    driven.GetType("Microsoft.FxCop.Command.FxCopCommand")

  let main =
    command.GetMethod("Main", BindingFlags.Static ||| BindingFlags.Public)

  let r = main.Invoke(null, [| argv :> obj |])

  [ add; add2 ]
  |> List.iter
       (fun a ->
         platform.GetProperties(
           BindingFlags.Public
           ||| BindingFlags.NonPublic
           ||| BindingFlags.Instance
           ||| BindingFlags.Static
         )
         |> Seq.iter
              (fun p ->
                let v = p.GetValue(a, null)
                printfn "%s => %A" p.Name v))

  r :?> int