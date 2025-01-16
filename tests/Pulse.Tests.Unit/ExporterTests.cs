using System.Net;
using System.Text;
using System.Text.Json;

using Pulse.Configuration;

using Pulse.Core;

namespace Pulse.Tests.Unit;

public class ExporterTests {
    [Fact]
    public void Exporter_ClearFiles() {
        // Arrange
        var dirInfo = Directory.CreateTempSubdirectory();
        try {
            var faker = new Faker();
            for (int i = 0; i < 10; i++) {
                var file = faker.System.FileName();
                var content = faker.Lorem.Paragraph();
                var path = Path.Join(dirInfo.FullName, file);
                File.WriteAllText(path, content);
            }

            // Assert
            Assert.Equal(10, dirInfo.GetFiles().Length);

            // Act
            Exporter.ClearFiles(dirInfo.FullName);

            // Assert
            Assert.Empty(dirInfo.GetFiles());
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Fact]
    public void Exporter_ToHtmlTable_ContainsAllHeaders() {
        // Arrange
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

        // Act
        var fileContent = Exporter.ToHtmlTable(response.Headers);

        // Assert
        foreach (var header in headers) {
            Assert.Contains(header.Key, fileContent);
            foreach (var value in header.Value) {
                Assert.Contains(value, fileContent);
            }
        }
    }

    [Fact]
    public async Task Exporter_Raw_NotSuccess_ContainsAllHeadersInJson() {
        // Arrange
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

        // Act
        var expectedFileName = $"response-1337-status-code-502.json";
        await Exporter.ExportRawAsync(response, string.Empty, false);

        Assert.True(File.Exists(expectedFileName));

        var fileContent = await File.ReadAllTextAsync(expectedFileName);

        // Assert
        Assert.Contains("502", fileContent);
        foreach (var header in headers) {
            Assert.Contains(header.Key, fileContent);
            foreach (var value in header.Value) {
                Assert.Contains(value, fileContent);
            }
        }

        File.Delete(expectedFileName);
    }

    [Fact]
    public async Task Exporter_Raw_Success_ContainsOnlyContent() {
        // Arrange
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

        // Act
        var expectedFileName = $"response-1337-status-code-200.html";
        await Exporter.ExportRawAsync(response, string.Empty, false);

        Assert.True(File.Exists(expectedFileName));

        var fileContent = await File.ReadAllTextAsync(expectedFileName);

        // Assert
        Assert.Equal(expectedContent, fileContent);

        File.Delete(expectedFileName);
    }

    [Fact]
    public async Task Exporter_Raw_NotSuccess_ButHasContent_ContainsOnlyContent() {
        // Arrange
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

        // Act
        var expectedFileName = $"response-1337-status-code-502.html";
        await Exporter.ExportRawAsync(response, string.Empty, false);

        Assert.True(File.Exists(expectedFileName));

        var fileContent = await File.ReadAllTextAsync(expectedFileName);

        // Assert
        Assert.Equal(expectedContent, fileContent);

        File.Delete(expectedFileName);
    }

    [Fact]
    public async Task Exporter_ExportHtmlAsync_CorrectFileName() {
        // Arrange
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

            // Act
            await Exporter.ExportHtmlAsync(response, dirInfo.FullName);

            // Assert
            var file = dirInfo.GetFiles();
            Assert.Single(file);
            Assert.Equal("response-1337-status-code-200.html", file[0].Name);
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Fact]
    public async Task Exporter_ExportHtmlAsync_ContainsAllHeaders() {
        // Arrange
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

            // Act
            await Exporter.ExportHtmlAsync(response, dirInfo.FullName);

            // Assert
            var file = dirInfo.GetFiles();
            Assert.Single(file);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName);

            foreach (var header in headers) {
                Assert.Contains(header.Key, fileContent);
                foreach (var value in header.Value) {
                    Assert.Contains(value, fileContent);
                }
            }
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Fact]
    public async Task Exporter_ExportHtmlAsync_WithoutException_HasContent() {
        // Arrange
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

            // Act
            await Exporter.ExportHtmlAsync(response, dirInfo.FullName);

            // Assert
            var file = dirInfo.GetFiles();
            Assert.Single(file);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName);
            Assert.Contains(expectedContent, fileContent);
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Fact]
    public async Task Exporter_ExportHtmlAsync_RawHtml() {
        // Arrange
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

            // Act
            await Exporter.ExportRawAsync(response, dirInfo.FullName);

            // Assert
            var file = dirInfo.GetFiles();
            Assert.Single(file);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName);
            Assert.Equal(expectedContent, fileContent);
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Fact]
    public async Task Exporter_ExportHtmlAsync_RawJson() {
        // Arrange
        var dirInfo = Directory.CreateTempSubdirectory();
        try {
            var options = new JsonSerializerOptions {
                WriteIndented = false
            };

            var expectedContent = JsonSerializer.Serialize(new ParametersBase(), options);

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

            // Act
            await Exporter.ExportRawAsync(response, dirInfo.FullName);

            // Assert
            var file = dirInfo.GetFiles();
            Assert.Single(file);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName);
            Assert.Equal(expectedContent, fileContent);
            Assert.DoesNotContain(Environment.NewLine, fileContent);
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Fact]
    public async Task Exporter_ExportHtmlAsync_RawJson_Formatted() {
        // Arrange
        var dirInfo = Directory.CreateTempSubdirectory();
        try {
            var options = new JsonSerializerOptions {
                WriteIndented = false
            };

            var content = JsonSerializer.Serialize(new ParametersBase(), options);

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

            // Act
            await Exporter.ExportRawAsync(response, dirInfo.FullName, true);

            // Assert
            var file = dirInfo.GetFiles();
            Assert.Single(file);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName);
            Assert.Contains(Environment.NewLine, fileContent);
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Fact]
    public async Task Exporter_ExportHtmlAsync_RawJson_Exception() {
        // Arrange
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

            // Act
            await Exporter.ExportRawAsync(response, dirInfo.FullName, true);

            // Assert
            var file = dirInfo.GetFiles();
            Assert.Single(file);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName);
            Assert.Contains("test", fileContent);
            Assert.Contains(Environment.NewLine, fileContent);
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Fact]
    public async Task Exporter_ExportHtmlAsync_WithException_HasExceptionAndNoContent() {
        // Arrange
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

            // Act
            await Exporter.ExportHtmlAsync(response, dirInfo.FullName);

            // Assert
            var file = dirInfo.GetFiles();
            Assert.Single(file);
            var fileContent = await File.ReadAllTextAsync(file[0].FullName);
            Assert.DoesNotContain("Hello World", fileContent);
            Assert.Contains(exception.Message, fileContent);
        } finally {
            dirInfo.Delete(true);
        }
    }
}