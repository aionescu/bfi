module Language.Brainfuck.Optimizer

open Language.Brainfuck.IR

let maxPasses = 64

let canDoWithOffset = function
  | Move _ | Loop _ -> false
  | _ -> true

let rec optimizeOnce' changed acc ops =
  match ops with
  | Add 0y :: rest
  | Move 0 :: rest -> optimizeOnce' true acc rest

  | Loop [Add -1y] :: rest
  | Loop [Add 1y] :: rest -> optimizeOnce' true acc <| set0 :: rest

  | Add _ :: (Read :: _ as rest)
  | Set _ :: (Read :: _ as rest)
  | Add _ :: (Set _ :: _ as rest)
  | Set _ :: (Set _ :: _ as rest) -> optimizeOnce' true acc rest

  | Set s :: Add a :: rest -> optimizeOnce' true acc <| Set (s + a) :: rest

  | Add a :: Add b :: rest -> optimizeOnce' true acc <| Add (a + b) :: rest
  | Move a :: Move b :: rest -> optimizeOnce' true acc <| Move (a + b) :: rest
  | Write a :: Write b :: rest -> optimizeOnce' true acc <| Write (a + b) :: rest

  | Set 0y as s :: Loop _ :: rest -> optimizeOnce' true acc <| s :: rest

  | Move m :: op :: Move n :: rest
    when canDoWithOffset op && m = -n ->
      match op with
        | WithOffset (o, op') -> optimizeOnce' true acc <| WithOffset (m + o, op') :: rest
        | _ -> optimizeOnce' true acc <| WithOffset (m, op) :: rest

  | Loop _ as l :: Loop _ :: rest
  | Loop [Loop _ as l] :: rest -> optimizeOnce' true acc <| l :: rest

  | Loop ops :: rest ->
      let changed, ops = optimizeOnce' false [] ops
      optimizeOnce' changed (Loop ops :: acc) rest

  | op :: rest -> optimizeOnce' changed (op :: acc) rest
  | [] -> (changed, List.rev acc)

let optimizeOnce = optimizeOnce' false []

let rec optimize' passesLeft ops =
  if passesLeft = 0
  then ops
  else
    let changed, ops = optimizeOnce ops

    if not changed
    then ops
    else optimize' (passesLeft - 1) ops

let optimize ops = optimize' maxPasses (set0 :: ops)
