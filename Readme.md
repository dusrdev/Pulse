# Pulse ![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/dusrdev/Pulse/total?label=Downloads&labelColor=FF00AA&color=0000FF&cacheSeconds=1)

Pulse is a general purpose, cross-platform, performance-oriented, command-line utility for testing HTTP endpoints. Pulse was inspired by [Bombardier](https://github.com/codesenberg/bombardier), but is designed to be configured, have native support for proxies, and suited for heavier and more frequent workflows.

## Features

- JSON based request configuration
- Support for using proxies
- True multi-threading with configurable modes (max concurrency, batches, sequential)
- Supports all HTTP methods
- Supports Headers
- Support Content-Type and Body for POST, PUT, PATCH, and DELETE
- Custom HTML generated outputs for easy inspection
- Format JSON outputs
- Capture all response headers for debugging

And more!

## Installation

Pulse comes in a pre-build self-contained binary, which can be downloaded from the [releases](https://github.com/dusrdev/Pulse/releases) page.

## Usage

Pulse reads the JSON input and outputs the results respective to the working path, which means that Pulse can be added to path and used anywhere.

### Sending a single request using JSON configuration

```bash
Pulse configuration.json
```

### Sample outputs

During the execution, `Pulse` displays current metrics such as progress, success rate, ETA, and counts of responses from each of the 6 categories, i.e, 1xx, 2xx, 3xx, 4xx, 5xx, others. where `others` is essentially exceptions.

![Runtime metrics](https://github.com/user-attachments/assets/64f48192-f60e-4021-9df3-558af21c1bbd)

After the execution (different configuration in this example), `Pulse` produces a detailed summary of the results

![Summary](https://github.com/user-attachments/assets/9a05ff6e-8d3d-46af-8509-013b5b1536a0)

### Setting up a configuration file

The configuration file is a JSON file that contains proxy information and the request details.

It is recommended to use the build in `get-sample` command to generate a sample configuration file.

```bash
Pulse get-sample
```

This will generate a `sample.json` file in the current directory.

The `sample.json` file contains the following:

```json
{
 "Proxy": {
  "Bypass": true,
  "IgnoreSSL": false,
  "Host": "",
  "Username": "",
  "Password": ""
 },
  "Request": {
    "Url": "https://ipinfo.io/geo",
    "Method": {
      "Method": "GET"
    },
    "Headers": {},
    "Content": {
      "ContentType": "",
      "Body": null
    }
  }
}
```

#### Proxy

Proxy contains the configuration that would be used for the HTTP client.

By default the proxy parameters are set to empty, and the proxy is set to bypass (which means use defaults of the OS).

- `Bypass` - can be set to `true` even if the proxy parameters are not empty, to make AB testing easier.
- `Host` - can be used alone, or in combination with `Username` and `Password` to specify the proxy host.
- The credentials (i.e `Username` and `Password`) will only be used if both are specified.

#### Request

Request contains the configuration for the request.

- `Url` - the URL of the request.
- `Method` - the HTTP method of the request.
- `Headers` - the headers of the request. Can be `null` or a `JSON` object.

#### Content

Content contains the configuration for the request content. Which is only used for `POST`, `PUT`, `PATCH`, and `DELETE` requests.

- `ContentType` - the content type of the request content, if empty will default to `application/json`.
- `Body` - the body of the request content, `null` or any type of object including `JSON`. If set to `null` it will not be attached to the request.

## Options

Pulse has a wide range of options that can be configured in the command line, and can be viewed with `help` or `--help` which shows this:

```plaintext
Pulse [RequestFile] [Options]

RequestFile:
  path to .json request details file
  - If you don't have one use the "get-sample" command
Options:
  -n, --number     : number of total requests (default: 1)
  -t, --timeout    : timeout in milliseconds (default: -1 - infinity)
  -m, --mode       : execution mode (default: parallel)
      * sequential = execute requests sequentially
        --delay    : delay between requests in milliseconds (default: 0)
      * parallel  = execute requests using maximum resources
        -c         : max concurrent connections (default: infinity)
  --json           : try to format response content as JSON
  --raw            : export raw results (without wrapping in custom html)
  -f               : use full equality (slower - default: false)
  --no-export      : don't export results (default: false)
  -v, --verbose    : display verbose output (default: false)
  -o, --output     : output folder (default: results)
Special:
  get-sample       : command - generates sample file
  get-schema       : command - generates a json schema file
  check-for-updates: command - checks for updates
  terms-of-use     : print the terms of use
  --noop           : print selected configuration but don't run
  -u, --url        : override the url of the request
  -h, --help       : print this help text
Notes:
  * when "-n" is 1, verbose output is enabled
```

- `--json` - try to format response content as JSON
- `--raw` - export raw results (without wrapping in custom html)
- `-v` or `--verbose` - display verbose output, this changes the output format, instead of displaying a dashboard, it prints requests/responses as they are being processed.
- `f` - use fully equality: by default because response content can be entire webpages, it can be a time consuming and resource heavy operation to make sure all responses are unique, so by default a simpler check is used which only compares the content length - for most cases this is sufficient since you usually expect the same content for the requests, but you can opt in for full equality.
- `u` or `url` - can be used to override the url of the request, this can be useful if you want to keep all other settings the same, and quickly change the url of the request.
- `noop` - is a very useful command which will print the selected configuration but not perform the pulse, this can be used to inspect the request settings after they are parsed by the program, to ensure everything is exactly as you intended.
- `o` or `output` - can be used to specify the output folder, by default it is "results", but you can specify a different folder if you want to.

## Disclaimer

By using `Pulse` you agree to take full responsibility for the consequences of its use.

Usage of this tool for attacking targets without prior mutual consent is illegal. It is the end user's
responsibility to obey all applicable local, state and federal laws.
The developers assume no liability and are not responsible for any misuse or damage caused by this program.

## Contributing and Error reporting

- Errors, and features can be reported on the [issues](https://github.com/dusrdev/Pulse/issues) page.
- Sensitive bug reports can also be sent to `dusrdev@gmail.com`.
- Discussions can be found on the [discussions](https://github.com/dusrdev/Pulse/discussions) page.
- If anyone wants to contribute, feel free to fork the project and open a pull request.

> This project is proudly made in Israel 🇮🇱 for the benefit of mankind.
