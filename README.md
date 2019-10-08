# Bfi

`Bfi` is an optimizing [Brainfuck](https://en.wikipedia.org/wiki/Brainfuck) interpreter that runs on [.NET Core](https://en.wikipedia.org/wiki/.NET_Core).

## Building from source

In order to build the project for a specific platform, run `dotnet build src/Bfi/Bfi.fsproj -r <platform>` in the repository's directory, where `<platform>` is either `win-x64`, `win-x86`, `linux-x64`, or `osx-x64`.
You will need the .NET Core SDK v3.0 or newer, which you can download [here](https://dotnet.microsoft.com/download).

## Syntax and memory tape

Since Brainfuck syntax is not fully agreed upon between different interpeters/compilers, I made the following choices:

* Characters that are not BF instructions are simply ignored
* Single-line comments are supported, and are introduced with the `#` character
* The size of cells in the memory tape is 1 byte
* Cell values can overflow (without introducing undefined behaviour)
* The number of cells in the memory tape is 65536, thus the memory tape occupies 64KiB
* The memory tape is allocated on the stack, so overflowing it will likely result in undefined behaviour

## Compiler internals

The compilation process is split into 3 phases: [Parsing](src/Bfi/Parser.fs), [Optimization](src/Bfi/Optimizer.fs), and [Code generation](src/Bfi/Codegen.fs) (links lead to corresponding source files).

In the parsing phase, the text source is converted into an Abstract Syntax Tree ([defined here](src/Bfi/Ast.fs)), which is then processed by the later phases.
In the optimization phase, the AST that was parsed is converted into a smaller, more efficient AST. More details can be found in [the next section](#ast-and-optimizations).
In the codegen phase, the AST is converted into [.NET CIL](https://en.wikipedia.org/wiki/Common_Intermediate_Language) code, which is then compiled to an in-memory assembly and executed.

## AST and Optimizations

The AST, or intermediate representation used by the compiler consists of generalized versions of BF instructions.
For example, the `+` instruction gets compiled to an `Add 1` node, while `-` gets compiled to `Add -1`.

This representation enables a number optimizations to be performed:

### Instruction merging

Multiple instructions of the same kind (e.g. + and -, or < and >) are merged into a single instruction.

```bf
+++ --- ++ => Add 2
```

```bf
+++ --- --- => Add -3
```

```bf
>>> << => Mov 1
```

```bf
> <<<< => Mov -3
```

```bf
+++ --- => # Nothing
```

### Efficient cell zeroing

It is a common pattern in BF code to reset a cell's value using the `[-]` instruction.
The compiler recognizes this pattern and transforms it into a single assignment.

Note that due to the fact that cells may overflow, `[+]` achieves the same purpose (and is also optimized).

```bf
[-] => Set 0
```

```bf
[+] => Set 0
```

### Dead code elimination

In some cases (such as consecutive loops), the compiler can statically determine that particular instructions have no effect, so they are removed.

```bf
[+>][<-] => [+>] => Loop [Add 1, Mov 1] # Second loop is not compiled
```

Loops at the beginning of the program are also removed, as the initial value of cells is 0.

### Instruction conversion

In some cases, certain instructions can be replaced with cheaper ones.
For example, adding after a loop can be replaced with a `set` instruction, as the value of the cell is 0 (since it exited the loop), so the 2 instructions are equivalent.

```bf
[-]+++ => Set 0, Add 3 => Set 3
```

Also, setting a value after adding to it (or subtracting from it) will overwrite the result of the additions, so they are removed.

```bf
+++[-] => Add 3, Set 0 => Set 0
```

Consecutive `set`s also behave similarly: Only the last `set` is compiled, previous ones are elided.

## Test programs

BF programs used to test the interpreter can be found in the [Tests](Tests/) folder.

## License

This project is licensed under the terms of the MIT License.
For more details, see [the license file](LICENSE.txt).