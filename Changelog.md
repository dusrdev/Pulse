# Changelog

- Updated `PrettyConsole` to latest version to use higher perf APIs.
- Moved from using `Sharpify.CommandLineInterface` to `ConsoleAppFramework` for better perf and less verbose code.
- Many different internals were optimized to provide higher stability and performance.
- `ExecutionMode` is no longer used, and the options were unified:
  - To use `Sequential` mode, simply set `-c| --connections` to 1.
  - By default `Parallel` number will be used and `connections` will be set to the number of requests.
  - To use `Limited` mode, simply set `-c| --connections` to the desired value by use the optional parameter.
  - `-d| --delay` can now be combined with any of the options above, even though at full `Parallel` it will only delay the results summary.
- Many outputs show now be more consistent and artifact free, including when updating output is interrupted (like when press CTRL+C).
- Compilations options were refined to produce a even more purpose fit executable.
  - The binary size should smaller.
  - Startup times should be better.
  - Potential delays due to GC should not be much less frequent.
  - Hot-paths should perform even faster due to multi-level perf analysis.
