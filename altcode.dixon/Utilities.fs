namespace AltCode.Dixon

open Microsoft.FxCop.Sdk

// TODO list
//CA1709 : Microsoft.Naming : Correct the casing of 'get' in member name 'Utilities.Member.get_IsFSharpCode(Member)' by changing it to 'Get'.
//CA1704 : Microsoft.Naming : In method 'Utilities.Member.get_IsFSharpCode(Member)', correct the spelling of 'param' in parameter name 'param0' or remove it entirely if it represents any sort of Hungarian notation.
//CA1707 : Microsoft.Naming : Remove the underscores from member name 'Utilities.Member.get_IsFSharpCode(Member)'.
//CA1709 : Microsoft.Naming : Correct the casing of 'get' in member name 'Utilities.Object.get_IsNotNull(object)' by changing it to 'Get'.
//CA1704 : Microsoft.Naming : In method 'Utilities.Object.get_IsNotNull(object)', correct the spelling of 'param' in parameter name 'param0' or remove it entirely if it represents any sort of Hungarian notation.
//CA1707 : Microsoft.Naming : Remove the underscores from member name 'Utilities.Object.get_IsNotNull(object)'.

module Utilities =
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