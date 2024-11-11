# Changelog

## Version 1.0.4.0

- Implemented much better mechanism to keep to track of concurrent connections, as well vastly improved the control and execution model of limited max connections parallel pulse.
- `-b`, `--batch` is not longer a valid parameter, now the parameter is `-c` (connections) to better reflect the real behavioral effect.

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
