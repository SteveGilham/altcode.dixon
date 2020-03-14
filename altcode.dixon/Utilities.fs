namespace AltCode.Dixon

open Microsoft.FxCop.Sdk
open System.Diagnostics.CodeAnalysis

module Utilities =
  // TODO list
  //CA1709 : Microsoft.Naming : Correct the casing of 'get' in member name
  // 'Utilities.Member.get_IsFSharpCode(Member)' by changing it to 'Get'.
  [<assembly: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
    Scope="member", Target="AltCode.Dixon.Utilities.#Member.get_IsFSharpCode(Microsoft.FxCop.Sdk.Member)",
    MessageId="get", Justification="")>]
  //CA1704 : Microsoft.Naming : In method 'Utilities.Member.get_IsFSharpCode(Member)',
  //correct the spelling of 'param' in parameter name 'param0' or remove it entirely
  //if it represents any sort of Hungarian notation.
  [<assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly",
    Scope="member", Target="AltCode.Dixon.Utilities.#Member.get_IsFSharpCode(Microsoft.FxCop.Sdk.Member)",
    MessageId="param", Justification="")>]
  //CA1707 : Microsoft.Naming : Remove the underscores from member name
  //'Utilities.Member.get_IsFSharpCode(Member)'.
  [<assembly: SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores",
    Scope="member", Target="AltCode.Dixon.Utilities.#Member.get_IsFSharpCode(Microsoft.FxCop.Sdk.Member)", Justification="")>]
  //CA1709 : Microsoft.Naming : Correct the casing of 'get' in member name
  // 'Utilities.Object.get_IsNotNull(object)' by changing it to 'Get'.
  [<assembly: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
    Scope="member", Target="AltCode.Dixon.Utilities.#Object.get_IsNotNull(System.Object)",
    MessageId="get", Justification="")>]
  //CA1704 : Microsoft.Naming : In method 'Utilities.Object.get_IsNotNull(object)',
  //correct the spelling of 'param' in parameter name 'param0' or remove it entirely
  //if it represents any sort of Hungarian notation.
  [<assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly",
    Scope="member", Target="AltCode.Dixon.Utilities.#Object.get_IsNotNull(System.Object)",
    MessageId="param", Justification="")>]
  //CA1707 : Microsoft.Naming : Remove the underscores from member name
  // 'Utilities.Object.get_IsNotNull(object)'.
  [<assembly: SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores",
    Scope="member", Target="AltCode.Dixon.Utilities.#Object.get_IsNotNull(System.Object)", Justification="")>]
    ()

  type System.Object with
    member self.IsNotNull
      with get() =
        self |> isNull |> not

  type Microsoft.FxCop.Sdk.Member with
    member self.HasAttribute typeName =
      self.Attributes
      |> Seq.exists(fun a -> a.Type.FullName = typeName)

    member self.IsFSharpCode
      with get() =
        self.IsNotNull &&
        match self with
        | :? TypeNode as t ->
          (t.Name.Name.Contains("@") ||
           t.HasAttribute "Microsoft.FSharp.Core.CompilationMappingAttribute")
        | _ -> self.DeclaringType.IsFSharpCode