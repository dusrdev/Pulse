using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Pulse.Configuration;

using Sharpify;

namespace Pulse.Core;

public static class Exporter {
  public static async Task ExportHtmlAsync(Response result, int index, CancellationToken token = default) {
    if (token.IsCancellationRequested) {
      return;
    }

    string basePath = Utils.Env.PathInBaseDirectory("results/");
    Directory.CreateDirectory(basePath);
    string path = Path.Join(basePath, $"response-{index}.html");
    string frameTitle;
    string content;

    if (result.Exception is not null) {
      frameTitle = "Exception:";
      content = JsonSerializer.Serialize(result.Exception, Services.Instance.JsonOptions);
    } else {
      frameTitle = "Content:";
      content = (result.Content ?? "").Replace('\'', '\"');
    }
    HttpStatusCode statusCode = result.StatusCode ?? 0;
    string contentFrame = content == "" ?
"""
<div>
<h2>Content: Empty...</h2>
</div>
"""
:
$$"""
<div class="iframe-container">
<h2>{{frameTitle}}</h2>
<iframe title="Content" width="100%" height="100%" srcdoc='{{content}}'></iframe>
</div>
""";
    string headers = "";
    if (result.Headers is not null && result.Headers.Any()) {
      headers =
      $"""
      <div>
      {ToHtmlTable(result.Headers)}
      </div>
      """;
    }
    string body =
$$"""
<!DOCTYPE html>
<html lang="en">
<title>Response: {{index}}</title>
<head>
<meta name="viewport" content="width=device-width, initial-scale=1" charset="utf-8"/>
<style>
body {
  display: flex;
  flex-direction: column;
}
html, body {
  height: 98%;
}
h1 {
  font-family: 'Lucida Bright';
  font-size: 200%;
  text-align: center;
}
h2 {
  text-align: left;
}
td {
  overflow: auto;
}
.iframe-container {
  flex: 1;
  display: flex;
  flex-direction: row;
}
iframe {
  flex: 1;
  width: 100%;
  border: none;
}
iframe {
  box-shadow: 0 0 10px rgba(0,0,0,0.1);
}
</style>
</head>
<body>
<h1 class="title">Response: {{index}}</h1>
<div>
<h2>StatusCode: {{statusCode}} ({{(int)statusCode}})</h2>
</div>
{{headers}}
{{contentFrame}}
</body>
""";
    await File.WriteAllTextAsync(path, body, token);
  }

  /// <summary>
  /// Converts HttpResponseHeaders to an HTML table representation.
  /// </summary>
  /// <param name="headers">The HttpResponseHeaders to convert.</param>
  /// <returns>A string containing the HTML table.</returns>
  /// <exception cref="ArgumentNullException">Thrown when headers is null.</exception>
  public static string ToHtmlTable(this HttpResponseHeaders headers) {
    StringBuilder sb = new();

    // Start the table and add some basic styling
    sb.AppendLine("<table style=\"border-collapse: collapse; table-layout: fixed; width: 98%; margin: 5px 0;\" border=\"1\">");
    sb.AppendLine("  <thead>");
    sb.AppendLine("    <tr>");
    sb.AppendLine("      <th style=\"padding: 8px; text-align: left; width: 25%;\">Header</th>");
    sb.AppendLine("      <th style=\"padding: 8px; text-align: left; width: 75%;\">Value</th>");
    sb.AppendLine("    </tr>");
    sb.AppendLine("  </thead>");
    sb.AppendLine("  <tbody>");

    foreach (var header in headers) {
      string headerName = WebUtility.HtmlEncode(header.Key);
      string headerValues = WebUtility.HtmlEncode(string.Join(", ", header.Value));

      sb.AppendLine("    <tr>");
      sb.AppendLine($"      <td style=\"padding: 8px;\">{headerName}</td>");
      sb.AppendLine($"      <td style=\"padding: 8px;\">{headerValues}</td>");
      sb.AppendLine("    </tr>");
    }

    sb.AppendLine("  </tbody>");
    sb.AppendLine("</table>");

    return sb.ToString();
  }
}