module Bfi.Program

open System.Diagnostics
open System.IO

[<EntryPoint>]
let main argv =
  let code = File.ReadAllText argv.[0]

  let sw = Stopwatch.StartNew()
  let mtd =
    code
    |> Parser.parse
    |> Optimizer.optimize
    |> Codegen.compile

  sw.Stop()
  printfn "[Compile: %dms]" sw.ElapsedMilliseconds

  sw.Restart()
  Codegen.run mtd

  sw.Stop()
  printfn "\n[Run: %dms]" sw.ElapsedMilliseconds
  
  0