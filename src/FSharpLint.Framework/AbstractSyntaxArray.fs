﻿(*
    FSharpLint, a linter for F#.
    Copyright (C) 2014 Matthew Mcveigh

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*)

namespace FSharpLint.Framework

module AbstractSyntaxArray =

    open System.Collections.Generic
    open Microsoft.FSharp.Compiler.Ast
    open Microsoft.FSharp.Compiler.Range
    open Microsoft.FSharp.Compiler.SourceCodeServices

    open Ast

    type SyntaxNode =
        | Identifier = 1uy
        | Null = 2uy
        | Expression = 3uy
        | FuncApp = 4uy
        | Unit = 5uy
        | AddressOf = 6uy
        
        | If = 10uy
        | Else = 11uy

        | Lambda = 20uy
        | LambdaArg = 21uy
        | LambdaBody = 22uy

        | ArrayOrList = 30uy
        | Tuple = 31uy

        | Wildcard = 41uy
            
        | ConstantBool = 51uy
        | ConstantByte = 52uy
        | ConstantChar = 53uy
        | ConstantDecimal = 54uy
        | ConstantDouble = 55uy
        | ConstantInt16 = 56uy
        | ConstantInt32 = 57uy
        | ConstantInt64 = 58uy
        | ConstantIntPtr = 59uy
        | ConstantSByte = 60uy
        | ConstantSingle = 61uy
        | ConstantString = 62uy
        | ConstantUInt16 = 63uy
        | ConstantUInt32 = 64uy
        | ConstantUInt64 = 65uy
        | ConstantUIntPtr = 66uy
        | ConstantBytes = 67uy
        
        | Cons = 101uy
        | And = 102uy
        | Or = 103uy

        | Other = 255uy

    let private constToSyntaxNode = function
        | SynConst.Unit(_) -> SyntaxNode.Unit
        | SynConst.Bool(_) -> SyntaxNode.ConstantBool
        | SynConst.Byte(_) -> SyntaxNode.ConstantByte
        | SynConst.Bytes(_) -> SyntaxNode.ConstantBytes
        | SynConst.Char(_) -> SyntaxNode.ConstantChar
        | SynConst.Decimal(_) -> SyntaxNode.ConstantDecimal
        | SynConst.Double(_) -> SyntaxNode.ConstantDouble
        | SynConst.Int16(_) -> SyntaxNode.ConstantInt16
        | SynConst.Int32(_) -> SyntaxNode.ConstantInt32
        | SynConst.Int64(_) -> SyntaxNode.ConstantInt64
        | SynConst.IntPtr(_) -> SyntaxNode.ConstantIntPtr
        | SynConst.SByte(_) -> SyntaxNode.ConstantSByte
        | SynConst.Single(_) -> SyntaxNode.ConstantSingle
        | SynConst.String(_) -> SyntaxNode.ConstantString
        | SynConst.UInt16(_) -> SyntaxNode.ConstantUInt16
        | SynConst.UInt32(_) -> SyntaxNode.ConstantUInt32
        | SynConst.UInt64(_) -> SyntaxNode.ConstantUInt64
        | SynConst.UIntPtr(_) -> SyntaxNode.ConstantUIntPtr
        | SynConst.UInt16s(_)
        | SynConst.UserNum(_)
        | SynConst.Measure(_) -> SyntaxNode.Other
        
    let private astNodeToSyntaxNode = function
        | Expression(SynExpr.Null(_)) -> SyntaxNode.Null
        | Expression(SynExpr.Tuple(_)) -> SyntaxNode.Tuple
        | Expression(SynExpr.ArrayOrListOfSeqExpr(_))
        | Expression(SynExpr.ArrayOrList(_)) -> SyntaxNode.ArrayOrList
        | Expression(SynExpr.AddressOf(_)) -> SyntaxNode.AddressOf
        | Identifier(_) -> SyntaxNode.Identifier
        | Expression(SynExpr.App(_)) -> SyntaxNode.FuncApp
        | Expression(SynExpr.Lambda(_)) -> SyntaxNode.Lambda
        | Expression(SynExpr.IfThenElse(_)) -> SyntaxNode.If
        | Expression(SynExpr.Const(constant, _)) -> constToSyntaxNode constant
        | Expression(SynExpr.Ident(_) | SynExpr.LongIdent(_) | SynExpr.LongIdentSet(_)) -> SyntaxNode.Other
        | Expression(_) -> SyntaxNode.Expression
        | Pattern(SynPat.Ands(_)) -> SyntaxNode.And
        | Pattern(SynPat.Or(_)) -> SyntaxNode.Or
        | Pattern(Cons(_)) -> 
            SyntaxNode.Cons
        | Pattern(SynPat.Wild(_)) -> SyntaxNode.Wildcard
        | Pattern(SynPat.Const(constant, _)) -> constToSyntaxNode constant
        | Pattern(SynPat.ArrayOrList(_)) -> SyntaxNode.ArrayOrList
        | Pattern(SynPat.Tuple(_)) -> SyntaxNode.Tuple
        | ModuleOrNamespace(_)
        | ModuleDeclaration(_)
        | AstNode.Binding(_)
        | ExceptionDefinition(_)
        | ExceptionRepresentation(_)
        | TypeDefinition(_)
        | TypeSimpleRepresentation(_)
        | AstNode.Field(_)
        | Type(_)
        | Match(_)
        | TypeParameter(_)
        | MemberDefinition(_)
        | Pattern(_)
        | ConstructorArguments(_)
        | SimplePattern(_)
        | SimplePatterns(_)
        | InterfaceImplementation(_)
        | TypeRepresentation(_)
        | AstNode.ComponentInfo(_)
        | AstNode.EnumCase(_)
        | AstNode.UnionCase(_) -> SyntaxNode.Other

    [<Struct>]
    type Node(hashcode: int, actual: AstNode) = 
        member __.Hashcode = hashcode
        member __.Actual = actual

    [<Struct>]
    type private PossibleSkip(skipPosition: int, depth: int) = 
        member __.SkipPosition = skipPosition
        member __.Depth = depth

    let private getHashCode node = 
        match node with
        | Identifier(x) when (List.isEmpty >> not) x -> x |> Seq.last |> hash
        | Pattern(SynPat.Const(SynConst.Bool(x), _))
        | Expression(SynExpr.Const(SynConst.Bool(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.Byte(x), _))
        | Expression(SynExpr.Const(SynConst.Byte(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.Bytes(x, _), _))
        | Expression(SynExpr.Const(SynConst.Bytes(x, _), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.Char(x), _))
        | Expression(SynExpr.Const(SynConst.Char(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.Decimal(x), _))
        | Expression(SynExpr.Const(SynConst.Decimal(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.Double(x), _))
        | Expression(SynExpr.Const(SynConst.Double(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.Int16(x), _))
        | Expression(SynExpr.Const(SynConst.Int16(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.Int32(x), _))
        | Expression(SynExpr.Const(SynConst.Int32(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.Int64(x), _))
        | Expression(SynExpr.Const(SynConst.Int64(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.IntPtr(x), _))
        | Expression(SynExpr.Const(SynConst.IntPtr(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.SByte(x), _))
        | Expression(SynExpr.Const(SynConst.SByte(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.Single(x), _))
        | Expression(SynExpr.Const(SynConst.Single(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.String(x, _), _))
        | Expression(SynExpr.Const(SynConst.String(x, _), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.UInt16(x), _))
        | Expression(SynExpr.Const(SynConst.UInt16(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.UInt16s(x), _))
        | Expression(SynExpr.Const(SynConst.UInt16s(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.UInt32(x), _))
        | Expression(SynExpr.Const(SynConst.UInt32(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.UInt64(x), _))
        | Expression(SynExpr.Const(SynConst.UInt64(x), _)) -> hash x
        | Pattern(SynPat.Const(SynConst.UIntPtr(x), _))
        | Expression(SynExpr.Const(SynConst.UIntPtr(x), _)) -> hash x
        | _ -> 0

    [<Struct>]
    type private StackedNode(node: Ast.Node, depth: int) = 
        member __.Node = node
        member __.Depth = depth

    [<Struct>]
    type Skip(numberOfChildren: int, parentIndex: int) = 
        member __.NumberOfChildren = numberOfChildren
        member __.ParentIndex = parentIndex

    /// Keep index of position so skip array can be created in the correct order.
    [<Struct>]
    type private TempSkip(numberOfChildren: int, parentIndex: int, index: int) = 
        member __.NumberOfChildren = numberOfChildren
        member __.Index = index
        member __.ParentIndex = parentIndex

    /// Contains information on the current node being visited.
    type CurrentNode =
        { Node: AstNode
          ChildNodes: AstNode list

          /// A list of parent nodes e.g. parent, grand parent, grand grand parent.
          Breadcrumbs: AstNode list

          /// Suppressed message attributes that have been applied to the block of code 
          /// the current node is within.
          SuppressedMessages: (SuppressedMessage * range) list }

        with
            /// Has a given rule been suppressed by SuppressMessageAttribute?
            member this.IsSuppressed(analyserName, ?rulename) =
                let isAnalyserSuppressed (analyser, _) =
                    analyser.Category = analyserName && 
                    (Option.exists ((=) analyser.Rule) rulename || analyser.Rule = "*")

                this.SuppressedMessages |> List.exists isAnalyserSuppressed

    /// Defines a function that visits a node on the AST.
    type Visitor = CurrentNode -> VisitorResult
    and 
        /// Defines a function that a visitor will return when it wants to supply 
        /// specific visitors for the node its visiting's children
        GetVisitorForChild = int -> AstNode -> Visitor option
    and 
        /// The return value of a visitor that lets the it specify how other nodes should be visited.
        /// Using partial application you can apply state to each visitor returned, 
        /// allowing for things such as summing the number of if statements in a function to be done purely.
        VisitorResult =
            /// Visit children with the current visitor.
            | Continue
            /// Do not visit any children.
            | Stop
            /// Enables state to be passed down to children.
            | ContinueWithVisitor of Visitor
            /// Enables state to be passed down to certain children.
            | ContinueWithVisitorsForChildren of GetVisitorForChild
        
    let astToArray ast visitor =
        let astRoot =
            match ast with
            | ParsedInput.ImplFile(ParsedImplFileInput(_,_,_,_,_,moduleOrNamespaces,_)) -> 
                ModuleOrNamespace moduleOrNamespaces.[0]
            | ParsedInput.SigFile _ -> failwith "Expected implementation file."
    
        let nodes = List<_>()
        let left = Stack<_>()
        let possibleSkips = Stack<PossibleSkip>()
        let skips = List<_>()

        let tryAddPossibleSkips depth =
            while possibleSkips.Count > 0 && possibleSkips.Peek().Depth >= depth do
                let nodePosition = possibleSkips.Pop().SkipPosition
                let numberOfChildren = nodes.Count - nodePosition - 1
                let parentIndex = if possibleSkips.Count > 0 then possibleSkips.Peek().SkipPosition else 0
                skips.Add(TempSkip(numberOfChildren, parentIndex, nodePosition))

        left.Push (StackedNode(Ast.Node(ExtraSyntaxInfo.None, astRoot), 0))

        while left.Count > 0 do
            let stackedNode = left.Pop()
            let astNode = stackedNode.Node.AstNode
            let depth = stackedNode.Depth
        
            tryAddPossibleSkips depth

            let children = traverseNode astNode
            children |> List.rev |> List.iter (fun node -> left.Push (StackedNode(node, depth + 1)))

            if stackedNode.Node.ExtraSyntaxInfo <> ExtraSyntaxInfo.None then
                possibleSkips.Push (PossibleSkip(nodes.Count, depth))

                let syntaxNode =
                    match stackedNode.Node.ExtraSyntaxInfo with
                    | ExtraSyntaxInfo.LambdaArg -> SyntaxNode.LambdaArg
                    | ExtraSyntaxInfo.LambdaBody -> SyntaxNode.LambdaBody
                    | ExtraSyntaxInfo.Else -> SyntaxNode.Else
                    | _ -> failwith ("Unknown extra syntax info: " + string stackedNode.Node.ExtraSyntaxInfo)

                nodes.Add (Node(Utilities.hash2 syntaxNode 0, astNode))

            match astNodeToSyntaxNode stackedNode.Node.AstNode with
            | SyntaxNode.Other -> ()
            | syntaxNode -> 
                possibleSkips.Push (PossibleSkip(nodes.Count, depth))

                nodes.Add (Node(Utilities.hash2 syntaxNode (getHashCode astNode), astNode))
        
        tryAddPossibleSkips 0

        let skipArray = Array.zeroCreate skips.Count

        let mutable i = 0
        while i < skips.Count do
            let skip = skips.[i]
            skipArray.[skip.Index] <- Skip(skip.NumberOfChildren, skip.ParentIndex)

            i <- i + 1

        (nodes.ToArray(), skipArray)

    /// Information for a file to be linted that is given to the visitors for them to analyse.
    type FileParseInfo =
        { /// Contents of the file.
          PlainText: string

          /// File represented as an AST.
          Ast: ParsedInput

          /// Optional results of inferring the types on the AST (allows for a more accurate lint).
          TypeCheckResults: FSharpCheckFileResults option

          /// Path to the file.
          File: string }

    /// Lint a file.
    let lintFile finishEarly fileInfo visitors =
        let visitorsWithTypeCheck = visitors |> List.map (fun visitor -> visitor fileInfo.TypeCheckResults)

        match fileInfo.Ast with
        | ParsedInput.ImplFile(ParsedImplFileInput(_,_,_,_,_,moduleOrNamespaces,_))-> 
            for moduleOrNamespace in moduleOrNamespaces do
                for visitor in visitorsWithTypeCheck do
                    ()
                    // TODO: walk finishEarly (ModuleOrNamespace(moduleOrNamespace)) visitor
        | ParsedInput.SigFile _ -> ()