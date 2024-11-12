using System.Net;
using System.Text;

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
            dirInfo.GetFiles().Length.Should().Be(10, "because 10 files were created");

            // Act
            Exporter.ClearFiles(dirInfo.FullName);

            // Assert
            dirInfo.GetFiles().Length.Should().Be(0, "because all files were deleted");
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Fact]
    public void Exporter_ToHtmlTable_ContainsAllHeaders() {
        // Arrange
        List<KeyValuePair<string, IEnumerable<string>>> headers = new() {
                new("Content-Type", ["application/json"]),
                new("X-Custom-Header", ["value1", "value2"])
            };

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
            fileContent.Should().Contain(header.Key);
            foreach (var value in header.Value) {
                fileContent.Should().Contain(value);
            }
        }
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
            file.Length.Should().Be(1, "because 1 file was created");
            file[0].Name.Should().Be("response-1337-status-code-200.html", "because the file name is correct");
        } finally {
            dirInfo.Delete(true);
        }
    }

    [Fact]
    public async Task Exporter_ExportHtmlAsync_ContainsAllHeaders() {
        // Arrange
        var dirInfo = Directory.CreateTempSubdirectory();
        try {
            List<KeyValuePair<string, IEnumerable<string>>> headers = new() {
                new("Content-Type", ["application/json"]),
                new("X-Custom-Header", ["value1", "value2"])
            };

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
            await Exporter.ExportHtmlAsync(response, dirInfo.FullName);

            // Assert
            var file = dirInfo.GetFiles();
            file.Length.Should().Be(1, "because 1 file was created");
            var fileContent = File.ReadAllText(file[0].FullName);

            foreach (var header in headers) {
                fileContent.Should().Contain(header.Key);
                foreach (var value in header.Value) {
                    fileContent.Should().Contain(value);
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
            file.Length.Should().Be(1, "because 1 file was created");
            var fileContent = File.ReadAllText(file[0].FullName);
            fileContent.Should().Contain("Hello World", "because the content is present");
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
            var exception = new StrippedException(nameof(Exception), "test", "");

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
            file.Length.Should().Be(1, "because 1 file was created");
            var fileContent = File.ReadAllText(file[0].FullName);
            fileContent.Should().NotContain("Hello World", "because the content is not present");
            fileContent.Should().Contain(exception.Message, "because the exception is present");
        } finally {
            dirInfo.Delete(true);
        }
    }
}