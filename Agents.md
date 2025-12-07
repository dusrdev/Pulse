# Agents

## Overview
- Pulse is a ConsoleAppFramework-driven CLI that stress-tests HTTP endpoints using JSON-defined request recipes.
- The codebase is organized into small, purpose-built agents that coordinatedly parse configuration, execute requests, collect telemetry, and export results.
- This document captures the role, location, and interactions of every agent discovered during the repository scan performed on December 7, 2025.

## Runtime Agents

### Command & Orchestration Agent
- **Files:** `src/Pulse/Program.cs`, `src/Pulse/Core/Commands.cs`
- Registers the command surface (`Pulse`, `get-sample`, `get-schema`, `check-for-updates`, `terms-of-use`, `info`, `cli-schema`) and wires a global exception filter.
- Adds global options:
  - `--output-format` (PlainText|JSON) to select human-readable vs structured output for all commands.
  - `--quiet` to silence progress reporting on stderr while still allowing fatal errors.
- `Commands.Root` parses CLI inputs into `ParametersBase`, loads request definitions from disk via `InputJsonContext`, and triggers the execution pipeline. When `--connections` is omitted, it is set to the request count so runs default to fully parallel.
- Helper commands generate request samples and JSON Schema artifacts, print terms of use, surface app metadata (`info`), and query GitHub releases for updates.

### Output Formatting Agent
- **Files:** `src/Pulse/Models/IOutputFormatter.cs`, `src/Pulse/Models/GlobalOptions.cs`, `src/Pulse/Core/Helper.cs`, `src/Pulse/Models/*Model.cs`
- `IOutputFormatter` defines paired `OutputAsPlainText` / `OutputAsJson` methods with an `Output(OutputFormat)` extension to centralize human-vs-LLM rendering.
- `OutputFormat` (PlainText/JSON) is provided by the global `--output-format` option and stored in `GlobalOptions` for all commands.
- User-facing models (`RunConfiguration`, `SummaryModel`, `TermsOfServiceModel`, `CheckForUpdatesModel`, `GetSampleModel`, `InfoModel`) implement the interface so the same execution flow can emit colored console text or serialized JSON.

### Quiet/Progress Agent
- **Files:** `src/Pulse/Models/GlobalOptions.cs`, `src/Pulse/Models/Parameters.cs`, `src/Pulse/Core/Pulse.cs`, `src/Pulse/Core/ConsoleState.cs`, `src/Pulse/Core/PulseSummary.cs`, `src/Pulse/Core/GlobalExceptionHandler.cs`
- The global `--quiet` option flows into `Parameters.Quiet`; when false, `Pulse` streams Stats over a bounded channel to render a spinner, progress percentage, success rate, ETA, and status buckets on stderr while tracking cursor positions via `ConsoleState`.
- When `--quiet` is true the channel and printer are skipped, but results are still collected; `GlobalExceptionHandler` only clears console regions when progress was rendered.

### Configuration Agent
- **Files:** `src/Pulse/Configuration/InputJsonContext.cs`, `src/Pulse/Configuration/DefaultJsonContext.cs`, `src/Pulse/Models/Parameters.cs`, `src/Pulse/Models/RequestDetails.cs`
- Source generators (`JsonSerializerContext`) provide strongly typed serializers for request payloads, headers, exceptions, and release metadata.
- `Parameters` capture run-time tuning knobs (request count, timeout, concurrency, export flags, output directory, cancellation token).
- `RequestDetails`, `Request`, `Proxy`, and `Content` model the input JSON, supply request factories, and compute byte-size estimates via `Request.GetRequestLength` for throughput reporting.

### HTTP Execution Agent
- **Files:** `src/Pulse/Core/Pulse.RunAsync.cs`, `src/Pulse/Core/Pulse.cs`, `src/Pulse/Core/PulseHttpClientFactory.cs`
- `Pulse.RunAsync` builds an `HttpClient` (proxy-aware via `PulseHttpClientFactory`), instantiates the `Pulse` runner, and spins up `min(connections, requests)` worker tasks; if `--connections` is omitted it defaults to the request count so all requests fire in parallel.
- Each worker loops until all request ids are consumed, optionally delaying between sends; `Pulse.SendRequest` constructs the `HttpRequestMessage`, tracks current concurrency, streams responses, and normalizes results into `Response` records that include `CurrentConcurrentConnections`.
- Proxy handling honors bypass flags, credentials, and optional SSL certificate suppression (`Helper.ConfigureSslHandling`).

### Progress & Metrics Agent
- **Files:** `src/Pulse/Core/Pulse.cs`, `src/Pulse/Core/ConsoleState.cs`, `src/Pulse/Models/PaddedULong.cs`
- A single `Pulse` implementation owns progress sampling: it aggregates status buckets into a padded counter array to avoid contention, pushes periodic `Stats` snapshots over a bounded channel, and computes success rate/ETA/spinner frames for on-screen dashboards.
- `ConcurrentStack<Response>` stores results for later summarization; `ClearAndReturnAsync` shuts down the printer, restores cursor visibility, and returns a `PulseResult` containing total duration and aggregate success rate.

### Result Aggregation & Export Agents
- **Files:** `src/Pulse/Core/PulseSummary.cs`, `src/Pulse/Core/Exporter.cs`, `src/Pulse/Models/Response.cs`, `src/Pulse/Models/RawFailure.cs`
- `PulseSummary` tallies status buckets, computes latency/size statistics (with IQR filtering and SIMD acceleration), captures peak concurrent connections from `Response.CurrentConcurrentConnections`, and deduplicates responses via `ResponseComparer` when exports are requested.
- Throughput now factors outbound request size and, when `Export` is enabled, includes response payload bytes; `SummaryModel` exposes the computed `ConcurrentConnections`, throughput, and outlier removal counts.
- `Exporter` emits either prettified HTML dashboards or raw JSON/HTML blobs, formats headers into tables, and can purge previous result folders.
- `RawFailure` offers a compact representation of unsuccessful HTTP interactions for raw exports.

### Error Handling & Diagnostic Agent
- **Files:** `src/Pulse/Core/ExceptionHandler.cs`, `src/Pulse/Configuration/StrippedException.cs`, `src/Pulse/Core/Helper.cs`
- `GlobalExceptionHandler` cleans console output on cancellation or unhandled exceptions and reports sanitized details.
- `StrippedException` recursively flattens exception metadata (type, message, optional detail, inner exceptions) while tracking default/no-error states.
- `Helper` exposes color heuristics, SSL configuration helpers, and diagnostic printers ensuring consistent console formatting.

### Release & Version Agent
- **Files:** `src/Pulse/Core/ReleaseInfo.cs`, `src/Pulse/Core/Commands.cs`
- Maintains the CLI semantic version (`Commands.Version`) and validates it against assembly metadata (reinforced by unit tests and the `info` command output).
- Parses GitHub release payloads (`ReleaseInfo`) to notify users about upgrades.

### Shared Infrastructure Agent
- **Files:** `src/Pulse/GlobalUsings.cs`, `.editorconfig`, `Readme.md`, `History.md`, `Changelog.md`
- `GlobalUsings` centralizes PrettyConsole and `MethodImplOptions` imports for consistent styling.
- Repository documentation outlines usage patterns, options, and legal disclaimers (mirrored by `Commands.TermsOfUse`).
- Formatting guidelines and tooling preferences are driven by `.editorconfig`.

## Testing Agents
- **Project:** `tests/Pulse.Tests`
- `ExporterTests.cs` validates clearing behaviors, file naming, header serialization, JSON formatting, HTML generation, and exception rendering across raw and HTML exports.
- `HelperTests.cs` asserts color-selection logic for status codes and success percentages.
- `HttpClientFactoryTests.cs` ensure handler composition respects proxy settings and SSL overrides.
- `ParametersTests.cs` document expected defaults for request counts, connections, and export toggles.
- `PulseMonitorTests.cs` validate timeout handling in `Pulse.SendRequest`.
- `ResponseComparerTests.cs` verify deduplication semantics with and without full content equality.
- `StrippedExceptionTests.cs` confirm serialization contracts and null handling.
- `SummaryTests.cs` cover statistical reductions, outlier trimming, and SIMD-friendly averaging.
- `VersionTests.cs` keep `Commands.Version` synchronized with assembly metadata.

## Data Flow Snapshot
1. User invokes the CLI; global options set `GlobalOptions.OutputFormat` and `Quiet`, then `Commands.Root` loads `RequestDetails` (and optional URL overrides) into `Parameters`, defaulting `Connections` to the request count when not provided.
2. `Pulse.RunAsync` creates proxy-aware HTTP plumbing, instantiates the `Pulse` runner, and spins up worker tasks capped by `Connections`.
3. Workers request ids incrementally, call `Pulse.SendRequest`, which constructs and sends `HttpRequestMessage` instances, tracks current concurrency, and pushes progress stats unless `Quiet`.
4. `Pulse.ClearAndReturnAsync` shuts down progress rendering and yields a `PulseResult` containing the success rate and elapsed duration.
5. `PulseSummary` materializes `SummaryModel` (plaintext or JSON), computes peak concurrency, latency/size summaries with optional outlier removal, and instructs `Exporter` to persist deduplicated responses (raw or HTML) when exports are enabled.
6. `GlobalExceptionHandler` guarantees graceful shutdown, while auxiliary commands (`get-sample`, `get-schema`, `cli-schema`, `check-for-updates`, `terms-of-use`, `info`) reuse serialization agents for their workflows.

## External Dependencies
- **ConsoleAppFramework** (CLI host) and **PrettyConsole** (colored terminal output).
- **Sharpify** utilities (string helpers) and `System.Text.Json` source generators for (de)serialization.
- Unit tests rely on **xUnit**, plus `Bogus` for synthetic file data within exporter tests.

## Operational Notes
- Terms of use explicitly shift legal responsibility to operators; they are surfaced both in CLI output and repository docs.
- Export routines overwrite prior artifacts by design (`Exporter.ClearFiles`) before writing new responses.
- Vectorized statistics (`Vector512` / `Vector`) require modern hardware support but fall back to scalar logic when unavailable.
- The repository retains build artifacts under `bin/` and `obj/` across both the main app and test project; these were intentionally excluded from this summary.
