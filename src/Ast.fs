module Bfi.Ast

type Op =
  | Add of sbyte
  | Mov of int
  | Set of sbyte
  | Read
  | Write
  | Loop of Op list

// Cached instances to avoid large amount of allocations on big files
let inc = Add 1y
let dec = Add -1y
let movl = Mov -1
let movr = Mov 1
let set0 = Set 0y
