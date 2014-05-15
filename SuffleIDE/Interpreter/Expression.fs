﻿module Interpreter.Expression

open System.Collections.Generic
open Suffle.Specification.Types
open Interpreter.ExceptionList

let lineNum = 0

type Interpreter () = 
    /// Current function context representation
    let vars = new Dictionary<string, Stack<Value>> ()
    let closure = new Dictionary<string, Stack<Value>> ()

    /// Returns given context as list
    let getContext (context: Dictionary<string, Stack<Value>>) = 
        let mutable toRet = []
        for x in context do
            toRet <- (x.Key, x.Value.Peek())::toRet
        toRet

    /// Sets closure context
    let setClosureContext (x: (string * Value) list) = 
        closure.Clear()
        for (key, value) in x do
            let stack = new Stack<Value>()
            stack.Push value
            closure.Add (key, stack)
        closure

    /// Add variable to context
    let addToContext (key, value) (context: Dictionary<string, Stack<Value>>) = 
        if not <| context.ContainsKey key then
            let tmp = new Stack<Value>()
            tmp.Push value
            context.Add (key, tmp)
        try
            (context.Item key)
                .Push value
        with
        | :? KeyNotFoundException -> raise (VariableNotFoundException (key, lineNum))
//        | :? System.ArgumentException -> raise (VariableNotFoundException (key, lineNum))

    /// Get variable from context
    let getFromContext name (context: Dictionary<string, Stack<Value>>) = 
        try 
            (context.Item name)
                .Peek()
        with
        //  Returns an exception to IDE to solve
        | :? KeyNotFoundException -> raise (VariableNotFoundException (name, lineNum))
        | :? System.ArgumentException -> raise (VariableNotFoundException (name, lineNum))

    /// Remove Variable from context
    let removeFromContext name (context: Dictionary<string, Stack<Value>>) =
        try
            let tmp = context.Item name
            tmp.Pop() |> ignore
        with
        | :? KeyNotFoundException -> ()
        | :? System.ArgumentNullException -> ()

    /// Return binary numeric (returning int) operator
    let retNumBin (op: BinaryOp): int -> int -> int = 
        match op with
        | BAdd -> (+)
        | BSub -> (-)
        | BDiv -> (/)
        | BMul -> (*)
        | _ -> raise (TypeMismatchException ("Wrong operation", lineNum))

    /// Return binary comparison operator
    let retBoolNumBin (op: BinaryOp): int -> int -> bool = 
        match op with
        | BEQ -> (=)
        | BNEQ -> (<>)
        | BGT -> (>)
        | BLT -> (<)
        | BNGT -> (<=)
        | BNLT -> (>=)
        | _ -> raise (TypeMismatchException ("Wrong operation", lineNum))

    /// Return binary boolean operator
    let retBoolBin (op: BinaryOp): bool -> bool -> bool = 
        match op with
        | BAnd -> (&&)
        | BOr -> (||)
        | _ -> raise (TypeMismatchException ("Wrong operation", lineNum))

    /// Evaluate closure
    let rec evalClosure (x: Value) (context: Dictionary<string, Stack<Value>>) =
        match x with
        | VClosure (_, expr) -> 
            evalExpr expr context
        | _ -> raise (TypeMismatchException ("Closure expected", lineNum))

    /// Evaluate type expression
    /// ETyped
    and evalType (x: Expression) (context: Dictionary<string, Stack<Value>>) = 
        //toWrite
        VUnit

    /// Evaluate identifier
    and evalIdent (id: EIdent) (context: Dictionary<string, Stack<Value>>) = 
        getFromContext id.Name context

    /// Evaluate literal
    and evalLiteral (liter: ELiteral) (_: Dictionary<string, Stack<Value>>) = 
        liter.Value
        
    /// Evaluate "if" statement 
    and evalIf (stmnt: EIfElse) (context: Dictionary<string, Stack<Value>>) = 
        match evalExpr stmnt.Cond context with
        | VBool cond -> evalExpr (if cond then stmnt.OnTrue else stmnt.OnFalse) context
        | _ -> raise (TypeMismatchException ("Bool expected", lineNum))
        
    /// Evaluate "let" stmnt
    //  TODO: Implement function declarations
    and evalLet (stmnt: ELetIn) (context: Dictionary<string, Stack<Value>>) = 
        match stmnt.Binding with
        | DValue x -> 
            //  Add to current context, eval, delete from context
            addToContext (x.Name.Name, (evalExpr x.Value context)) context
            let toRet = evalExpr stmnt.Body context
            removeFromContext x.Name.Name context
            toRet
        //  Maybe need to pass closure or lambda - dunno now, check on it later
        | DFunction x -> 
            addToContext (x.Name.Name, (evalExpr x.Body context)) context
            let toRet = evalExpr stmnt.Body context
            removeFromContext x.Name.Name context
            toRet
        | _ -> raise (TypeMismatchException ("Declaration expected", lineNum))
            
    /// Evaluate unary stmnt
    and evalUnary (stmnt: EUnary) (context: Dictionary<string, Stack<Value>>) = 
        let evaluated = evalExpr stmnt.Arg context
        match stmnt.Op with
        | UNeg ->   match evaluated with
                    | VInt x -> VInt -x
                    | _ -> raise (TypeMismatchException ("Int expected", lineNum))
        | UNot ->   match evaluated with
                    | VBool x -> VBool <| not x
                    | _ -> raise (TypeMismatchException ("Bool expected", lineNum))

    /// Evaluate binary statement
    and evalBinary (stmnt: EBinary) (context: Dictionary<string, Stack<Value>>) = 
        let evaluated1 = evalExpr stmnt.Arg1 context
        let evaluated2 = evalExpr stmnt.Arg2 context
        match evaluated1 with
        | VInt x -> match evaluated2 with
        //  FIXME: Must also evaluate bool statements. Mb need to know argument somewhere from outside (typecheck)
                    | VInt y -> VInt <| retNumBin stmnt.Op x y
                    | _ -> raise (TypeMismatchException ("Int expected", lineNum))
        | VBool x -> match evaluated2 with
                        | VBool y -> VBool<| retBoolBin stmnt.Op x y
                        | _ -> raise (TypeMismatchException ("Bool expected", lineNum))
        | _ -> raise (TypeMismatchException ("Int or Bool expected", lineNum))

    /// Evaluate lambda expression
    and evalLambda (stmnt: ELambda) (context: Dictionary<string, Stack<Value>>) = 
        VClosure (getContext context, ELambda stmnt)

    /// Evaluate function application
    and evalFunApp (stmnt: EFunApp) (context: Dictionary<string, Stack<Value>>) = 
        let arg = evalExpr stmnt.Arg context
        match stmnt.Func with
        //  TODO: Implement declared functions
        | EIdent id ->
            match getFromContext id.Name context with
            | VClosure (cont, ex) as it ->                 
                match ex with
                | ELambda x -> 
                    let closureContext = setClosureContext cont
                    addToContext (x.Arg.Name, arg) closureContext
                    evalClosure it closureContext
                | ECtor x ->
                //  TODO
                    VInt 5
                | _ -> evalExpr ex <| setClosureContext cont
            | _ -> raise (TypeMismatchException ("Closure exprected", lineNum))
        | ELambda x -> 
            let closure = evalLambda x context
            let currentContext = setClosureContext <| getContext context
            addToContext (x.Arg.Name, arg) currentContext
            evalClosure closure currentContext
        | ECtor x ->
            let ctor = evalConstr x context
            ctor
        | _ -> raise (TypeMismatchException ("Lambda function excpected", lineNum))


    /// Evaluate 'case ... of ...' expression
    and evalCaseOf (stmnt: ECaseOf) (context: Dictionary<string, Stack<Value>>) = 
        let matchable = evalExpr stmnt.Matching context
        VInt 5


    ///  Evaluate constructor application
    and evalConstr (stmnt: ECtor) (context: Dictionary<string, Stack<Value>>) = 
        VCtor (stmnt.CtorName, [ for ident in stmnt.Args -> getFromContext ident.Name context ])

    /// Evaluate expression
    and evalExpr (expr: Expression) (context: Dictionary<string, Stack<Value>>) = 
        match expr with
        | EIdent x -> evalIdent x context
        | ELiteral x -> evalLiteral x context
        | EIfElse x -> evalIf x context
        | ELetIn x -> evalLet x context
        | EBinary x -> evalBinary x context
        | EUnary x -> evalUnary x context
        | ELambda x -> evalLambda x context
        | EFunApp x -> evalFunApp x context
        | ECaseOf x -> evalCaseOf x context
        | _ -> failwith "Not Implemented Pattern"
