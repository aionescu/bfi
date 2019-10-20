module Bfi.Program

open System.Diagnostics
open System.IO

[<EntryPoint>]
let main argv =
  let sw = Stopwatch.StartNew()

  let ast =
    argv.[0]
    |> File.ReadAllText
    |> Parser.parse
    |> Optimizer.optimize

  printfn "[Compile: %dms]" sw.ElapsedMilliseconds

  sw.Restart()
  Codegen.run ast

  printfn "\n[Run: %dms]" sw.ElapsedMilliseconds
  0