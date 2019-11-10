module Bfi.Codegen

open System
open System.Reflection
open System.Reflection.Emit
open PtrHelper
open Bfi.Ast

type IL = ILGenerator

[<Literal>]
let methodAttrs = MethodAttributes.Private ||| MethodAttributes.HideBySig ||| MethodAttributes.Static

[<Literal>]
let bindingFlags = BindingFlags.NonPublic ||| BindingFlags.Static

let readKey = typeof<Console>.GetMethod("ReadKey", Array.empty)
let getKeyChar = typeof<ConsoleKeyInfo>.GetProperty("KeyChar").GetMethod
let write = typeof<Console>.GetMethod("Write", [|typeof<char>|])

// AssemblyBuilderAccess.Save is (currently?) unavailable in .NET Core
let inline mkAssembly() = AssemblyBuilder.DefineDynamicAssembly(AssemblyName("BF"), AssemblyBuilderAccess.Run)

let inline mkModule (asm: AssemblyBuilder) = asm.DefineDynamicModule("Module")

let inline mkType (mdl: ModuleBuilder) = mdl.DefineType("Program")

let inline mkMethod (ty: TypeBuilder) = ty.DefineMethod("Main", methodAttrs, typeof<Void>, Array.empty)

let inline getIL (mtd: MethodBuilder) = mtd.GetILGenerator()

let emitTapeAlloc (il: ILGenerator) =
  il.DeclareLocal(Ptr<sbyte>.TypeOf) |> ignore // typeof<_ nativeptr> always returns typeof<IntPtr> in F#, so I wrote a helper classlib in C#
  il.DeclareLocal(typeof<ConsoleKeyInfo>) |> ignore // Local needed for storing result of Console.ReadKey() to call .KeyChar on it

  il.Emit(OpCodes.Ldc_I4, 65536)
  il.Emit(OpCodes.Conv_U)
  il.Emit(OpCodes.Localloc)
  il.Emit(OpCodes.Stloc_0)

let inline emitRet (il: IL) =
  il.Emit(OpCodes.Ret)

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
  il.EmitCall(OpCodes.Call, readKey, Array.empty)
  il.Emit(OpCodes.Stloc_1)
  il.Emit(OpCodes.Ldloca_S, 1)
  il.EmitCall(OpCodes.Call, getKeyChar, Array.empty)
  il.Emit(OpCodes.Conv_I1)
  il.Emit(OpCodes.Stind_I1)

let emitWrite (il: IL) =
  il.Emit(OpCodes.Ldloc_0)
  il.Emit(OpCodes.Ldind_I1)
  il.Emit(OpCodes.Conv_U2)
  il.Emit(OpCodes.Call, write)

let rec emitLoop (il: IL) (ops: Op list) =
  let loopStart = il.DefineLabel()
  let loopEnd = il.DefineLabel()

  il.Emit(OpCodes.Br, loopEnd)

  il.MarkLabel(loopStart)

  emitOps' il ops
 
  il.MarkLabel(loopEnd)

  il.Emit(OpCodes.Ldloc_0)
  il.Emit(OpCodes.Ldind_I1)
  il.Emit(OpCodes.Brtrue, loopStart)

and emitOp (il: IL) (op: Op) =
  match op with
  | Add a -> emitAdd il a
  | Mov m -> emitMov il m
  | Set s -> emitSet il s
  | Read -> emitRead il
  | Write -> emitWrite il
  | Loop ops -> emitLoop il ops

and emitOps' il ops =
  match ops with
  | [] -> ()
  | op :: rest ->
      emitOp il op
      emitOps' il rest

let inline emitOps il ops =
  emitTapeAlloc il
  emitOps' il ops
  emitRet il

let compile ops =
  let ty =
    mkAssembly()
    |> mkModule
    |> mkType

  let il =
    ty
    |> mkMethod
    |> getIL

  emitOps il ops
  ty.CreateType().GetMethod("Main", bindingFlags)

let inline run (mtd: MethodInfo) =
  mtd.Invoke(null, Array.empty) |> ignore