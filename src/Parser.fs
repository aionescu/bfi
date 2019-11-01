module Bfi.Parser

open Bfi.Ast

let parse (source: string) =
  let mutable stack = []
  let mutable scope = []
  let mutable inComment = false
  let mutable idx = 0

  for c in source do
    match c with
    | '\n' | '\r' -> inComment <- false
    | '#' -> inComment <- true
    | _ when inComment -> ()

    | '+' -> scope <- inc :: scope
    | '-' -> scope <- dec :: scope
    | '<' -> scope <- movl :: scope
    | '>' -> scope <- movr :: scope
    | ',' -> scope <- Read :: scope
    | '.' -> scope <- Write :: scope

    | '[' ->
        stack <- (idx, scope) :: stack
        scope <- []

    | ']' ->
        match stack with
        | [] -> failwithf "Unmatched ']' at index %d" idx
        | (_, parent) :: stack' ->
            scope <- Loop (List.rev scope) :: parent
            stack <- stack'

    | _ -> ()

    idx <- idx + 1

  match stack with
  | (idx, _) :: _ -> failwithf "Unmatched '[' at index %d" idx
  | [] -> List.rev scope