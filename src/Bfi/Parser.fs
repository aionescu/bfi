module Bfi.Parser

open System
open Bfi.Ast

type 'a span = 'a ReadOnlySpan

let inline skipComment (source: char span) =
  let cr = source.IndexOf('\r')
  let lf = source.IndexOf('\n')

  if cr = -1 && lf = -1 then
    span<char>.Empty
  else
    source.Slice(min cr lf)

let inline next (source: char span) = source.Slice(1)

let rec parse' scope stack (source: char span) =
  if source.IsEmpty then
    match stack with
    | [] -> List.rev scope
    | _ -> failwith "Unmatched '['"
  else
    match source.[0] with
    | '+' -> parse' (inc :: scope) stack <| next source
    | '-' -> parse' (dec :: scope) stack <| next source
    | '<' -> parse' (movl :: scope) stack <| next source
    | '>' -> parse' (movr :: scope) stack <| next source
    | ',' -> parse' (Read :: scope) stack <| next source
    | '.' -> parse' (Write :: scope) stack <| next source
    | '[' -> parse' [] (scope :: stack) <| next source
    | ']' ->
        match stack with
        | parent :: stack -> parse' (Loop (List.rev scope) :: parent) stack <| next source
        | [] -> failwith "Unmatched ']'"
    | '#' -> parse' scope stack <| skipComment source
    | _ -> parse' scope stack <| next source

let inline parse (source: string) = parse' [] [] <| source.AsSpan()