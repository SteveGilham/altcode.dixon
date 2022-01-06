open System
open Mono.Cecil

let netst = AssemblyDefinition.ReadAssembly @"C:\Program Files\dotnet\sdk\6.0.101\ref\netstandard.dll"
netst.MainModule.GetTypes()
|> Seq.filter (fun t -> t.IsNested |> not)
|> Seq.filter (fun t -> t.FullName <> "<Module>")
|> Seq.iter (fun t -> let n0 = t.FullName
                      let n = if t.HasGenericParameters
                              then
                                let i = n0.IndexOf('`')
                                let stem = n0.Substring(0, i)
                                let tag = "<" + String.Join (",",
                                                             t.GenericParameters
                                                             |> Seq.map(fun p -> String.Empty)) + ">"
                                stem + tag
                              else
                                n0

                      printfn "[assembly: TypeForwardedTo(typeof(%s))]" n)