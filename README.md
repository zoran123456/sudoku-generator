# SudokuGenerator

A .NET 8 console application for generating and solving Sudoku puzzles of varying difficulty levels.

## Features
- Generate Sudoku puzzles with selectable difficulty (1–5)
- Print puzzles and their solutions in the console
- Logic-based solver with multiple strategies (Singles, Pairs/Triples, Pointing, X-Wing)
- Difficulty analysis for generated puzzles
- Fully unit-tested core logic

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Building
Clone the repository and build the solution:

```sh
dotnet build
```

### Running
Run the console application:

```sh
dotnet run --project SudokuGenerator/SudokuGenerator.csproj
```

You will be prompted to select a difficulty level (1 = Easy, 5 = Hard). The app will generate a puzzle and print both the puzzle and its solution.

### Testing
Run all unit tests:

```sh
dotnet test
```

## Project Structure
- `SudokuGrid` – Core 9x9 grid representation and validation
- `SudokuSolver` – Facade for puzzle generation and solving
- `SudokuPuzzleGenerator` – Logic for generating puzzles of a given difficulty
- `SudokuLogicSolver` – Logic-based solving strategies
- `SudokuBacktrackingSolver` – Backtracking and solution counting
- `SudokuDrawer` – Console output formatting
- `StrategyLevel` – Difficulty/strategy classification
- `SudokuUnitHelper`, `CombinatoricsHelper` – Helper utilities

## Example Output
```
Select difficulty level (1-Easy … 5-Hard): 3
Calibrated puzzle for level 3 (needs Pointing):
+---+---+---+
|5 3  | 7   |
|6   |1 9 5|
| 9 8|     |
+---+---+---+
|8   | 6   3|
|4   |8   1|
|7   | 2   6|
+---+---+---+
| 6  |     2|
|   4|1 9 8|
|   7|   6 5|
+---+---+---+

...solution grid follows...
```

## License
MIT License.
