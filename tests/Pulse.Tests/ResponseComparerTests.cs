using System.Net;

using Pulse.Models;

namespace Pulse.Tests;

public class ResponseComparerTests {
    [Test]
    public async Task Equals_NotUsingFullEquality_ComparesByContentLength() {
        var parameters = new Parameters(new ParametersBase { UseFullEquality = false }, CancellationToken.None);
        var comparer = new ResponseComparer(parameters);
        var original = CreateResponse(1, HttpStatusCode.OK, "foo");
        var candidate = original with {
            Id = 2,
            Content = "bar",
            ContentLength = 3
        };

        await Assert.That(comparer.Equals(original, candidate)).IsTrue();
        await Assert.That(comparer.GetHashCode(original)).IsEqualTo(comparer.GetHashCode(candidate));
    }

    [Test]
    public async Task Equals_NotUsingFullEquality_IdentifiesDifferentLengths() {
        var parameters = new Parameters(new ParametersBase { UseFullEquality = false }, CancellationToken.None);
        var comparer = new ResponseComparer(parameters);
        var original = CreateResponse(1, HttpStatusCode.OK, "foo");
        var different = original with {
            Id = 2,
            Content = "foobar",
            ContentLength = 6
        };

        await Assert.That(comparer.Equals(original, different)).IsFalse();
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