using System.Net;

using Pulse.Configuration;
using Pulse.Models;

namespace Pulse.Tests.Unit;

public class ResponseComparerTests {
    [Fact]
    public void Equals_NotUsingFullEquality_ComparesByContentLength() {
        // Arrange
        var parameters = new Parameters(new ParametersBase { UseFullEquality = false }, CancellationToken.None);
        var comparer = new ResponseComparer(parameters);
        var original = CreateResponse(1, HttpStatusCode.OK, "foo");
        var candidate = original with {
            Id = 2,
            Content = "bar",
            ContentLength = 3
        };

        // Act + Assert
        Assert.True(comparer.Equals(original, candidate));
        Assert.Equal(comparer.GetHashCode(original), comparer.GetHashCode(candidate));
    }

    [Fact]
    public void Equals_NotUsingFullEquality_IdentifiesDifferentLengths() {
        // Arrange
        var parameters = new Parameters(new ParametersBase { UseFullEquality = false }, CancellationToken.None);
        var comparer = new ResponseComparer(parameters);
        var original = CreateResponse(1, HttpStatusCode.OK, "foo");
        var different = original with {
            Id = 2,
            Content = "foobar",
            ContentLength = 6
        };

        // Act + Assert
        Assert.False(comparer.Equals(original, different));
    }

    private static Response CreateResponse(int id, HttpStatusCode statusCode, string content) {
        return new Response {
            Id = id,
            StatusCode = statusCode,
            Headers = Array.Empty<KeyValuePair<string, IEnumerable<string>>>(),
            Content = content,
            ContentLength = content.Length,
            Latency = TimeSpan.FromMilliseconds(10),
            Exception = StrippedException.Default,
            CurrentConcurrentConnections = 1
        };
    }
}