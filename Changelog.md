# Changelog

- Updated `PrettyConsole` to latest version to use higher perf APIs.
- Moved from using `Sharpify.CommandLineInterface` to `ConsoleAppFramework` for better perf and less verbose code.
- Dropped `Sharpify` dependency.
- Many different internals were optimized to provide higher stability and performance.
- `ExecutionMode` is no longer used, and the options were unified:
  - To use `Sequential` mode, simply set `-c| --connections` to 1.
  - By default `Parallel` number will be used and `connections` will be set to the number of requests.
  - To use `Limited` mode, simply set `-c| --connections` to the desired value by use the optional parameter.
  - `-d| --delay` can now be combined with any of the options above, even though at full `Parallel` it will only delay the results summary.
- Many outputs are now more consistent and artifact free, including when the output is interrupted (like when press CTRL+C).
- Added `cli-schema` command prints the usage schema for the app in JSON format - Useful for LLM's and AGENTS.
- Compilations options were refined to produce a even more purpose fit executable.
  - Smaller output binary size.
  - Shorter startup times.
  - Much less memory allocations and lower GC pressure.
  - De-abstraction of hot-paths should now results in overall performance increase.
