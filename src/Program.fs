module Bfi.Program

open System.IO

let openFile args =
  if Array.isEmpty args then
    Error "No input file specified"
  else
    Ok <| File.ReadAllText args.[0]

let writeErrors = function
  | Ok _ -> ()
  | Error err -> printfn "Error: %s." err

[<EntryPoint>]
let main argv =
  openFile argv
  |> Result.bind Parser.parse
  |> Result.map Optimizer.optimize
  |> Result.map Codegen.compile
  |> Result.map Codegen.run
  |> writeErrors
  
  0