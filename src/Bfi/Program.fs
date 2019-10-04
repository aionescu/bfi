module Bfi.Program

open System.IO

[<EntryPoint>]
let main argv =
  argv.[0]
  |> File.ReadAllText
  |> Parser.parse
  |> Optimizer.optimize
  |> Codegen.run

  0