module Language.Brainfuck.AST

type Op =
  | Add of sbyte
  | Move of int
  | Set of sbyte
  | Read
  | Write of int
  | WithOffset of int * Op
  | Loop of Op list

// Cached instances to avoid large amount of allocations on big files
let incr = Add 1y
let decr = Add -1y
let moveL = Move -1
let moveR = Move 1
let read = Read
let write = Write 1
let set0 = Set 0y
