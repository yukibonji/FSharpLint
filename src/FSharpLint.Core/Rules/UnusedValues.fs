namespace FSharpLint.Rules

module UnusedValues =
    
    open Microsoft.FSharp.Compiler.Ast
    open Microsoft.FSharp.Compiler.SourceCodeServices
    open FSharpLint.Framework
    open FSharpLint.Framework.Analyser
    open FSharpLint.Framework.Ast
    open FSharpLint.Framework.Configuration

    [<Literal>]
    let AnalyserName = "UnusedValues"

    let analyser (args: AnalyserArgs) : unit = 
        let syntaxArray, skipArray = args.SyntaxArray, args.SkipArray

        let isNotSuppressed i ruleName =
            AbstractSyntaxArray.getSuppressMessageAttributes syntaxArray skipArray i 
            |> AbstractSyntaxArray.isRuleSuppressed AnalyserName ruleName
            |> not

        for i = 0 to syntaxArray.Length - 1 do
            match syntaxArray.[i].Actual with
            | AstNode.Binding(SynBinding.Binding(_, _, _, _, _, _, _, pattern, _, _, _, _)) ->
                ()
            | _ -> ()