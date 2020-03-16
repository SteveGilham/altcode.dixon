namespace AltCode.Dixon.Tests

open System
open System.Reflection
open Microsoft.FxCop.Sdk
open NUnit.Framework
open AltCode.Dixon.Design

module Exceptions =
  [<Test>]
  let JustThrowTest() =
    let subject = new AltCode.Dixon.TestData.Exceptions()
    let subjectNode = Utilities.GetType subject

    let offendingMethod = Utilities.GetMethod subjectNode "Fail1"

    let ruleUnderTest = new ReraiseCorrectlyRule()
    let problems = ruleUnderTest.Check(offendingMethod)

    Assert.That(problems.Count, Is.EqualTo(0))

  [<Test>]
  let NoReThrowTest() =
    let subject = new AltCode.Dixon.TestData.Exceptions()
    let subjectNode = Utilities.GetType subject

    let offendingMethod = Utilities.GetMethod subjectNode "Fail2"

    let ruleUnderTest = new ReraiseCorrectlyRule()
    let problems = ruleUnderTest.Check(offendingMethod)

    Assert.That(problems.Count, Is.EqualTo(1))
    let problem = problems.[0].Resolution
    Assert.That(problem.Name, Is.EqualTo("preserveStackDetails"))

  let myType () =
    let assembly = AssemblyNode.GetAssembly(Assembly.GetExecutingAssembly().Location)
    assembly.GetType(Identifier.For("AltCode.Dixon.Tests"), Identifier.For("Exceptions"))

  [<Test>]
  let JustRaiseTest() =
    let subjectNode = myType()

    let offendingMethod = Utilities.GetMethod subjectNode "Fail"

    let ruleUnderTest = new ReraiseCorrectlyRule()
    let problems = ruleUnderTest.Check(offendingMethod)

    Assert.That(problems.Count, Is.EqualTo(0))

  [<Test>]
  let JustRaise0Test() =
    let subjectNode = myType()

    let offendingMethod = Utilities.GetMethod subjectNode "Fail0"

    let ruleUnderTest = new ReraiseCorrectlyRule()
    let problems = ruleUnderTest.Check(offendingMethod)

    Assert.That(problems.Count, Is.EqualTo(0))

  [<Test>]
  let JustRaise1Test() =
    let subjectNode = myType()

    let offendingMethod = Utilities.GetMethod subjectNode "Fail1"

    let ruleUnderTest = new ReraiseCorrectlyRule()
    let problems = ruleUnderTest.Check(offendingMethod)

    Assert.That(problems.Count, Is.EqualTo(0))

  [<Test>]
  let NoReRaiseTest() =
    let subjectNode = myType()

    let offendingMethod = Utilities.GetMethod subjectNode "Fail2"

    let ruleUnderTest = new ReraiseCorrectlyRule()
    let problems = ruleUnderTest.Check(offendingMethod)

    Assert.That(problems.Count, Is.EqualTo(1))
    let problem = problems.[0].Resolution
    Assert.That(problem.Name, Is.EqualTo("preserveStackDetails"))

  [<Test>]
  let NoReRaiseTest2() =
    let subjectNode = myType()

    let offendingMethod = Utilities.GetMethod subjectNode "Fail3"

    let ruleUnderTest = new ReraiseCorrectlyRule()
    let problems = ruleUnderTest.Check(offendingMethod)

    Assert.That(problems.Count, Is.EqualTo(1))
    let problem = problems.[0].Resolution
    Assert.That(problem.Name, Is.EqualTo("preserveStackDetails"))

  [<Test>]
  let SafeReThrowTest() =
    let subject = new AltCode.Dixon.TestData.Exceptions()
    let subjectNode = Utilities.GetType subject

    let offendingMethod = Utilities.GetMethod subjectNode "FailSafe"

    let ruleUnderTest = new ReraiseCorrectlyRule()
    let problems = ruleUnderTest.Check(offendingMethod)

    Assert.That(problems.Count, Is.EqualTo(0))

  [<Test>]
  let SafeReRaiseTest() =
    let subjectNode = myType()

    let offendingMethod = Utilities.GetMethod subjectNode "FailSafe"

    let ruleUnderTest = new ReraiseCorrectlyRule()
    let problems = ruleUnderTest.Check(offendingMethod)

    Assert.That(problems.Count, Is.EqualTo(0))

  let Fail () =
    raise (InvalidOperationException())

  let Fail0 () =
    InvalidOperationException() |> raise

  let Fail1 () =
    raise <| InvalidOperationException()

  let Fail2 () =
    try
      Fail0()
    with
    | x -> printfn "%A" x
           x |> raise

  let Fail3 () =
    try
      Fail1()
    with
    | x -> printfn "%A" x
           raise x

  let FailSafe () =
    try
      Fail1()
    with
    | x -> printfn "%A" x
           reraise()