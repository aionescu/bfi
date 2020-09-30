module Language.Brainfuck.Optimizer

open Language.Brainfuck.AST

[<Literal>]
let maxPasses = 64

let rec optimizeOnce' changed acc ops =
  match ops with
  | Add 0y :: rest
  | Mov 0 :: rest -> optimizeOnce' true acc rest

  | Loop [Add -1y] :: rest
  | Loop [Add 1y] :: rest -> optimizeOnce' true acc <| set0 :: rest

  | Add _ :: (Read :: _ as rest)
  | Set _ :: (Read :: _ as rest)
  | Add _ :: (Set _ :: _ as rest)
  | Set _ :: (Set _ :: _ as rest) -> optimizeOnce' true acc rest

  | Set s :: Add a :: rest -> optimizeOnce' true acc <| Set (s + a) :: rest

  | Add a :: Add b :: rest -> optimizeOnce' true acc <| Add (a + b) :: rest
  | Mov a :: Mov b :: rest -> optimizeOnce' true acc <| Mov (a + b) :: rest

  | Set 0y as s :: Loop _ :: rest -> optimizeOnce' true acc <| s :: rest

  | Loop _ as l :: Loop _ :: rest
  | Loop [Loop _ as l] :: rest -> optimizeOnce' true acc <| l :: rest

  | Loop ops :: rest ->
      let changed, ops = optimizeOnce' false [] ops
      optimizeOnce' changed (Loop ops :: acc) rest

  | op :: rest -> optimizeOnce' changed (op :: acc) rest
  | [] -> (changed, List.rev acc)

let inline optimizeOnce ops = optimizeOnce' false [] ops

let rec optimize' passesLeft ops =
  if passesLeft = 0 then
    ops
  else
    let changed, ops = optimizeOnce ops

    if not changed then
      ops
    else
      optimize' (passesLeft - 1) ops 
  
let inline optimize ops = optimize' maxPasses (set0 :: ops)
