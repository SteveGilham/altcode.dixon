// A port of the well travelled example rule to ensure that
// SuppressMessage attributes include a justification.  See e.g.
// http://www.binarycoder.net/fxcop/html/ex_specifysuppressmessagejustification.html
// StyleCop will do this for you in C# code, but as an FxCop rule it's language neutral

namespace AltCode.Dixon.Design

open System

open Microsoft.FxCop.Sdk

type JustifySuppressionRule =
  class
    inherit BaseIntrospectionRule

    // Default constructor as required
    new() =
      { inherit BaseIntrospectionRule("JustifySuppression", "altcode.dixon.Dixon.Design",
                                      typeof<JustifySuppressionRule>.Assembly) }

    // Overrides of every Check method
    override self.Check(``type`` : TypeNode) =
      self.VerifyAttributes ``type``.Attributes ``type``

    override self.Check(``member`` : Member) =
      self.VerifyAttributes ``member``.Attributes ``member``

    override self.Check(``module`` : ModuleNode) =
      self.VerifyAttributes ``module``.Attributes ``module``

    override self.Check(parameter : Parameter) =
      self.VerifyAttributes parameter.Attributes parameter

    // Common code to scan each node for its attributes and check justifications
    member private self.VerifyAttributes attributes (context : Node) =
      attributes
      |> Seq.cast<AttributeNode>
      |> Seq.filter
           (fun attribute -> attribute.Type = FrameworkTypes.SuppressMessageAttribute)
      |> Seq.iter (self.CheckJustification context)
      self.Problems

    // Separates sheep from goats so far as Justification strings go
    member private self.CheckJustification context (attribute : AttributeNode) =
      match attribute.GetNamedArgument(Identifier.For("Justification")) with
      | :? Literal as lit ->
          match lit.Value :?> string with
          | y when String.IsNullOrWhiteSpace(y) -> self.Violation context
          | x when x.Trim().Length < 10 -> self.Violation context
          | _ -> ()

      | _ -> self.Violation context

    member private self.Violation context =
      self.Problems.Add
        (new Problem(self.GetNamedResolution
                       ("justificationAbsent",

                        // Extract an appropriate string
                        match context with
                        | :? Member as m -> m.FullName // Method or Type
                        | :? Parameter as p ->
                            p.Name.ToString() + " of " + p.DeclaringMethod.FullName
                        | :? ModuleNode as o -> o.Name
                        | q -> q.ToString() // Not reachable

                       ), context))
  end