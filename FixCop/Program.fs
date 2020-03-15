open System
open System.IO
open System.Reflection
open Microsoft.VisualStudio.CodeAnalysis
open Microsoft.VisualStudio.CodeAnalysis.Common
open Microsoft.FxCop.Sdk
open System.Collections.Generic

[<EntryPoint>]
let main argv =
    let plat = argv
               |> Seq.find (fun a -> a.StartsWith("/platform:"))
    let platformPath = plat.Substring(10)

    let here = Assembly.GetExecutingAssembly().Location
    let files = here |> Path.GetDirectoryName
                |> Directory.GetFiles
                |> Seq.filter (fun f -> f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                |> Seq.sortDescending
                |> Seq.map (fun n -> Path.Combine ( here |> Path.GetDirectoryName, n)
                                     |> Assembly.LoadFile)

    // tracing
    let ca = Path.Combine ( here |> Path.GetDirectoryName, "Microsoft.VisualStudio.CodeAnalysis.dll")
                   |> Assembly.LoadFile
    let catrace = ca.GetType("Microsoft.VisualStudio.CodeAnalysis.Diagnostics.CATrace")
    let verbose = System.Diagnostics.TraceLevel.Verbose
    catrace.GetProperty("TraceLevel").SetValue(null, verbose)

    // interop info
    let cainterop = Path.Combine ( here |> Path.GetDirectoryName, "Microsoft.VisualStudio.CodeAnalysis.Interop.dll")
                   |> Assembly.LoadFile

    let info = cainterop.GetType("Microsoft.VisualStudio.CodeAnalysis.Common.AssemblyInfo")
    let getInfo = info.GetMethod("GetAssemblyInfo", BindingFlags.Public|||BindingFlags.Static)
    let props = info.GetProperties(BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance ||| BindingFlags.Static)

    // key assembly
    let cacommon = Path.Combine ( here |> Path.GetDirectoryName, "Microsoft.VisualStudio.CodeAnalysis.Common.dll")
                   |> Assembly.LoadFile

    // interesting types
    let platform = cacommon.GetType("Microsoft.VisualStudio.CodeAnalysis.Common.Platform")
    let unification = cacommon.GetType("Microsoft.VisualStudio.CodeAnalysis.Common.UnificationAssemblyNameMap")
    let ptype = typeof<PlatformInfo>.Assembly.GetType("Microsoft.VisualStudio.CodeAnalysis.PlatformType")
    let unknown = Enum.Parse(ptype, "Unknown")

    // interesting calls
    let makeUnify = unification.GetConstructor(BindingFlags.Public|||BindingFlags.Instance, null, [| |], null)
    let makePlatform = platform.GetConstructor(BindingFlags.NonPublic|||BindingFlags.Instance, null,
                           [| ptype; typeof<AssemblyName>; typeof<IAssemblyNameMap>; typeof<IEnumerable<string>>; typeof<string> |], null)
    let adder = unification.GetMethod("AddMapping", BindingFlags.Public|||BindingFlags.Instance)
    let platforms = platform.GetField("s_platforms", BindingFlags.Static ||| BindingFlags.NonPublic).GetValue(null) :?> System.Collections.IList
    let alt = platform.GetField("m_alternatePlatform", BindingFlags.NonPublic|||BindingFlags.Instance)

    // interesting platform assemblies
    let netstd2 = Path.Combine(platformPath, "netstandard.dll")
    let netstdlib = netstd2 |> AssemblyName.GetAssemblyName

    let core = Path.Combine(platformPath, "System.Private.CoreLib.dll")
    //let corelib = core |> AssemblyName.GetAssemblyName // throws, but name seems fixed anyway
    let corelib = AssemblyName("System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")

    // TODO -- environment names
    //let printInfo i =
    //    props
    //    |> Array.iter(fun p -> printfn "%s : %A" p.Name (p.GetValue(i, null)))

    let netinfo = getInfo.Invoke(null, [| netstd2 :> obj|])
    //printInfo netinfo
    let refs = (props
                |> Array.find (fun p -> p.Name = "AssemblyReferences" )).GetValue(netinfo, null) :?> IList<AssemblyName>
                |> Seq.sortBy (fun n -> n.Name)
                |> Seq.toArray

    let dirpath = netstd2 |> Path.GetDirectoryName
    let refpaths = Directory.GetFiles(platformPath, "*.dll")
    let cci = typeof<Identifier>.Assembly
    let tp = cci.GetType("Microsoft.FxCop.Sdk.TargetPlatform")
    let areff = tp.GetProperty("AssemblyReferenceFor").GetValue(null)
    let areffi = areff.GetType().GetMethod("set_Item",
                                           BindingFlags.Instance |||
                                           BindingFlags.Public |||
                                           BindingFlags.NonPublic,
                                           null,
                                           [| typeof<Int32>; typeof<obj> |],
                                           null)
    let areffi2 = areff.GetType().GetProperty("Item")
    let aref = cci.GetType("Microsoft.FxCop.Sdk.AssemblyReference")
    let build = aref.GetConstructor(BindingFlags.Instance |||
                                    BindingFlags.Public |||
                                    BindingFlags.NonPublic,
                                    null, [|typeof<AssemblyNode>|], null)

    let refnames = refpaths
                   |> Seq.map (fun p -> try
                                          let an = p |> AssemblyName.GetAssemblyName
                                          let key = Identifier.For(an.Name).UniqueIdKey
                                          let node = AssemblyNode.GetAssembly(p)
                                          let ar = build.Invoke( [| node :> obj |])
                                          areffi.Invoke(areff, [|
                                                  key :> obj
                                                  ar
                                               |]) |> ignore

                                          an |> Some
                                        with
                                        | _ -> None)
                   |> Seq.choose id
                   |> Seq.toArray

    let refmap = refs
                 |> Seq.map (fun n -> if n.Version.ToString() <> "0.0.0.0"
                                      then n
                                      else refnames
                                           |> Seq.find (fun r -> r.Name = n.Name))
                 |> Seq.toArray

    let uMap = Convert.ChangeType(makeUnify.Invoke([| |]), unification)

    refs
    |> Seq.zip refmap
    |> Seq.iter(fun r -> printfn "%A" r
                         adder.Invoke(uMap, [| fst r; snd r |]) |> ignore)
                         //adder.Invoke(uMap, [| snd r; fst r |]) |> ignore)
    adder.Invoke(uMap, [| corelib :> obj; netstdlib :> obj|]) |> ignore
    adder.Invoke(uMap, [| netstdlib :> obj; corelib :> obj|]) |> ignore

    let add = makePlatform.Invoke([| unknown; netstdlib; uMap; [dirpath] ; core |])
    platforms.Add add |> ignore

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

    let add = makePlatform.Invoke([| unknown; netstdlib; uMap; refpaths ; netstd2 |])
    let add2 = makePlatform.Invoke([| unknown; corelib; uMap; [dirpath] ; core |])
    let pi = platform.GetProperty("PlatformInfo")
    let pi1 = pi.GetValue(add) :?> PlatformInfo
    let pi2 = pi.GetValue(add2) :?> PlatformInfo
    let pin = typeof<PlatformInfo>.GetProperty("PlatformType", BindingFlags.NonPublic ||| BindingFlags.Instance)
    pin.SetValue(pi2, pin.GetValue(pi1))
    let piv = typeof<PlatformInfo>.GetProperty("PlatformVersion", BindingFlags.Public ||| BindingFlags.Instance)
    piv.SetValue(pi2, piv.GetValue(pi1))

    alt.SetValue(add, add2)
    alt.SetValue(add2, add)

    platforms.Add add |> ignore
    platforms.Add add2 |> ignore

    let fxcop = Path.Combine ( here |> Path.GetDirectoryName, "FxCopCmd.exe")
    let driven = fxcop
                 |> Assembly.LoadFile
    let command = driven.GetType("Microsoft.FxCop.Command.FxCopCommand")
    let main = command.GetMethod("Main", BindingFlags.Static ||| BindingFlags.Public)
    let r = main.Invoke(null, [| argv :> obj |])

    //[add; add2]
    //|> List.iter (fun a -> platform.GetProperties(BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance ||| BindingFlags.Static)
    //                       |> Seq.iter (fun p -> let v = p.GetValue(a, null)
    //                                             printfn "%s => %A" p.Name v))
    r:?> int 