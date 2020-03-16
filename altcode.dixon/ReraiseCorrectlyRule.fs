namespace AltCode.Dixon.Design

open System
open System.Diagnostics.CodeAnalysis

open Microsoft.FxCop.Sdk

open AltCode.Dixon.Utilities

[<SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly",
  MessageId="Reraise", Justification = "'Raise' is the F# for 'throw'")>]
type ReraiseCorrectlyRule  = class
  inherit BaseIntrospectionRule

  // Default constructor as required
  new() = { inherit BaseIntrospectionRule(
                                "ReraiseCorrectly",
                                "altcode.dixon.Dixon.Design",
                                typeof<ReraiseCorrectlyRule>.Assembly);
          }

  override self.Check (``member`` : Member) =
    match ``member`` with
    | :? Method as fn ->
      if fn.IsFSharpCode
      then
        // allowed pattern is newobj (stloc, ldloc)*(0..2) throw
        let indexable = fn.Instructions |> Seq.toArray

        let throws = indexable
                     |> Seq.mapi (fun index instr -> if instr.OpCode = OpCode.Throw
                                                     then Some index
                                                     else None)
                     |> Seq.choose id
                     |> Seq.toList

        let local (value:obj) =
          match value with
          | :? Local as l ->
                if l.Name.Name.StartsWith("local$", StringComparison.Ordinal)
                then let (a,b) = Int32.TryParse(l.Name.Name.Substring(6))
                     if a then Some b else None
                else None
          | _ -> None

        let stloc (instr : Instruction) =
          match instr.OpCode with
          | OpCode.Stloc_0 -> Some 0
          | OpCode.Stloc_1 -> Some 1
          | OpCode.Stloc_2 -> Some 2
          | OpCode.Stloc_3 -> Some 3
          | OpCode.Stloc
          | OpCode.Stloc_S -> instr.Value |> local
          | _ -> None

        let ldloc (instr : Instruction) =
          match instr.OpCode with
          | OpCode.Ldloc_0 -> Some 0
          | OpCode.Ldloc_1 -> Some 1
          | OpCode.Ldloc_2 -> Some 2
          | OpCode.Ldloc_3 -> Some 3
          | OpCode.Ldloc
          | OpCode.Ldloc_S -> instr.Value |> local
          | _ -> None

        if throws
           |> List.exists(fun i -> if indexable.[i-1].OpCode = OpCode.Newobj
                                   then false
                                   else if i > 2 &&
                                        indexable.[i-3].OpCode = OpCode.Newobj
                                        then match (stloc indexable.[i-2], ldloc indexable.[i-1]) with
                                        | (Some x, Some y) when x = y -> false
                                        | _ -> true
                                   else if i > 4 &&
                                        indexable.[i-5].OpCode = OpCode.Newobj
                                        then match (stloc indexable.[i-4],
                                                    ldloc indexable.[i-3],
                                                    stloc indexable.[i-2],
                                                    ldloc indexable.[i-1]) with
                                        | (Some x, Some y, Some a, Some b) when x = y && a = b -> false
                                        | _ -> true
                                   else true)
        then self.Problems.Add(new Problem(self.GetNamedResolution("preserveStackDetails")))

      else self.VisitBlock(fn.Body)
    | _ -> ()
    self.Problems

  [<SuppressMessage("Gendarme.Rules.Maintainability",
                    "AvoidUnnecessarySpecializationRule",
                    Justification="No premature/pointless generalization")>]
  member private self.MustBeConstructor (expression : Expression) =
    match expression.NodeType with
      | NodeType.Construct -> ()
      | _ ->
        self.Problems.Add(
          new Problem(self.GetNamedResolution("preserveStackDetails")))

  override self.VisitThrow(throwInstruction : ThrowNode) =
    match throwInstruction.NodeType with
    | NodeType.Throw -> self.MustBeConstructor throwInstruction.Expression
    | _ -> ()
    base.VisitThrow(throwInstruction)

  override self.VisitExpression(expression : Expression) =
    if expression.IsNotNull && expression.NodeType = NodeType.Call then
       let call = expression :?> MethodCall
       match call.Callee with
       | :? MemberBinding as b ->
            if b.BoundMember.FullName.StartsWith("Microsoft.FSharp.Core.Operators.raise<",
                                                 StringComparison.Ordinal)
             then self.MustBeConstructor call.Operands.[0]
       | _ -> ()

    base.VisitExpression(expression)

end