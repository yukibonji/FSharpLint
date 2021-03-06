﻿module TestRedundantNewKeyword

open NUnit.Framework
open FSharpLint.Rules.RedundantNewKeyword
open FSharpLint.Framework.Configuration

let config = 
    let ruleEnabled = { Rule.Settings = Map.ofList [ ("Enabled", Enabled(true)) ] }

    Map.ofList 
        [ (AnalyserName, { Rules = Map.empty; Settings = Map.ofList [ ("Enabled", Enabled(true)) ] }) ]
              
[<TestFixture>]
type TestRedundantNewKeyword() =
    inherit TestRuleBase.TestRuleBase(analyser, config)

    [<Test>]
    member this.``Lint gives suggestion when new keyword is not required.``() = 
        this.Parse("""
module Program

let _ = new System.Version()""", checkInput = true)

        Assert.IsTrue(this.ErrorExistsAt(4, 8))

    [<Test>]
    member this.``RedundantNewKeyword analyser does not offer suggestions when suppressed.``() = 
        this.Parse("""
module Program

[<System.Diagnostics.CodeAnalysis.SuppressMessage("RedundantNewKeyword", "*")>]
let _ = new System.Version()""", checkInput = true)

        this.AssertNoWarnings()

    [<Test>]
    member this.``New keyword not considered unnecassery if used with a constructor of a type which implements IDisposable.``() = 
        this.Parse("""
module Program

let _ = new System.IO.MemoryStream()""", checkInput = true)

        this.AssertNoWarnings()

    [<Test>]
    member this.``Quick fix for unnecassery new keyword.``() =
        let source = """
module Program

let _ = new System.Version()"""
 
        let expected = """
module Program

let _ = System.Version()"""
 
        this.Parse(source, checkInput = true)
        Assert.AreEqual(expected, this.ApplyQuickFix source)