module Bfi.Parser

open System
open Bfi.Ast

type 'a span = 'a ReadOnlySpan

let inline next (src: char span) = src.Slice(1)

let rec parse' inComment scope stack (src: char span) =
  if src.IsEmpty then
    match stack with
    | [] -> List.rev scope
    | _ -> failwith "Unmatched '['"
  else
    match src.[0] with
    | '\r' | '\n' when inComment -> parse' false scope stack <| next src
    | _ when inComment -> parse' true scope stack <| next src
    | '#' -> parse' true scope stack <| next src
    
    | '+' -> parse' false (inc :: scope) stack <| next src
    | '-' -> parse' false (dec :: scope) stack <| next src
    | '<' -> parse' false (movl :: scope) stack <| next src
    | '>' -> parse' false (movr :: scope) stack <| next src
    | ',' -> parse' false (Read :: scope) stack <| next src
    | '.' -> parse' false (Write :: scope) stack <| next src
    | '[' -> parse' false [] (scope :: stack) <| next src
    | ']' ->
        match stack with
        | parent :: stack -> parse' false (Loop (List.rev scope) :: parent) stack <| next src
        | [] -> failwith "Unmatched ']'"
    | _ -> parse' false scope stack <| next src

let inline parse (src: string) = parse' false [] [] <| src.AsSpan()