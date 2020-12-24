module Language.Brainfuck.Parser

open System
open Language.Brainfuck.IR

let err line col msg = Error <| sprintf "(L%d, C%d): %s" line col msg

let rec parse' inComment scope stack line col (src: char ReadOnlySpan) =
  if src.IsEmpty
  then
    match stack with
    | [] -> Ok <| List.rev scope
    | (openLine, openCol, _) :: _ -> err openLine openCol "Unmatched '['."
  else
    let nextLine, nextCol =
      if src.[0] = '\n'
      then line + 1, 1
      else line, col + 1

    let nextSrc = src.Slice(1)

    match src.[0] with
    | '\n' when inComment -> parse' false scope stack nextLine nextCol nextSrc
    | _ when inComment -> parse' true scope stack nextLine nextCol nextSrc
    | '#' -> parse' true scope stack nextLine nextCol nextSrc

    | '+' -> parse' false (incr :: scope) stack nextLine nextCol nextSrc
    | '-' -> parse' false (decr :: scope) stack nextLine nextCol nextSrc
    | '<' -> parse' false (moveL :: scope) stack nextLine nextCol nextSrc
    | '>' -> parse' false (moveR :: scope) stack nextLine nextCol nextSrc
    | ',' -> parse' false (read :: scope) stack nextLine nextCol nextSrc
    | '.' -> parse' false (write :: scope) stack nextLine nextCol nextSrc
    | '[' -> parse' false [] ((line, col, scope) :: stack) nextLine nextCol nextSrc
    | ']' ->
        match stack with
        | (_, _, parent) :: stack -> parse' false (Loop (List.rev scope) :: parent) stack nextLine nextCol nextSrc
        | [] -> err line col "Unmatched ']'."

    | c when Char.IsWhiteSpace c -> parse' false scope stack nextLine nextCol nextSrc
    | c -> err line col <| sprintf "Invalid character '%c'." c

let parse (src: string) = parse' false [] [] 1 1 (src.AsSpan())
