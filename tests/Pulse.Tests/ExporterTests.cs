using System.Net;
using System.Text;
using System.Text.Json;

using Pulse.Core;
using Pulse.Models;

namespace Pulse.Tests;

public class ExporterTests {
    [Test]
    public async Task Exporter_ClearFiles() {
        var dirInfo = Directory.CreateTempSubdirectory();
        try {
            var faker = new Faker();
            for (int i = 0; i < 10; i++) {
                var file = faker.System.FileName();
                var content = faker.Lorem.Paragraph();
                var path = Path.Join(dirInfo.FullName, file);
                File.WriteAllText(path, content);
            }

            await Assert.That(dirInfo.GetFiles().Length).IsEqualTo(10);

            Exporter.ClearFiles(dirInfo.FullName);

            await Assert.That(dirInfo.GetFiles().Length).IsEqualTo(0);
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Test]
    public async Task Exporter_ToHtmlTable_ContainsAllHeaders() {
        List<KeyValuePair<string, IEnumerable<string>>> headers = [
            new("Content-Type", ["application/json"]),
            new("X-Custom-Header", ["value1", "value2"])
        ];

        const string content = "Hello World";

        var response = new Response {
            Id = 1337,
            StatusCode = HttpStatusCode.OK,
            Content = content,
            ContentLength = Encoding.Default.GetByteCount(content),
            Headers = headers,
            Exception = StrippedException.Default,
            Latency = TimeSpan.FromSeconds(1),
            CurrentConcurrentConnections = 1
        };

        var fileContent = Exporter.ToHtmlTable(response.Headers);

        foreach (var header in headers) {
            await Assert.That(fileContent.Contains(header.Key)).IsTrue();
            foreach (var value in header.Value) {
                await Assert.That(fileContent.Contains(value)).IsTrue();
            }
        }
    }

    [Test]
    public async Task Exporter_Raw_NotSuccess_ContainsAllHeadersInJson() {
        List<KeyValuePair<string, IEnumerable<string>>> headers = [
            new("Content-Type", ["application/json"]),
            new("X-Custom-Header", ["value1", "value2"])
        ];

        var response = new Response {
            Id = 1337,
            StatusCode = HttpStatusCode.BadGateway,
            Content = string.Empty,
            ContentLength = 0,
            Headers = headers,
            Exception = StrippedException.Default,
            Latency = TimeSpan.FromSeconds(1),
            CurrentConcurrentConnections = 1
        };

        var expectedFileName = $"response-1337-status-code-502.json";
        await Exporter.ExportRawAsync(response, string.Empty, false, CancellationToken.None);

        await Assert.That(File.Exists(expectedFileName)).IsTrue();

        var fileContent = await File.ReadAllTextAsync(expectedFileName, CancellationToken.None);

        await Assert.That(fileContent.Contains("502")).IsTrue();
        foreach (var header in headers) {
            await Assert.That(fileContent.Contains(header.Key)).IsTrue();
            foreach (var value in header.Value) {
                await Assert.That(fileContent.Contains(value)).IsTrue();
            }
        }

        File.Delete(expectedFileName);
    }

    [Test]
    public async Task Exporter_Raw_Success_ContainsOnlyContent() {
        const string expectedContent = "Hello World";

        var response = new Response {
            Id = 1337,
            StatusCode = HttpStatusCode.OK,
            Content = expectedContent,
            ContentLength = expectedContent.Length,
            Headers = [],
            Exception = StrippedException.Default,
            Latency = TimeSpan.FromSeconds(1),
            CurrentConcurrentConnections = 1
        };

        var expectedFileName = $"response-1337-status-code-200.html";
        await Exporter.ExportRawAsync(response, string.Empty, false, CancellationToken.None);

        await Assert.That(File.Exists(expectedFileName)).IsTrue();

        var fileContent = await File.ReadAllTextAsync(expectedFileName, CancellationToken.None);

        await Assert.That(fileContent).IsEqualTo(expectedContent);

        File.Delete(expectedFileName);
    }

    [Test]
    public async Task Exporter_Raw_NotSuccess_ButHasContent_ContainsOnlyContent() {
        const string expectedContent = "Hello World";

        var response = new Response {
            Id = 1337,
            StatusCode = HttpStatusCode.BadGateway,
            Content = expectedContent,
            ContentLength = expectedContent.Length,
            Headers = [],
            Exception = StrippedException.Default,
            Latency = TimeSpan.FromSeconds(1),
            CurrentConcurrentConnections = 1
        };

        var expectedFileName = $"response-1337-status-code-502.html";
        await Exporter.ExportRawAsync(response, string.Empty, false, CancellationToken.None);

        await Assert.That(File.Exists(expectedFileName)).IsTrue();

        var fileContent = await File.ReadAllTextAsync(expectedFileName, CancellationToken.None);

        await Assert.That(fileContent).IsEqualTo(expectedContent);

        File.Delete(expectedFileName);
    }

    [Test]
    public async Task Exporter_ExportHtmlAsync_CorrectFileName() {
        var dirInfo = Directory.CreateTempSubdirectory();
        try {
            const string content = "Hello World";

            var response = new Response {
                Id = 1337,
                StatusCode = HttpStatusCode.OK,
                Content = content,
                ContentLength = Encoding.Default.GetByteCount(content),
                Headers = [],
                Exception = StrippedException.Default,
                Latency = TimeSpan.FromSeconds(1),
                CurrentConcurrentConnections = 1
            };

            await Exporter.ExportHtmlAsync(response, dirInfo.FullName, token: CancellationToken.None);

            var file = dirInfo.GetFiles();
            await Assert.That(file.Length).IsEqualTo(1);
            await Assert.That(file[0].Name).IsEqualTo("response-1337-status-code-200.html");
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Test]
    public async Task Exporter_ExportHtmlAsync_ContainsAllHeaders() {
        var dirInfo = Directory.CreateTempSubdirectory();
        try {
            List<KeyValuePair<string, IEnumerable<string>>> headers = [
                new("Content-Type", ["application/json"]),
                new("X-Custom-Header", ["value1", "value2"])
            ];

            const string expectedContent = "Hello World";

            var response = new Response {
                Id = 1337,
                StatusCode = HttpStatusCode.OK,
                Content = expectedContent,
                ContentLength = Encoding.Default.GetByteCount(expectedContent),
                Headers = headers,
                Exception = StrippedException.Default,
                Latency = TimeSpan.FromSeconds(1),
                CurrentConcurrentConnections = 1
            };

            await Exporter.ExportHtmlAsync(response, dirInfo.FullName, token: CancellationToken.None);

            var file = dirInfo.GetFiles();
            await Assert.That(file.Length).IsEqualTo(1);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName, CancellationToken.None);

            foreach (var header in headers) {
                await Assert.That(fileContent.Contains(header.Key)).IsTrue();
                foreach (var value in header.Value) {
                    await Assert.That(fileContent.Contains(value)).IsTrue();
                }
            }
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Test]
    public async Task Exporter_ExportHtmlAsync_WithoutException_HasContent() {
        var dirInfo = Directory.CreateTempSubdirectory();
        try {
            const string expectedContent = "Hello World";

            var response = new Response {
                Id = 1337,
                StatusCode = HttpStatusCode.OK,
                Content = expectedContent,
                ContentLength = Encoding.Default.GetByteCount(expectedContent),
                Headers = [],
                Exception = StrippedException.Default,
                Latency = TimeSpan.FromSeconds(1),
                CurrentConcurrentConnections = 1
            };

            await Exporter.ExportHtmlAsync(response, dirInfo.FullName, token: CancellationToken.None);

            var file = dirInfo.GetFiles();
            await Assert.That(file.Length).IsEqualTo(1);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName, CancellationToken.None);
            await Assert.That(fileContent.Contains(expectedContent)).IsTrue();
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Test]
    public async Task Exporter_ExportHtmlAsync_RawHtml() {
        var dirInfo = Directory.CreateTempSubdirectory();
        try {
            const string expectedContent = "Hello World";

            var response = new Response {
                Id = 1337,
                StatusCode = HttpStatusCode.OK,
                Content = expectedContent,
                ContentLength = Encoding.Default.GetByteCount(expectedContent),
                Headers = [],
                Exception = StrippedException.Default,
                Latency = TimeSpan.FromSeconds(1),
                CurrentConcurrentConnections = 1
            };

            await Exporter.ExportRawAsync(response, dirInfo.FullName, token: CancellationToken.None);

            var file = dirInfo.GetFiles();
            await Assert.That(file.Length).IsEqualTo(1);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName, CancellationToken.None);
            await Assert.That(fileContent).IsEqualTo(expectedContent);
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Test]
    public async Task Exporter_ExportHtmlAsync_RawJson() {
        var dirInfo = Directory.CreateTempSubdirectory();
        try {
            var expectedContent = JsonSerializer.Serialize(new ParametersBase(), ModelsJsonContext.Default.ParametersBase);

            var response = new Response {
                Id = 1337,
                StatusCode = HttpStatusCode.OK,
                Content = expectedContent,
                ContentLength = Encoding.Default.GetByteCount(expectedContent),
                Headers = [],
                Exception = StrippedException.Default,
                Latency = TimeSpan.FromSeconds(1),
                CurrentConcurrentConnections = 1
            };

            await Exporter.ExportRawAsync(response, dirInfo.FullName, token: CancellationToken.None);

            var file = dirInfo.GetFiles();
            await Assert.That(file.Length).IsEqualTo(1);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName, CancellationToken.None);
            await Assert.That(fileContent).IsEqualTo(expectedContent);
            await Assert.That(fileContent.Contains(Environment.NewLine)).IsFalse();
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Test]
    public async Task Exporter_ExportHtmlAsync_RawJson_Formatted() {
        var dirInfo = Directory.CreateTempSubdirectory();
        try {
            var content = JsonSerializer.Serialize(new ParametersBase(), ModelsJsonContext.Default.ParametersBase);

            var response = new Response {
                Id = 1337,
                StatusCode = HttpStatusCode.OK,
                Content = content,
                ContentLength = Encoding.Default.GetByteCount(content),
                Headers = [],
                Exception = StrippedException.Default,
                Latency = TimeSpan.FromSeconds(1),
                CurrentConcurrentConnections = 1
            };

            await Exporter.ExportRawAsync(response, dirInfo.FullName, true, CancellationToken.None);

            var file = dirInfo.GetFiles();
            await Assert.That(file.Length).IsEqualTo(1);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName, CancellationToken.None);
            await Assert.That(fileContent.Contains(Environment.NewLine)).IsTrue();
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Test]
    public async Task Exporter_ExportHtmlAsync_RawJson_Exception() {
        var dirInfo = Directory.CreateTempSubdirectory();
        try {
            var content = string.Empty;

            var response = new Response {
                Id = 1337,
                StatusCode = HttpStatusCode.OK,
                Content = content,
                ContentLength = Encoding.Default.GetByteCount(content),
                Headers = [],
                Exception = new StrippedException("test", "test"),
                Latency = TimeSpan.FromSeconds(1),
                CurrentConcurrentConnections = 1
            };

            await Exporter.ExportRawAsync(response, dirInfo.FullName, true, CancellationToken.None);

            var file = dirInfo.GetFiles();
            await Assert.That(file.Length).IsEqualTo(1);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName, CancellationToken.None);
            await Assert.That(fileContent.Contains("test")).IsTrue();
            await Assert.That(fileContent.Contains(Environment.NewLine)).IsTrue();
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Test]
    public async Task Exporter_ExportHtmlAsync_WithException_HasExceptionAndNoContent() {
        var dirInfo = Directory.CreateTempSubdirectory();
        try {
            const string content = "Hello World";
            var exception = new StrippedException(nameof(Exception), "test");

            var response = new Response {
                Id = 1337,
                StatusCode = HttpStatusCode.OK,
                Content = content,
                ContentLength = Encoding.Default.GetByteCount(content),
                Headers = [],
                Exception = exception,
                Latency = TimeSpan.FromSeconds(1),
                CurrentConcurrentConnections = 1
            };

            await Exporter.ExportHtmlAsync(response, dirInfo.FullName, token: CancellationToken.None);

            var file = dirInfo.GetFiles();
            await Assert.That(file.Length).IsEqualTo(1);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName, CancellationToken.None);
            await Assert.That(fileContent.Contains("Hello World")).IsFalse();
            await Assert.That(fileContent.Contains(exception.Message)).IsTrue();
        } finally {
            dirInfo.Delete(true);
        }
    }
}
