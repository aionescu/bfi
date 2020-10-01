module Language.Brainfuck.Main

open System.IO

let (>>=) a f = Result.bind f a
let (<&>) a f = Result.map f a

let openFile args =
  if Array.isEmpty args then
    Error "No input file specified"
  else
    Ok <| File.ReadAllText args.[0]

let writeErrors = function
  | Ok _ -> 0
  | Error err ->
      printfn "Error: %s." err
      1

[<EntryPoint>]
let main argv =
  openFile argv
  >>= Parser.parse
  <&> Optimizer.optimize
  <&> Codegen.compile
  <&> Codegen.run
  |> writeErrors
