namespace AltCode.Dixon

open System
open System.Diagnostics.CodeAnalysis
open System.IO
open System.Reflection

open Microsoft.FxCop.Sdk

open AltCover.Shared

module Utilities =
  [<SuppressMessage("Gendarme.Rules.BadPractice",
                    "AvoidCallingProblematicMethodsRule",
                    Justification = "desperate cases, desperate remedy")>]
  let FindRuleClass assembly rule =
    let here =
      Assembly.GetExecutingAssembly().Location
      |> Path.GetDirectoryName

    let targetAssembly =
      Path.Combine(here, assembly)

    let source =
      Assembly.LoadFile(targetAssembly)

    let rule =
      source.GetTypes()
      |> Seq.find (fun t -> t.Name == rule)

    rule.GetConstructor(Type.EmptyTypes).Invoke([||]) :?> BaseIntrospectionRule

  // CA2001 : Remove the call to Assembly.LoadFile from 'Utilities.FindRuleClass(string, string)'.
  [<assembly: SuppressMessage("Microsoft.Reliability",
                              "CA2001:AvoidCallingProblematicMethods",
                              Scope = "member",
                              Target =
                                "AltCode.Dixon.Utilities.#FindRuleClass(System.String,System.String)",
                              MessageId = "System.Reflection.Assembly.LoadFile",
                              Justification = "desperate cases, desperate remedy")>]

  // TODO list
  //CA1709 : Microsoft.Naming : Correct the casing of 'get' in member name
  // 'Utilities.Member.get_IsFSharpCode(Member)' by changing it to 'Get'.
  [<assembly: SuppressMessage("Microsoft.Naming",
                              "CA1709:IdentifiersShouldBeCasedCorrectly",
                              Scope = "member",
                              Target =
                                "AltCode.Dixon.Utilities.#Member.get_IsFSharpCode(Microsoft.FxCop.Sdk.Member)",
                              MessageId = "get",
                              Justification = "> ten characters")>]
  //CA1704 : Microsoft.Naming : In method 'Utilities.Member.get_IsFSharpCode(Member)',
  //correct the spelling of 'param' in parameter name 'param0' or remove it entirely
  //if it represents any sort of Hungarian notation.
  [<assembly: SuppressMessage("Microsoft.Naming",
                              "CA1704:IdentifiersShouldBeSpelledCorrectly",
                              Scope = "member",
                              Target =
                                "AltCode.Dixon.Utilities.#Member.get_IsFSharpCode(Microsoft.FxCop.Sdk.Member)",
                              MessageId = "param",
                              Justification = "> ten characters")>]
  //CA1707 : Microsoft.Naming : Remove the underscores from member name
  //'Utilities.Member.get_IsFSharpCode(Member)'.
  [<assembly: SuppressMessage("Microsoft.Naming",
                              "CA1707:IdentifiersShouldNotContainUnderscores",
                              Scope = "member",
                              Target =
                                "AltCode.Dixon.Utilities.#Member.get_IsFSharpCode(Microsoft.FxCop.Sdk.Member)",
                              Justification = "> ten characters")>]
  //CA1709 : Microsoft.Naming : Correct the casing of 'get' in member name
  // 'Utilities.Object.get_IsNotNull(object)' by changing it to 'Get'.
  [<assembly: SuppressMessage("Microsoft.Naming",
                              "CA1709:IdentifiersShouldBeCasedCorrectly",
                              Scope = "member",
                              Target =
                                "AltCode.Dixon.Utilities.#Object.get_IsNotNull(System.Object)",
                              MessageId = "get",
                              Justification = "> ten characters")>]
  //CA1704 : Microsoft.Naming : In method 'Utilities.Object.get_IsNotNull(object)',
  //correct the spelling of 'param' in parameter name 'param0' or remove it entirely
  //if it represents any sort of Hungarian notation.
  [<assembly: SuppressMessage("Microsoft.Naming",
                              "CA1704:IdentifiersShouldBeSpelledCorrectly",
                              Scope = "member",
                              Target =
                                "AltCode.Dixon.Utilities.#Object.get_IsNotNull(System.Object)",
                              MessageId = "param",
                              Justification = "> ten characters")>]
  //CA1707 : Microsoft.Naming : Remove the underscores from member name
  // 'Utilities.Object.get_IsNotNull(object)'.
  [<assembly: SuppressMessage("Microsoft.Naming",
                              "CA1707:IdentifiersShouldNotContainUnderscores",
                              Scope = "member",
                              Target =
                                "AltCode.Dixon.Utilities.#Object.get_IsNotNull(System.Object)",
                              Justification = "> ten characters")>]
  ()

  type System.Object with
    member self.IsNotNull = self |> isNull |> not

  type Microsoft.FxCop.Sdk.Member with
    member self.HasAttribute typeName =
      self.Attributes
      |> Seq.exists (fun a -> a.Type.FullName == typeName)

    [<SuppressMessage("Gendarme.Rules.Globalization",
                      "PreferStringComparisonOverrideRule",
                      Justification = ".Contains overload not available at net472")>]
    member self.IsFSharpCode =
      self.IsNotNull
      && match self with
         | :? TypeNode as t ->
           (t.Name.Name.Contains("@")
            || t.HasAttribute "Microsoft.FSharp.Core.CompilationMappingAttribute")
         | _ -> self.DeclaringType.IsFSharpCode

  type Microsoft.FSharp.Core.Option<'T> with
    [<SuppressMessage("Gendarme.Rules.Naming",
                      "UseCorrectCasingRule",
                      Justification = "Idiomatic F# style")>]
    // fsharplint:disable-next-line MemberNames
    static member getOrElse (fallback: 'T) (x: option<'T>) = defaultArg x fallback

  // CA1709 : Correct the casing of 'get' in member name 'Utilities.Option`1.getOrElse.Static<T>(T, FSharpOption<T>)' by changing it to 'Get'.
  [<assembly: SuppressMessage("Microsoft.Naming",
                              "CA1709:IdentifiersShouldBeCasedCorrectly",
                              Scope = "member",
                              Target =
                                "AltCode.Dixon.Utilities.#Option`1.getOrElse.Static`1(!!0,Microsoft.FSharp.Core.FSharpOption`1<!!0>)",
                              MessageId = "get",
                              Justification = "to be fixed at source")>]
  // CA1704 : In method 'Utilities.Option`1.getOrElse.Static<T>(T, FSharpOption<T>)', correct the spelling of 'fallback' in parameter name 'fallback' or remove it entirely if it represents any sort of Hungarian notation.
  [<assembly: SuppressMessage("Microsoft.Naming",
                              "CA1704:IdentifiersShouldBeSpelledCorrectly",
                              Scope = "member",
                              Target =
                                "AltCode.Dixon.Utilities.#Option`1.getOrElse.Static`1(!!0,Microsoft.FSharp.Core.FSharpOption`1<!!0>)",
                              MessageId = "fallback",
                              Justification = "tto be fixed at source")>]
  // CA1704 : In method 'Utilities.Option`1.getOrElse.Static<T>(T, FSharpOption<T>)', consider providing a more meaningful name than parameter name 'x'.
  [<assembly: SuppressMessage("Microsoft.Naming",
                              "CA1704:IdentifiersShouldBeSpelledCorrectly",
                              Scope = "member",
                              Target =
                                "AltCode.Dixon.Utilities.#Option`1.getOrElse.Static`1(!!0,Microsoft.FSharp.Core.FSharpOption`1<!!0>)",
                              MessageId = "x",
                              Justification = "to be fixed at source")>]
  ()