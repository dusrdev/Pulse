# Version 1.1.1.0

- Fixed a bug where `--raw` was not respected in 1 request workflows
- [ ] content size metrics are no longer IQR filtered, outliers in content size could require manual inspection so alerting for them is a good idea.
- [ ] implemented much more efficient SIMD optimized implementation for summary calculations
