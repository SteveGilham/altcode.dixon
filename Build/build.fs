namespace AltCode.Dixon

module Build =
  open Fake.Core
  open System
  open System.Reflection
  open System.Runtime.InteropServices

  [<assembly: CLSCompliant(true)>]
  [<assembly: ComVisible(false)>]
  [<assembly: AssemblyVersionAttribute("1.0.0.0")>]
  [<assembly: AssemblyFileVersionAttribute("1.0.0.0")>]
  ()

  [<EntryPoint>]
  let private main argv =
    use c =
      argv
      |> Array.toList
      |> Context.FakeExecutionContext.Create false "build.fsx"

    c
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext

    Targets.initTargets ()
    Target.runOrDefault <| Targets.defaultTarget ()

    0

[<assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Gendarme.Rules.Performance",
                                                            "OverrideValueTypeDefaultsRule",
                                                            Scope = "type", // TypeDefinition
                                                            Target =
                                                              "AltCode.Dixon.Actions/T_12Bytes@",
                                                            Justification =
                                                              "Compiler generated buffer type")>]
()