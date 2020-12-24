module Language.Brainfuck.Parser

open System
open Language.Brainfuck.IR

let rec parse' inComment scope stack line col (src: char ReadOnlySpan) =
  if src.IsEmpty
  then
    match stack with
    | [] -> Ok <| List.rev scope
    | (openLine, openCol, _) :: _ -> Error <| sprintf "(L%d, C%d): Unmatched '['." openLine openCol
  else
    let nextLine, nextCol, sliceAmount =
      if src.StartsWith(Environment.NewLine.AsSpan())
      then line + 1, 1, Environment.NewLine.Length
      else line, col + 1, 1

    let nextSrc = src.Slice(sliceAmount)

    match src.[0] with
    | '\r' | '\n' when inComment -> parse' false scope stack nextLine nextCol nextSrc
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
        | [] -> Error <| sprintf "(L%d, C%d): Unmatched ']'." line col
    | _ -> parse' false scope stack nextLine nextCol nextSrc

let parse (src: string) = parse' false [] [] 1 1 (src.AsSpan())
