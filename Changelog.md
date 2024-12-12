# Changelog

- `--raw` export mode now outputs a special json with debug information for non-successful responses with no content, it contains:
  - Response status code as integer
  - Response headers
  - Response content (if any)
