# Changelog

## Version 1.2.0.0

- Raw export mode now outputs a special json with debug information for non-successful responses, it contains:
  - Response status code as integer
  - Response headers
  - Response content (if any)

## Version 1.1.2.0

- Fixed parsing exception in cases where response charset contained redundant quotes like `\"utf-8\"`
- Added support for AVX512

## Version 1.1.1.0

- Fixed a bug where `--raw` was not respected in 1 request workflows
- Content size metrics are no longer IQR filtered, outliers in content size could require manual inspection so alerting for them is a good idea.
- Implemented much more efficient SIMD optimized implementation for summary calculations

## Version 1.1.0.0

- Export styling was reworked for much prettier ui
- Optimized interlocking variables to match hardware cache line size and reduce thread contention
- Optimized tasks creation to increase maximum concurrency level
- Enhanced styling of verbose progress reporting
- Stabilized and improved concurrency tracking in the face of exceptions
- Added `--raw` option to exports (skips creating a custom html)
  - this option can be combined with `--json` to format the content as json
  - this option will also change the file extension to `json` when the flag is used or if it is an exception
- `terms-of-use` is now a command, not a flag
- When `-n` (amount of requests) is set to 1, verbose output will be enforced for better readability
- The changelog for the versions will now be the body of each release
- Updated internals to watch for different values in remote to track updates
  - This means that previous versions will not be able to track (>=1.1.0.0) updates
- Summary will now filter out latency and size IQR outliers

## Version 1.0.7.0

- Added `get-schema` command to generate a json schema file
- Implemented a workaround for calculating the content length of a response when not provider in the response headers - only viable when exporting results
- Implemented a recursive exception tracking mechanism allow for better error reporting
- Removed redundant empty lines before summary output in `--verbose` mode
- Implemented more efficient locking mechanism for output synchronization

## Version 1.0.6.0

- Updated to .NET 9.0
- Optimized using new version of `PrettyConsole` (3.1.0)

## Version 1.0.5.0

- Request duration is was changed to latency, a more important metric to measure
- Latency is properly measured immediately after response is received, reading the response content is now a separate step - saving resources in the process, especially when `--no-export` is used
- Summary was redesigned to be more informative
  - Removed "Summary" header, result looks cleaner now
  - Total throughput measurement was implemented and now is displayed
  - `verbose` no longer prints used memory, this metric didn't have much to do the purpose of `Pulse`, and it is also not recorded anymore.
- A header showing the request method and url is now printed before the pulse starts, makes easier to track that you're executing the right thing, when the URL is hidden in the request file.
- `verbose` mode now changes the output format, instead of displaying a dashboard, it prints which request/response is being processed.
- The progress bar that was used when cross referencing results was removed, now it is replaced by a simpler text output. This is because it produced artifacts with the headers
- Added `--delay` parameter to specify the delay between requests in milliseconds (default: 0) - only applicable when execution mode is sequential

## Version 1.0.4.0

- Implemented much better mechanism to keep to track of concurrent connections, as well vastly improved the control and execution model of limited max connections parallel pulse.
- `-b`, `--batch` is not longer a valid parameter, now the parameter is `-c` (connections) to better reflect the real behavioral effect.
- Implement better mechanism to keep track of content size, now it should require less resources and be available even when `--no-export` is used.

## Version 1.0.3.0

- Added `-t, --timeout` parameter to specify the timeout in milliseconds (default: -1 - infinity)
- Fixed handling of timeouts as HttpClient hides the exception under `TaskCanceledException` leading to same output and handling as manual user cancellation, which was incorrect

## Version 1.0.2.0

- Added `-o, --output` parameter to specify the output folder (default: "results")
- Updated automations to add ubuntu 20.04 support to address issues with glibc2.31

## Version 1.0.1

- Added `check-for-updates` command

## Version 1.0.0

Initial release
