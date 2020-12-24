module Main

open System.IO

open Language.Brainfuck.Parser
open Language.Brainfuck.Optimizer
open Language.Brainfuck.Codegen

let (>>=) a f = Result.bind f a
let (<&>) a f = Result.map f a

let openFile args =
  if Array.isEmpty args
  then Error "No input file specified."
  else
    try Ok <| File.ReadAllText args.[0]
    with e -> Error e.Message

let writeErrors = function
  | Ok _ -> 0
  | Error err ->
      printfn "Error: %s" err
      1

[<EntryPoint>]
let main argv =
  openFile argv
  >>= parse
  <&> (optimize >> compileAndRun)
  |> writeErrors
