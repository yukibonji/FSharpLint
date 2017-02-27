module TestUnusedValues

open NUnit.Framework
open FSharpLint.Rules.UnusedValues
open FSharpLint.Framework.Configuration

let config = 
    let ruleEnabled = { Rule.Settings = Map.ofList [ ("Enabled", Enabled(true)) ] }

    Map.ofList 
        [ (AnalyserName, { Rules = Map.empty; Settings = Map.ofList [ ("Enabled", Enabled(true)) ] }) ]
              
[<TestFixture>]
type TestUnusedValues() =
    inherit TestRuleBase.TestRuleBase(analyser, config)

    [<Test>]
    member this.``Suggestion when local binding unused.``() = 
        this.Parse("""
module Program

let foo () = 
    let bar = 1 + 1
    ()""")

        Assert.IsTrue(this.ErrorExistsAt(4, 8))

    [<Test>]
    member this.``No suggestion when local binding is used.``() = 
        this.Parse("""
module Program

let foo () = 
    let bar = 1 + 1
    bar""")

        this.AssertNoWarnings()