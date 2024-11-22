# Versions 1.1.0.0 - 1.1.1.0

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
