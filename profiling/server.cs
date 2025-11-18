#:sdk Microsoft.Net.Sdk.Web@*
#:property CompileAot=false
#:property PublishTrimmed=false

var builder = WebApplication.CreateBuilder();

const string address = "http://0.0.0.0:3000";

builder.WebHost.UseUrls(address);

var app = builder.Build();

app.MapGet("/", static () => "Hello!");

app.MapGet("/json", static () => {
	const string payload =
"""
{
  "data": [
    { "id": 1, "name": "Item 1", "value": 10.5, "active": true, "tags": ["a", "b", "c"] },
    { "id": 2, "name": "Item 2", "value": 20.0, "active": false, "tags": ["b", "d"] },
    { "id": 3, "name": "Item 3", "value": 5.75, "active": true, "tags": ["a", "c", "e"] },
    { "id": 4, "name": "Item 4", "value": 100.2, "active": true, "tags": [] },
    { "id": 5, "name": "Item 5", "value": 0.5, "active": false, "tags": ["f"] },
    { "id": 6, "name": "Item 6", "value": 10.5, "active": true, "tags": ["a", "b", "c"] },
    { "id": 7, "name": "Item 7", "value": 20.0, "active": false, "tags": ["b", "d"] },
    { "id": 8, "name": "Item 8", "value": 5.75, "active": true, "tags": ["a", "c", "e"] },
    { "id": 9, "name": "Item 9", "value": 100.2, "active": true, "tags": [] },
    { "id": 10, "name": "Item 10", "value": 0.5, "active": false, "tags": ["f"] },
    { "id": 11, "name": "Item 11", "value": 10.5, "active": true, "tags": ["a", "b", "c"] },
    { "id": 12, "name": "Item 12", "value": 20.0, "active": false, "tags": ["b", "d"] },
    { "id": 13, "name": "Item 13", "value": 5.75, "active": true, "tags": ["a", "c", "e"] },
    { "id": 14, "name": "Item 14", "value": 100.2, "active": true, "tags": [] },
    { "id": 15, "name": "Item 15", "value": 0.5, "active": false, "tags": ["f"] },
    { "id": 16, "name": "Item 16", "value": 10.5, "active": true, "tags": ["a", "b", "c"] },
    { "id": 17, "name": "Item 17", "value": 20.0, "active": false, "tags": ["b", "d"] },
    { "id": 18, "name": "Item 18", "value": 5.75, "active": true, "tags": ["a", "c", "e"] },
    { "id": 19, "name": "V20", "value": 100.2, "active": true, "tags": [] },
    { "id": 20, "name": "Item 20", "value": 0.5, "active": false, "tags": ["f"] },
    { "id": 21, "name": "Item 21", "value": 10.5, "active": true, "tags": ["a", "b", "c"] },
    { "id": 22, "name": "Item 22", "value": 20.0, "active": false, "tags": ["b", "d"] },
    { "id": 23, "name": "Item 23", "value": 5.75, "active": true, "tags": ["a", "c", "e"] },
    { "id": 24, "name": "Item 24", "value": 100.2, "active": true, "tags": [] },
    { "id": 25, "name": "Item 25", "value": 0.5, "active": false, "tags": ["f"] },
    { "id": 26, "name": "Item 26", "value": 10.5, "active": true, "tags": ["a", "b", "c"] },
    { "id": 27, "name": "Item 27", "value": 20.0, "active": false, "tags": ["b", "d"] },
    { "id": 28, "name": "Item 28", "value": 5.75, "active": true, "tags": ["a", "c", "e"] },
    { "id": 29, "name": "Item 29", "value": 100.2, "active": true, "tags": [] },
    { "id": 30, "name": "Item 30", "value": 0.5, "active": false, "tags": ["f"] },
    { "id": 31, "name": "Item 31", "value": 10.5, "active": true, "tags": ["a", "b", "c"] },
    { "id": 32, "name": "Item 32", "value": 20.0, "active": false, "tags": ["b", "d"] },
    { "id": 33, "name": "Item 33", "value": 5.75, "active": true, "tags": ["a", "c", "e"] },
    { "id": 34, "name": "Item 34", "value": 100.2, "active": true, "tags": [] },
    { "id": 35, "name": "Item 35", "value": 0.5, "active": false, "tags": ["f"] },
    { "id": 36, "name": "Item 36", "value": 10.5, "active": true, "tags": ["a", "b", "c"] },
    { "id": 37, "name": "Item 37", "value": 20.0, "active": false, "tags": ["b", "d"] },
    { "id": 38, "name": "Item 38", "value": 5.75, "active": true, "tags": ["a", "c", "e"] },
    { "id": 39, "name": "Item 39", "value": 100.2, "active": true, "tags": [] },
    { "id": 40, "name": "Item 40", "value": 0.5, "active": false, "tags": ["f"] },
    { "id": 41, "name": "Item 41", "value": 10.5, "active": true, "tags": ["a", "b", "c"] },
    { "id": 42, "name": "Item 42", "value": 20.0, "active": false, "tags": ["b", "d"] },
    { "id": 43, "name": "Item 43", "value": 5.75, "active": true, "tags": ["a", "c", "e"] },
    { "id": 44, "name": "Item 44", "value": 100.2, "active": true, "tags": [] },
    { "id": 45, "name": "Item 45", "value": 0.5, "active": false, "tags": ["f"] },
    { "id": 46, "name": "Item 46", "value": 10.5, "active": true, "tags": ["a", "b", "c"] },
    { "id": 47, "name": "Item 47", "value": 20.0, "active": false, "tags": ["b", "d"] },
    { "id": 48, "name": "Item 48", "value": 5.75, "active": true, "tags": ["a", "c", "e"] },
    { "id": 49, "name": "Item 49", "value": 100.2, "active": true, "tags": [] },
    { "id": 50, "name": "Item 50", "value": 0.5, "active": false, "tags": ["f"] }
  ],
  "metadata": {
    "count": 50,
    "timestamp": "2025-11-04T10:42:00Z",
    "source": "pgo-test-server"
  }
}
""";
	return payload;
});

app.MapGet("/html", static () => {
	const string payload =
"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Test Payload</title>
    <style>
        body { font-family: sans-serif; line-height: 1.6; }
        .container { max-width: 800px; margin: 20px auto; padding: 20px; }
        .item { border-bottom: 1px solid #eee; padding-bottom: 10px; margin-bottom: 10px; }
    </style>
</head>
<body>
    <div class="container">
        <h1>Large HTML Test Data</h1>
        <p>This document contains a significant amount of text to simulate a real-world webpage. The goal is to provide a non-trivial payload for HTTP load testing, specifically for profiling the client's parsing and handling capabilities.</p>

        <div class="item">
            <h2>Section 1</h2>
            <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.</p>
        </div>
        <div class="item">
            <h2>Section 2</h2>
            <p>Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Vestibulum tortor quam, feugiat vitae, ultricies eget, tempor sit amet, ante. Donec eu libero sit amet quam egestas semper. Aenean ultricies mi vitae est. Mauris placerat eleifend leo. Quisque sit amet est et sapien ullamcorper pharetra. Vestibulum erat wisi, condimentum sed, commodo vitae, ornare sit amet, wisi.</p>
        </div>
        <div class="item">
            <h2>Section 3</h2>
            <p>Curabitur bibendum, erat id consequat consequat, odio
            urna
            suscipit
            sapien,
            nec
            sollicitudin
            nisl
            erat
            vel
            enim.
            Maecenas
            viverra
            condimentum
            erat.
            Aenean
            porttitor,
            mauris
            eget
            aliquam
            dictum,
            massa
            erat
            sodales
            justo,
            ac
            rhoncus
            elit
            purus
            eget
            massa.
            Donec
            velit
            est,
            lobortis
            quis,
            vulputate
            sit
            amet,
            molestie
            id,
            magna.
            Vivamus
            consequat,
            felis
            id
            pulvinar
            ullamcorper,
            nunc
            erat
            id
            sapien,
            id
            faucibus
            sapien
            odio
            vel
            pede.</p>
        </div>
        <div class="item">
            <h2>Section 4</h2>
            <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.</p>
        </div>
        <div class="item">
            <h2>Section 5</h2>
            <p>Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Vestibulum tortor quam, feugiat vitae, ultricies eget, tempor sit amet, ante. Donec eu libero sit amet quam egestas semper. Aenean ultricies mi vitae est. Mauris placerat eleifend leo. Quisque sit amet est et sapien ullamcorper pharetra. Vestibulum erat wisi, condimentum sed, commodo vitae, ornare sit amet, wisi.</p>
        </div>
        <div class="item">
            <h2>Section 6</h2>
            <p>Curabitur bibendum, erat id consequat consequat, odio
            urna
            suscipit
            sapien,
            nec
            sollicitudin
            nisl
            erat
            vel
            enim.
            Maecenas
            viverra
            condimentum
            erat.
            Aenean
            porttitor,
            mauris
            eget
            aliquam
            dictum,
            massa
            erat
            sodales
            justo,
            ac
            rhoncus
            elit
            purus
            eget
            massa.
            Donec
            velit
            est,
            lobortis
            quis,
            vulputate
            sit
            amet,
            molestie
            id,
            magna.
            Vivamus
            consequat,
            felis
            id
            pulvinar
            ullamcorper,
            nunc
            erat
            id
            sapien,
            id
            faucibus
            sapien
            odio
            vel
            pede.</p>
        </div>
        <div class="item">
            <h2>Section 7</h2>
            <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.</p>
        </div>
        <div class="item">
            <h2>Section 8</h2>
            <p>Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Vestibulum tortor quam, feugiat vitae, ultricies eget, tempor sit amet, ante. Donec eu libero sit amet quam egestas semper. Aenean ultricies mi vitae est. Mauris placerat eleifend leo. Quisque sit amet est et sapien ullamcorper pharetra. Vestibulum erat wisi, condimentum sed, commodo vitae, ornare sit amet, wisi.</p>
        </div>
        <div class="item">
            <h2>Section 9</h2>
            <p>Curabitur bibendum, erat id consequat consequat, odio
            urna
            suscipit
            sapien,
            nec
            sollicitudin
            nisl
            erat
            vel
            enim.
            Maecenas
            viverra
            condimentum
            erat.
            Aenean
            porttitor,
            mauris
            eget
            aliquam
            dictum,
            massa
            erat
            sodales
            justo,
            ac
            rhoncus
            elit
            purus
            eget
            massa.
            Donec
            velit
            est,
            lobortis
            quis,
            vulputate
            sit
            amet,
            molestie
            id,
            magna.
            Vivamus
            consequat,
    </div>
</body>
</html>
""";
	return payload;
});

app.Run();