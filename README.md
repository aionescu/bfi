# bfi

Optimizing [Brainfuck](https://en.wikipedia.org/wiki/Brainfuck) interpreter that runs on [.NET 5](https://en.wikipedia.org/wiki/.NET_Core).

## Building and running

You will need the .NET SDK, which can be found [here](https://dotnet.microsoft.com/download).

To build, run `dotnet build` in the repo's root directory.

To run the interpreter, run `dotnet run <file>` (e.g. `dotnet run Examples/HelloWorld.bf`). This will also build the project if it was not built beforehand.

## Syntax and memory tape

Since Brainfuck's syntax and semantics are not fully agreed upon between different implementations, `bfi` implements the language as follows:

* Single-line comments are supported, and are introduced by the `#` character
* Non-whitespace characters that are not BF instructions are not allowed outside comments
* The size of each cell in the memory tape is 1 byte
* The total number of cells in the memory tape is 65536
* Cell values can overflow

## Example programs

BF programs used to test the interpreter can be found in the [Examples](Examples/) folder.

Some of the example programs were found on the web. They contain the original copyright notice, as well as the URL they were found at, in their comments.

## License

This repository is licensed under the terms of the MIT License.
For more details, see [the license file](LICENSE.txt).
