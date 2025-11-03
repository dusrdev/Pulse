# Agents

## Overview
- Pulse is a ConsoleAppFramework-driven CLI that stress-tests HTTP endpoints using JSON-defined request recipes.
- The codebase is organized into small, purpose-built agents that coordinatedly parse configuration, execute requests, collect telemetry, and export results.
- This document captures the role, location, and interactions of every agent discovered during the repository scan performed on November 3, 2025.

## Runtime Agents

### Command & Orchestration Agent
- **Files:** `src/Pulse/Program.cs`, `src/Pulse/Core/Commands.cs`
- Registers the command surface (`Pulse`, `get-sample`, `get-schema`, `update`, `terms-of-use`) and wires a global exception filter.
- `Commands.Root` parses CLI inputs into `ParametersBase`, loads request definitions from disk via `InputJsonContext`, and triggers the execution pipeline.
- Hosts helper commands that generate request samples and JSON Schema artifacts, print terms of use, and query GitHub releases for updates.

### Configuration Agent
- **Files:** `src/Pulse/Configuration/InputJsonContext.cs`, `src/Pulse/Configuration/DefaultJsonContext.cs`, `src/Pulse/Configuration/Parameters.cs`, `src/Pulse/Core/RequestDetails.cs`
- Source generators (`JsonSerializerContext`) provide strongly typed serializers for request payloads, headers, exceptions, and release metadata.
- `Parameters` (and its base) capture run-time tuning knobs (request count, timeout, concurrency, export flags, verbosity, output directory, cancellation token).
- `RequestDetails`, `Request`, `Proxy`, and `Content` model the input JSON, supply request factories, and compute byte-size estimates.

### HTTP Execution Agent
- **Files:** `src/Pulse/Core/Pulse.cs`, `src/Pulse/Core/PulseHttpClientFactory.cs`, `src/Pulse/Core/IPulseMonitor.cs`
- `Pulse.RunAsync` builds an `HttpClient` (proxy-aware via `PulseHttpClientFactory`), instantiates a monitor, and dispatches multiple tasks gated by a `SemaphoreSlim`.
- `IPulseMonitor.RequestExecutionContext` prepares `HttpRequestMessage` instances, executes them with streamed responses, tracks concurrency, and normalizes results into `Response`.
- Proxy handling honors bypass flags, credentials, and optional SSL certificate suppression (`Helper.ConfigureSslHandling` extension).

### Monitoring Agents
- **Files:** `src/Pulse/Core/PulseMonitor.cs`, `src/Pulse/Core/VerbosePulseMonitor.cs`, `src/Pulse/Core/PaddedULong.cs`
- `PulseMonitor` provides dashboard-style aggregation: concurrent stacks store `Response` data while padded counters avoid false sharing when sampling metrics.
- `VerbosePulseMonitor` favors per-request logging (send/receive) with success tracking when verbose mode or single-shot execution is selected.
- Both implementations ultimately flush results through `ClearAndReturn`, returning a `PulseResult` with success rates and total duration.

### Result Aggregation & Export Agents
- **Files:** `src/Pulse/Core/PulseSummary.cs`, `src/Pulse/Core/Exporter.cs`, `src/Pulse/Core/Response.cs`, `src/Pulse/Core/RawFailure.cs`
- `PulseSummary` deduplicates responses (`ResponseComparer`), tallies status buckets, computes latency/size statistics (with IQR filtering and SIMD acceleration), and orchestrates exports.
- `Exporter` emits either prettified HTML dashboards or raw JSON/HTML blobs, formats headers into tables, and can purge previous result folders.
- `RawFailure` offers a compact representation of unsuccessful HTTP interactions for raw exports.
- Throughput, ETA, and success-rate colorization rely on utilities in `Helper.cs`.

### Error Handling & Diagnostic Agent
- **Files:** `src/Pulse/Core/ExceptionHandler.cs`, `src/Pulse/Configuration/StrippedException.cs`, `src/Pulse/Core/Helper.cs`
- `GlobalExceptionHandler` cleans console output on cancellation or unhandled exceptions and reports sanitized details.
- `StrippedException` recursively flattens exception metadata (type, message, optional detail, inner exceptions) while tracking default/no-error states.
- `Helper` exposes color heuristics, SSL configuration helpers, and diagnostic printers ensuring consistent console formatting.

### Release & Version Agent
- **Files:** `src/Pulse/Core/ReleaseInfo.cs`, `src/Pulse/Core/Commands.cs`
- Maintains the CLI semantic version (`Commands.VERSION`) and validates it against assembly metadata (reinforced by unit tests).
- Parses GitHub release payloads (`ReleaseInfo`) to notify users about upgrades.

### Shared Infrastructure Agent
- **Files:** `src/Pulse/GlobalUsings.cs`, `.editorconfig`, `Readme.md`, `History.md`, `Changelog.md`
- `GlobalUsings` centralizes PrettyConsole and `MethodImplOptions` imports for consistent styling.
- Repository documentation outlines usage patterns, options, and legal disclaimers (mirrored by `Commands.TermsOfUse`).
- Formatting guidelines and tooling preferences are driven by `.editorconfig`.

## Testing Agents
- **Project:** `tests/Pulse.Tests.Unit`
- `ExporterTests.cs` validates clearing behaviors, file naming, header serialization, JSON formatting, HTML generation, and exception rendering across raw and HTML exports.
- `HelperTests.cs` asserts color-selection logic for status codes and success percentages.
- `HttpClientFactoryTests.cs` (and related fixtures) ensure handler composition respects proxy settings and SSL overrides.
- `ParametersTests.cs` document expected defaults for request counts, connections, and export toggles.
- `PulseMonitorTests.cs` exercise timeout handling via `RequestExecutionContext`.
- `StrippedExceptionTests.cs` confirm serialization contracts and null handling.
- `SummaryTests.cs` cover statistical reductions, outlier trimming, and SIMD-friendly averaging.
- `VersionTests.cs` keep `Commands.VERSION` synchronized with assembly metadata.

## Data Flow Snapshot
1. User invokes the CLI; `Commands.Root` loads `RequestDetails` (and optional overrides) into `Parameters`.
2. `Pulse.RunAsync` creates proxy-aware HTTP plumbing, chooses a monitor (verbose or dashboard), and schedules the requested workload with semaphore-throttled concurrency.
3. `RequestExecutionContext` issues HTTP requests, captures responses or exceptions, and records latency and concurrency metrics inside `Response`.
4. Monitors update live console feedback and accumulate results before handing off a `PulseResult`.
5. `PulseSummary` prints aggregated metrics, deduplicates responses, and instructs `Exporter` to persist unique payloads (raw or HTML) into the configured output folder.
6. `GlobalExceptionHandler` guarantees graceful shutdown, while optional commands (`get-sample`, `get-schema`, `update`, `terms-of-use`) reuse serialization agents for auxiliary workflows.

## External Dependencies
- **ConsoleAppFramework** (CLI host) and **PrettyConsole** (colored terminal output).
- **Sharpify** utilities (string helpers) and `System.Text.Json` source generators for (de)serialization.
- Unit tests rely on **xUnit**, plus `Bogus` for synthetic file data within exporter tests.

## Operational Notes
- Terms of use explicitly shift legal responsibility to operators; they are surfaced both in CLI output and repository docs.
- Export routines overwrite prior artifacts by design (`Exporter.ClearFiles`) before writing new responses.
- Vectorized statistics (`Vector512` / `Vector`) require modern hardware support but fall back to scalar logic when unavailable.
- The repository retains build artifacts under `bin/` and `obj/` across both the main app and test project; these were intentionally excluded from this summary.
