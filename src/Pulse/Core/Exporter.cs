using System.Net;
using System.Text.Json;

using Pulse.Configuration;

using Sharpify;

namespace Pulse.Core;

public static class Exporter {
  public static void ExportHtml(RequestResult result, int index) {
    string basePath = Utils.Env.PathInBaseDirectory("results/");
    Directory.CreateDirectory(basePath);
    string path = Path.Join(basePath, $"{index}.html");
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
    string body =
$$"""
<!DOCTYPE html>
<html lang="en">
<title>Result: {{index}}</title>
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
<h1 class="title">Result: {{index}}</h1>
<div>
<h2>StatusCode: {{statusCode}} ({{(int)statusCode}})</h2>
</div>
<div class="iframe-container">
<h2>{{frameTitle}}</h2>
<iframe title="Content" width="100%" height="100%" srcdoc='{{content}}'></iframe>
</div>
</body>
""";
    File.WriteAllText(path, body);
  }
}