# bfi

Optimizing [Brainfuck](https://en.wikipedia.org/wiki/Brainfuck) interpreter that runs on [.NET Core](https://en.wikipedia.org/wiki/.NET_Core).

## Building and running

You will need the .NET Core SDK, which can be found [here](https://dotnet.microsoft.com/download).

To build, run `dotnet build` in the repo's root directory.

To run the project, run `dotnet run <file>` (e.g. `dotnet run Samples/HelloWorld.bf`). Running will also build the project.

## Syntax and memory tape

Since Brainfuck syntax is not fully agreed upon between different interpeters/compilers, `bfi` follows these conventions:

* Characters that are not BF instructions are simply ignored
* Single-line comments are supported, and are introduced with the `#` character
* The size of cells in the memory tape is 1 byte
* Cell values can overflow (without introducing undefined behaviour)
* The number of cells in the memory tape is 65536, thus the memory tape occupies 64KiB
* The memory tape is allocated on the stack, so overflowing it will likely result in undefined behaviour

## Sample programs

BF programs used to test the interpreter can be found in the [Samples](Samples/) folder.

Some sample programs are made by other people and were found on the web. Such programs have a comment containing the download link, as well as the original license/copyright notices.

## Optimizations

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
>>> << => Move 1
```

```bf
> <<<< => Move -3
```

```bf
+++ --- => # Nothing
```

### Efficient cell zeroing

It is a common pattern in BF code to reset a cell's value using the `[-]` instruction.
The interpreter recognizes this pattern and transforms it into a single assignment.

Note that due to the fact that cells may overflow, `[+]` achieves the same purpose (and is also optimized).

```bf
[-] => Set 0
```

```bf
[+] => Set 0
```

### Instructions with offset

Another common BF pattern is to go to some cell, run an instruction on it, then move back (e.g. `>>>>++<<<<`).

The naive way to compile this is to emit `[Move 4, Add 2, Move -4]`.

Not only does this use 3 instructions, it also reads and modifies the current cell twice.

The interpreter optimizes this into `[WithOffset 4 (Add 2)]`, which reads the current cell once, adds the offset to it, then performs the instruction at the resulting address.

### Dead code elimination

In some cases (such as consecutive loops), the compiler can statically determine that particular instructions have no effect, so they are removed.

```bf
[+>][<-] => [+>] => Loop [Add 1, Move 1] # Second loop is not compiled
```

Loops at the beginning of the program are also removed, as the initial value of cells is 0.

### Instruction conversion

In some cases, certain instructions can be replaced with cheaper ones.

For example, `Set` instructions are cheaper than `Add` instructions, since an `Add` needs to read the current cell, load the other number, perform the addition, then store it back into the current cell, while a `Set` only needs 1 load + write.

As such, an `Add` instruction after a loop can be replaced with a `Set` instruction, as the value of the cell is 0 (since it exited the loop), so the 2 instructions are equivalent.

```bf
[-]+++ => Set 0, Add 3 => Set 3
```

Also, setting a value after adding to it (or subtracting from it) will overwrite the result of the additions, so they are removed.

```bf
+++[-] => Add 3, Set 0 => Set 0
```

Consecutive `Set`s also behave similarly: Only the last `Set` is emitted, previous ones are elided.

## License

This repository is licensed under the terms of the MIT License.
For more details, see [the license file](LICENSE.txt).
