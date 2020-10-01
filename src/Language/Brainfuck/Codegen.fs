module Language.Brainfuck.Codegen

open System
open System.Reflection
open System.Reflection.Emit
open PtrHelper
open Language.Brainfuck.AST

type IL = ILGenerator

let tapeSize = 65_536

let consoleReadKey = typeof<Console>.GetMethod("ReadKey", Array.empty)
let getKeyChar = typeof<ConsoleKeyInfo>.GetProperty("KeyChar").GetMethod
let consoleWrite = typeof<Console>.GetMethod("Write", [|typeof<char>|])

let emitTapeAlloc (il: IL) =
  il.DeclareLocal(Ptr<sbyte>.TypeOf) |> ignore // typeof<_ nativeptr> always returns typeof<IntPtr> in F#, so I wrote a helper classlib in C#
  il.DeclareLocal(typeof<ConsoleKeyInfo>) |> ignore // Local needed for storing result of Console.ReadKey() to call .KeyChar on it

  il.Emit(OpCodes.Ldc_I4, tapeSize)
  il.Emit(OpCodes.Conv_U)
  il.Emit(OpCodes.Localloc)
  il.Emit(OpCodes.Stloc_0)

let emitAdd (il: IL) (value: sbyte) =
  il.Emit(OpCodes.Ldloc_0)
  il.Emit(OpCodes.Dup)
  il.Emit(OpCodes.Ldind_I1)
  il.Emit(OpCodes.Ldc_I4_S, value)
  il.Emit(OpCodes.Add)
  il.Emit(OpCodes.Conv_I1)
  il.Emit(OpCodes.Stind_I1)

let emitMov (il: IL) (value: int) =
  il.Emit(OpCodes.Ldloc_0)
  il.Emit(OpCodes.Ldc_I4, value)
  il.Emit(OpCodes.Add)
  il.Emit(OpCodes.Stloc_0)

let emitSet (il: IL) (value: sbyte) =
  il.Emit(OpCodes.Ldloc_0)
  il.Emit(OpCodes.Ldc_I4_S, value)
  il.Emit(OpCodes.Stind_I1)

let emitRead (il: IL) =
  il.Emit(OpCodes.Ldloc_0)
  il.EmitCall(OpCodes.Call, consoleReadKey, Array.empty)
  il.Emit(OpCodes.Stloc_1)
  il.Emit(OpCodes.Ldloca_S, 1)
  il.EmitCall(OpCodes.Call, getKeyChar, Array.empty)
  il.Emit(OpCodes.Conv_I1)
  il.Emit(OpCodes.Stind_I1)

let emitWrite (il: IL) =
  il.Emit(OpCodes.Ldloc_0)
  il.Emit(OpCodes.Ldind_I1)
  il.Emit(OpCodes.Conv_U2)
  il.Emit(OpCodes.Call, consoleWrite)

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

and emitOp (il: IL) = function
  | Add a -> emitAdd il a
  | Move m -> emitMov il m
  | Set s -> emitSet il s
  | Read -> emitRead il
  | Write -> emitWrite il
  | Loop ops -> emitLoop il ops

and emitOps' il = function
  | [] -> ()
  | op :: rest ->
      emitOp il op
      emitOps' il rest

let emitOps il ops =
  emitTapeAlloc il
  emitOps' il ops
  il.Emit(OpCodes.Ret)

let compile ops =
  let ty =
    AssemblyBuilder
    .DefineDynamicAssembly(AssemblyName("Brainfuck"), AssemblyBuilderAccess.Run)
    .DefineDynamicModule("Module")
    .DefineType("Program")

  let il =
    ty
    .DefineMethod("Main", MethodAttributes.Private ||| MethodAttributes.HideBySig ||| MethodAttributes.Static, typeof<Void>, Array.empty)
    .GetILGenerator()

  emitOps il ops
  ty.CreateType()

let run (ty: Type) =
  ty
  .GetMethod("Main", BindingFlags.NonPublic ||| BindingFlags.Static)
  .Invoke(null, Array.empty)
  |> ignore
