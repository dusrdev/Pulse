using System.Net;
using System.Text;
using System.Text.Json;

using Pulse.Configuration;

using Sharpify;

namespace Pulse.Core;

public static class Exporter {
  public static Task ExportResponseAsync(Response result, string path, Parameters parameters, CancellationToken token = default) {
    if (token.IsCancellationRequested) {
      return Task.CompletedTask;
    }

    if (parameters.ExportRaw) {
      return ExportRawAsync(result, path, parameters.FormatJson, token);
    } else {
      return ExportHtmlAsync(result, path, parameters.FormatJson, token);
    }
  }

  public static async Task ExportRawAsync(Response result, string path, bool formatJson = false, CancellationToken token = default) {
    if (string.IsNullOrEmpty(result.Content) && result.Exception.IsDefault) {
      return;
    }

    HttpStatusCode statusCode = result.StatusCode;
    string extension;
    string content;

    if (!result.Exception.IsDefault) {
      content = DefaultJsonContext.SerializeException(result.Exception);
      extension = "json";
    } else {
      if (formatJson) {
        content = FormatJson(result.Content).Message;
        extension = "json";
      } else {
        content = result.Content;
        extension = "html";
      }
    }

    string filename = Path.Join(path, $"response-{result.Id}-status-code-{(int)statusCode}.{extension}");

    await File.WriteAllTextAsync(filename, content, token);

    static Result FormatJson(string content) {
      try {
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        var json = JsonSerializer.Serialize(root, InputJsonContext.Default.JsonElement);
        return Result.Ok(json);
      } catch (JsonException) {
        return Result.Fail("Failed to format content as JSON");
      }
    }
  }
  public static async Task ExportHtmlAsync(Response result, string path, bool formatJson = false, CancellationToken token = default) {
    HttpStatusCode statusCode = result.StatusCode;
    string frameTitle;
    string content = string.IsNullOrWhiteSpace(result.Content) ? string.Empty : result.Content;
    string status;

    if (result.Exception.IsDefault) {
      status = $"{statusCode} ({(int)statusCode})";
      frameTitle = "Content:";
      if (formatJson) {
        try {
          using var doc = JsonDocument.Parse(content);
          var root = doc.RootElement;
          var json = JsonSerializer.Serialize(root, InputJsonContext.Default.JsonElement);
          content = $"<pre>{json}</pre>";
        } catch (JsonException) { } // Ignore - Keep content as is
      }
      content = content.Replace('\'', '\"');
    } else {
      status = "Exception (0)";
      frameTitle = "Exception:";
      content = $"<pre>{DefaultJsonContext.SerializeException(result.Exception)}</pre>";
    }

    string filename = Path.Join(path, $"response-{result.Id}-status-code-{(int)statusCode}.html");
    string contentFrame = content == string.Empty ?
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
    string headers = string.Empty;
    if (result.Headers.Any()) {
      headers =
      $"""
      <div class="table-section">
      {ToHtmlTable(result.Headers)}
      </div>
      """;
    }
    const string css =
"""
/* Reset and Base Styles */
*, *::before, *::after {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}
body {
  display: flex;
  flex-direction: column;
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  background-color: #f4f6f8;
  color: #333;
  min-height: 100vh;
  padding: 20px;
  font-size: small; /* Set font size to small */
}
/* Status Section Styles */
.status-section {
  display: flex;
  flex-direction: row;
  justify-content: center;
  align-items: center;
  background-color: #fff;
  padding: 10px 20px;
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  border-bottom-left-radius: 8px; /* Rounded bottom corners */
  border-bottom-right-radius: 8px;
  margin-bottom: 20px; /* Margin between status and table sections */
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}
.status-section h1.title {
  margin-right: 20px; /* Space between h1 and h2 */
  font-size: 2rem;
  /* Gradient Text: Dark Purple, Blue, and Pink */
  background: linear-gradient(to right, #6a0dad, #0000ff, #ff389b);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  color: transparent;
}
.status-section h1 {
  margin-left: 20px; /* Space between h1 and h2 */
  font-size: 2rem;
}
/* Section Styles */
.table-section, .iframe-section {
  background-color: #fff;
  border-radius: 8px;
  margin-bottom: 20px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  overflow: hidden;
}
/* Section Headers */
.iframe-section {
  padding: 10px;
  overflow: visible;
}
.iframe-section h2 {
  position: sticky;
  top: 0;
  background: #fff;
  font-size: 1.75rem;
  width: 100%;
  padding: 5px;
  margin-bottom: 15px;
  color: #000;
  border-bottom: 2px solid #6a0dad;
}
/* Table Styles */
table {
  width: 100%;
  border-collapse: separate; /* Changed from collapse to separate to allow border-radius */
  border-spacing: 0;
  table-layout: fixed;
  word-wrap: break-word;
}
col:first-child {
  width: 33.33%;
}
col:nth-child(2) {
  width: 66.66%;
}
th, td {
  padding: 12px 15px;
  text-align: left;
  vertical-align: top;
}
thead {
  background: #333;
  color: floralwhite;
}
/* Colored Background for Table Headers */
th {
  color: #fff;
  font-weight: bold;
  font-size: 1rem;
}
/* Remove individual column background colors */
th.header, td.header,
th.value, td.value {
  background-color: inherit;
  color: inherit;
}
/* Apply uniform background to all data cells */
tbody td {
  background-color: #f9f9f9;
  color: #333;
}

tr:nth-child(even) td {
  background-color: #f1f1f1;
}

tr td:last-child {
  border-top-right-radius: 8px;
  border-bottom-right-radius: 8px;
}

/* Hover Effect for Table Rows */
tr:hover td {
  background-color: #e6f7ff;
}

/* Iframe Section Styles */
.iframe-container {
  display: flex;
  flex-direction: column;
}
iframe {
  width: 100%;
  height: 1000px; /* Increased height to 1000px */
  border: none;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  background-color: #fff; /* Removed gray background */
  padding: 10px;
}
/* Responsive Design */
@media (max-width: 768px) {
  .status-section {
    flex-direction: column;
    align-items: center;
  }
  .status-section h1.title {
    margin-right: 0;
    margin-bottom: 10px;
    font-size: 2rem;
  }
  .status-section h2 {
    font-size: 1rem;
  }
  .iframe-section h2 {
    font-size: 1.5rem;
  }
  th, td {
    padding: 10px 12px;
  }
  iframe {
    height: 600px; /* Adjusted height for smaller screens */
  }
}
""";
    string body =
$$"""
<!DOCTYPE html>
<html lang="en">
<title>Response: {{result.Id}}</title>
<head>
<meta name="viewport" content="width=device-width, initial-scale=1" charset="utf-8"/>
<style>
{{css}}
</style>
</head>
<body>
<div class="status-section">
    <h1 class="title">Response: {{result.Id}}</h1>
    <h1>{{status}}</h1>
</div>
{{headers}}
<div class="iframe-section">
{{contentFrame}}
</div>
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
    sb.AppendLine("<colgroup>");
    sb.AppendLine("<col><col>");
    sb.AppendLine("</colgroup>");
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