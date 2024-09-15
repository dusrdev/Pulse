using System.Text.Json;

using Pulse.Configuration;

using Sharpify;

namespace Pulse.Core;

public static class Exporter {
	public static void ExportHtml(RequestResult result, int index) {
		string path = Utils.Env.PathInBaseDirectory($"results/{index}.html");
        string frameTitle;
        string content;

        if (result.Exception is not null) {
            frameTitle = "Exception:";
            content = JsonSerializer.Serialize(result.Exception, Services.Instance.JsonOptions);
        } else {
            frameTitle = "Content:";
            content = result.Content ?? "";
        }
        string body =
		$$"""
<!DOCTYPE html>
<html lang="en">
<title>Result: {{index}}</title>
<head>
<meta name="viewport" content="width=device-width, initial-scale=1" charset="utf-8"/>
<style>
:root {
  --highlight: #A0FF00;
  --checked: #ff4929;
}
body {
  background-color: black;
}
h1 {
  font-family: 'Lucida Bright';
  font-size: 200%;
  color: var(--highlight);
}
h1.date {
  color: var(--highlight);
  text-align: center;
}
h2 {
  color: var(--highlight);
  text-align: left;
}
</style>
</head>
<body>
<h1 class="title">Result: {{index}}</h1>
<div>
<h2>StatusCode: {{result.StatusCode ?? 0}}</h2>
</div>
<div>
<h2>{{frameTitle}}</h2>
<iframe title="Content" width="600" height="300" srcdoc="{{content}}"></iframe>
</div>
</body>
""";
		File.WriteAllText(path, body);
	}
}