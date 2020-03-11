namespace altcode.dixon.tests

open Microsoft.FxCop.Sdk
open NUnit.Framework
open AltCode.Dixon.Design

module Utilities =
  let GetType (o:obj) =
    let t = o.GetType()
    let assembly = AssemblyNode.GetAssembly(t.Module.Assembly.Location)
    assembly.GetType(Identifier.For(t.Namespace),
                     Identifier.For(t.Name))

  let GetMethod (t:TypeNode) (name:string) =
    t.Members
    |> Seq.find (fun x -> x.Name.Name = name)

module Justifications =
  [<SetUp>]
  let Setup () =
      ()

  [<Test>]
  let NoJustificationTest() =
    let subject = new AltCode.Dixon.TestData.Justifications();
    let subjectNode = Utilities.GetType subject

    let offendingMethod = Utilities.GetMethod subjectNode "Token"

    let ruleUnderTest = new JustifySuppressionRule()
    let problems = ruleUnderTest.Check(offendingMethod)

    Assert.That(problems.Count, Is.EqualTo(1))
    let problem = problems.[0].Resolution
    Assert.That(problem.Name, Is.EqualTo("justificationAbsent"))