using System.Net;
using System.Text;
using System.Text.Json;

using Pulse.Configuration;

namespace Pulse.Core;

public static class Exporter {
  public static async Task ExportHtmlAsync(Response result, string path, bool formatJson = false, CancellationToken token = default) {
    if (token.IsCancellationRequested) {
      return;
    }

    HttpStatusCode statusCode = result.StatusCode;
    string filename = Path.Join(path, $"response-{result.Id}-status-code-{(int)statusCode}.html");
    string frameTitle;
    string content = string.IsNullOrWhiteSpace(result.Content) ? "" : result.Content;

    if (result.Exception.IsDefault) {
      frameTitle = "Content:";
      if (formatJson) {
        try {
          using var doc = JsonDocument.Parse(content);
          var root = doc.RootElement;
          var json = JsonSerializer.Serialize(root, JsonContext.Default.JsonElement);
          content = $"<pre>{json}</pre>";
        } catch (JsonException) { } // Ignore - Keep content as is
      }
      content = content.Replace('\'', '\"');
    } else {
      frameTitle = "Exception:";
      content = $"<pre>{JsonContext.SerializeException(result.Exception)}</pre>";
    }
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
    if (result.Headers.Any()) {
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
<title>Response: {{result.Id}}</title>
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
table {
  border-collapse: collapse;
  table-layout: fixed;
  width: 100%;
  margin: 5px 0;
}
table,td,th {
  border: 1px solid;
}
tr:nth-child(even) {
  background-color: whitesmoke;
}
th {
  background-color: black;
  color: white;
  font-weight: bold;
}
td,th {
  padding: 8px;
  text-align: left;
  vertical-align: top;
  overflow: auto;
}
td.header, th.header {
  width: 25%;
}
td.value, th.value {
  width: 75%;
}
.iframe-container {
  flex: 1;
  flex-direction: row;
  display: flex;
}
iframe {
  flex: 1;
  width: 100%;
  height: 1000px;
  box-shadow: 0 0 10px rgba(0,0,0,0.1);
}
</style>
</head>
<body>
<h1 class="title">Response: {{result.Id}}</h1>
<div>
<h2>StatusCode: {{statusCode}} ({{(int)statusCode}})</h2>
</div>
{{headers}}
{{contentFrame}}
</body>
""";
    await File.WriteAllTextAsync(filename, body, token);
  }

  /// <summary>
  /// Converts HttpResponseHeaders to an HTML table representation.
  /// </summary>
  /// <param name="headers">The HttpResponseHeaders to convert.</param>
  /// <returns>A string containing the HTML table.</returns>
  /// <exception cref="ArgumentNullException">Thrown when headers is null.</exception>
  internal static string ToHtmlTable(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers) {
    StringBuilder sb = new();

    // Start the table and add some basic styling
    sb.AppendLine("<table>");
    sb.AppendLine("<thead>");
    sb.AppendLine("<tr>");
    sb.AppendLine("<th class=\"header\">Header</th>");
    sb.AppendLine("<th class=\"value\">Value</th>");
    sb.AppendLine("</tr>");
    sb.AppendLine("</thead>");
    sb.AppendLine("<tbody>");

    foreach (var header in headers) {
      string headerName = WebUtility.HtmlEncode(header.Key);
      string headerValues = WebUtility.HtmlEncode(string.Join(", ", header.Value));

      sb.AppendLine("<tr>");
      sb.AppendLine($"<td class=\"header\">{headerName}</td>");
      sb.AppendLine($"<td class=\"value\">{headerValues}</td>");
      sb.AppendLine("</tr>");
    }

    sb.AppendLine("</tbody>");
    sb.AppendLine("</table>");

    return sb.ToString();
  }

  /// <summary>
  /// Removes all files in the directory
  /// </summary>
  /// <param name="directoryPath"></param>
  internal static void ClearFiles(string directoryPath) {
    var files = Directory.GetFiles(directoryPath);
    if (files.Length == 0) {
      return;
    }
    foreach (var file in files) {
      try {
        File.Delete(file);
      } catch {
        // ignored
      }
    }
  }
}