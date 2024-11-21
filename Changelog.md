# Version 1.1.0.0

- TEST: add `-raw` option to exports (and check duplicate div with exception/json)
- [X]: move `changelog` to `history` and create `changelog` for only the latest version
- [ ]: attach (latest)`changelog` as body of new releases
- [ ]: configure release action to use different place to attach version
- [ ]: change version checks to work with (change above)
- [ ]: deprecate previous versions
- TEST: fix concurrent connections not decrement when exception is thrown
- Export styling was reworked for much prettier ui
- Enhanced styling of verbose progress reporting
- Optimized interlocking variables to match hardware cache line size and reduce thread contention
- Optimized tasks creation to increase maximum concurrency level
- Summary will now filter out latency and size IQR outliers
- `terms-of-use` is now a command, not a flag
- When `-n` (amount of requests) is set to 1, verbose output will be enforced for better readability
