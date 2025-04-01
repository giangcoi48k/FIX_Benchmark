# FIX Message Parser Benchmark

## Overview
The project provides a `FixDictionary` class that parses FIX messages into a `Dictionary<int, string>` for quick access. The parsing method utilizes `ReadOnlySpan<char>` to reduce allocations. 

## Requirements
- .NET 8.0 or later
- BenchmarkDotNet (for benchmarking performance)

## Usage
To parse a FIX message, simply download the project and run the following command:

```sh
dotnet run -c Release
```

This will execute the benchmark and parse the FIX message as part of the performance test.
