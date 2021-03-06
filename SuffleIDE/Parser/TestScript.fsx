﻿
#r @"M:\projects\Suffle\SuffleIDE\Parser\bin\Debug\Specification.dll"                    
#r @"M:\projects\Suffle\SuffleIDE\packages\FParsec.1.0.1\lib\net40-client\FParsecCS.dll"
#r @"M:\projects\Suffle\SuffleIDE\packages\FParsec.1.0.1\lib\net40-client\FParsec.dll"

//#r @"C:\Users\Сергей\SkyDrive\Documents\Visual Studio 2013\Projects\Suffle\SuffleIDE\packages\FParsec.1.0.1\lib\net40-client\FParsecCS.dll"
//#r @"C:\Users\Сергей\SkyDrive\Documents\Visual Studio 2013\Projects\Suffle\SuffleIDE\packages\FParsec.1.0.1\lib\net40-client\FParsec.dll"
//#r @"C:\Users\Сергей\SkyDrive\Documents\Visual Studio 2013\Projects\Suffle\SuffleIDE\Specification\bin\Debug\Specification.dll"

open FParsec
open Suffle.Specification.Syntax  
open Suffle.Specification.Types     

#load "Auxiliary.fs"
#load "Literals.fs"          
#load "Types.fs"
#load "Pattern.fs" 
#load "Unary.fs"
#load "Binary.fs"
#load "Structures.fs"
#load "Parser.fs"

open Parser.Auxiliary
open Parser.Literals     
open Parser.Types
open Parser.Pattern
open Parser.Structures
open Suffle.Parser

let run' p = run (_ws p .>> eof)


let t2 = parse """

fun :: ('a -> 'a -> bool) -> List 'a -> List 'a -> List 'a
merge pred xs ys =
    case (P xs ys) of
    | P xs [] -> xs
    | P [] ys -> ys
    | P (x:xs) (y:ys) -> if (pred x y) then (x : (merge pred xs (y:ys))) else (y : (merge pred (x:xs) ys)) end
    end

"""