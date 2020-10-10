module Language.Brainfuck.Parser

open System
open Language.Brainfuck.AST

let rec parse' inComment scope stack pos (src: char ReadOnlySpan) =
  if src.IsEmpty then
    match stack with
    | [] -> Ok <| List.rev scope
    | (openPos, _) :: _ -> Error <| sprintf "Unmatched '[' at position %d." openPos
  else
    let nextPos = pos + 1
    let nextSrc = src.Slice(1)

    match src.[0] with
    | '\r' | '\n' when inComment -> parse' false scope stack nextPos nextSrc
    | _ when inComment -> parse' true scope stack nextPos nextSrc
    | '#' -> parse' true scope stack nextPos nextSrc
    
    | '+' -> parse' false (incr :: scope) stack nextPos nextSrc
    | '-' -> parse' false (decr :: scope) stack nextPos nextSrc
    | '<' -> parse' false (moveL :: scope) stack nextPos nextSrc
    | '>' -> parse' false (moveR :: scope) stack nextPos nextSrc
    | ',' -> parse' false (read :: scope) stack nextPos nextSrc
    | '.' -> parse' false (write :: scope) stack nextPos nextSrc
    | '[' -> parse' false [] ((pos, scope) :: stack) nextPos nextSrc
    | ']' ->
        match stack with
        | (_, parent) :: stack -> parse' false (Loop (List.rev scope) :: parent) stack nextPos nextSrc
        | [] -> Error <| sprintf "Unmatched ']' at position %d." pos
    | _ -> parse' false scope stack nextPos nextSrc

let parse (src: string) = parse' false [] [] 0 (src.AsSpan())
