module Language.Brainfuck.Codegen

open System
open System.Reflection
open System.Reflection.Emit
open Language.Brainfuck.AST

type IL = ILGenerator

let tapeSize = 65_536

let consoleRead = typeof<Console>.GetMethod("Read", Array.empty)
let consoleWriteChar = typeof<Console>.GetMethod("Write", [|typeof<char>|])
let consoleWriteString = typeof<Console>.GetMethod("Write", [|typeof<string>|])
let stringCtor = typeof<string>.GetConstructor([|typeof<char>; typeof<int>|])

let emitTapeAlloc (il: IL) =
  il.DeclareLocal(typeof<sbyte>.MakePointerType()) |> ignore

  il.Emit(OpCodes.Ldc_I4, tapeSize)
  il.Emit(OpCodes.Conv_U)
  il.Emit(OpCodes.Localloc)
  il.Emit(OpCodes.Stloc_0)

let pushCrrCell (il: IL) (offset: int) =
  il.Emit(OpCodes.Ldloc_0)

  if (offset <> 0) then
    il.Emit(OpCodes.Ldc_I4, offset)
    il.Emit(OpCodes.Add)

let emitAdd (il: IL) offset (value: sbyte) =
  pushCrrCell il offset
  il.Emit(OpCodes.Dup)
  il.Emit(OpCodes.Ldind_I1)
  il.Emit(OpCodes.Ldc_I4_S, value)
  il.Emit(OpCodes.Add)
  il.Emit(OpCodes.Conv_I1)
  il.Emit(OpCodes.Stind_I1)

let emitMove (il: IL) value =
  pushCrrCell il value
  il.Emit(OpCodes.Stloc_0)

let emitSet (il: IL) offset (value: sbyte) =
  pushCrrCell il offset
  il.Emit(OpCodes.Ldc_I4_S, value)
  il.Emit(OpCodes.Stind_I1)

let emitRead (il: IL) offset =
  pushCrrCell il offset
  il.Emit(OpCodes.Call, consoleRead)
  il.Emit(OpCodes.Conv_I1)
  il.Emit(OpCodes.Stind_I1)

let emitWrite (il: IL) offset count =
  pushCrrCell il offset
  il.Emit(OpCodes.Ldind_I1)
  il.Emit(OpCodes.Conv_U2)

  if count = 1 then
    il.Emit(OpCodes.Call, consoleWriteChar)
  else
    il.Emit(OpCodes.Ldc_I4, count)
    il.Emit(OpCodes.Newobj, stringCtor)
    il.Emit(OpCodes.Call, consoleWriteString)

let rec emitLoop (il: IL) ops =
  let loopStart = il.DefineLabel()
  let loopEnd = il.DefineLabel()

  il.Emit(OpCodes.Br, loopEnd)

  il.MarkLabel(loopStart)

  emitOps' il ops

  il.MarkLabel(loopEnd)

  il.Emit(OpCodes.Ldloc_0)
  il.Emit(OpCodes.Ldind_I1)
  il.Emit(OpCodes.Brtrue, loopStart)

and emitOp (il: IL) offset = function
  | Add a -> emitAdd il offset a
  | Move m -> emitMove il m
  | Set s -> emitSet il offset s
  | Read -> emitRead il offset
  | Write n -> emitWrite il offset n
  | WithOffset (off, op) -> emitOp il off op
  | Loop ops -> emitLoop il ops

and emitOps' il = function
  | [] -> ()
  | op :: rest ->
      emitOp il 0 op
      emitOps' il rest

let emitOps il ops =
  emitTapeAlloc il
  emitOps' il ops
  il.Emit(OpCodes.Ret)

let compileAndRun ops =
  let asm = AssemblyBuilder.DefineDynamicAssembly(AssemblyName("Brainfuck"), AssemblyBuilderAccess.Run)
  let mdl = asm.DefineDynamicModule("Module")
  let ty = mdl.DefineType("Program")

  let mtd = ty.DefineMethod("Main", MethodAttributes.Private ||| MethodAttributes.HideBySig ||| MethodAttributes.Static, typeof<Void>, Array.empty)
  let il = mtd.GetILGenerator()

  emitOps il ops

  let ty = ty.CreateType()
  let mtd = ty.GetMethod("Main", BindingFlags.NonPublic ||| BindingFlags.Static)

  mtd.Invoke(null, Array.empty)
  |> ignore
