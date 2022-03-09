using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tests.UnitTests;

[TestFixture]
public class ExternalTaskClientTests
{
    private static readonly Mock<HttpClient> _httpClient = new();
    private readonly IExternalTaskClient _sut = new ExternalTaskClient(_httpClient.Object);

    [Test]
    public async Task FetchAndLock_returns_list_of_locked_external_tasks()
    {
        var id = Guid.NewGuid();

        _httpClient.Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(@$"
                [
                    {{
                        ""id"": ""{id}"",
                        ""topicName"": ""topic"",
                        ""workerId"": ""worker"",
                        ""variables"": {{
                            ""var"": {{
                                ""value"": ""<root/>"",
                                ""type"": ""String"",
                                ""valueInfo"": {{
                                    ""objectTypeName"": ""XML"",
                                    ""serializationDataFormat"": ""xml"",
                                    ""filename"": ""root.xml"",
                                    ""mimetype"": ""application/xml"",
                                    ""encoding"": ""utf-8"",
                                }}
                            }}
                        }},
                    }}
                ]
            ") });

        var response = await _sut.FetchAndLock(new FetchExternalTasksDto 
        {
        }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.Length, Is.EqualTo(1));
            Assert.That(response[0].Id, Is.EqualTo(id));
            Assert.That(response[0].TopicName, Is.EqualTo("topic"));
            Assert.That(response[0].WorkerId, Is.EqualTo("worker"));
            Assert.That(response[0].Variables["var"].Value.ToString(), Is.EqualTo("<root/>"));
            Assert.That(response[0].Variables["var"].Type, Is.EqualTo("String"));

            var valueInfo = response[0].Variables["var"].ValueInfo ?? new ValueInfoDto();
            Assert.That(valueInfo.ObjectTypeName, Is.EqualTo("XML"));
            Assert.That(valueInfo.SerializationDataFormat, Is.EqualTo("xml"));
            Assert.That(valueInfo.FileName, Is.EqualTo("root.xml"));
            Assert.That(valueInfo.MimeType, Is.EqualTo("application/xml"));
            Assert.That(valueInfo.Encoding, Is.EqualTo("utf-8"));
        });
    }

    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.BadGateway)]
    public void FetchAndLock_throws_when_http_status_code_is_not_success(HttpStatusCode statusCode)
    {
        _httpClient.Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent("") });

        var exception = Assert.ThrowsAsync<HttpRequestException>(() => 
            _sut.FetchAndLock(new FetchExternalTasksDto(), CancellationToken.None)) ?? new HttpRequestException();

#if NET5_0_OR_GREATER
        Assert.That(exception.StatusCode, Is.EqualTo(statusCode));
#endif
    }

    [Test]
    public async Task Complete_sends_request_with_correct_values()
    {
        var id = Guid.NewGuid();

        _httpClient.Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });

        await _sut.Complete(id, new CompleteExternalTaskDto
        {
            WorkerId = "worker",
            Variables = new Dictionary<string, VariableDto>()
        }, CancellationToken.None);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        _httpClient.Verify(m => m.SendAsync(It.Is<HttpRequestMessage>(r => 
            r.Method == HttpMethod.Post &&
            r.RequestUri.ToString().Contains($"/{id}/")
        ), CancellationToken.None), Times.Once());
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.BadGateway)]
    public void Complete_throws_when_http_status_code_is_not_success(HttpStatusCode statusCode)
    {
        _httpClient.Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent("") });

        var dto = new CompleteExternalTaskDto
        {
            WorkerId = "worker",
            Variables = new Dictionary<string, VariableDto>() 
            {
                { "var", new VariableDto(
                    new JValue("value"), 
                    new ValueInfoDto 
                    {
                        Encoding = "",
                        FileName = "",
                        MimeType = "",
                        ObjectTypeName = "",
                        SerializationDataFormat = ""
                    })
                }
            }
        };

        var exception = Assert.ThrowsAsync<HttpRequestException>(() =>
            _sut.Complete(Guid.NewGuid(), dto, CancellationToken.None)) ?? new HttpRequestException();

#if NET5_0_OR_GREATER
        Assert.That(exception.StatusCode, Is.EqualTo(statusCode));
#endif
    }

    [Test]
    public async Task Fail_sends_request_with_correct_values()
    {
        var id = Guid.NewGuid();

        _httpClient.Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });

        await _sut.Fail(id, new FailExternalTaskDto
        {
            WorkerId = "worker",
            ErrorDetails = "details",
            ErrorMessage = "message"
        }, CancellationToken.None);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        _httpClient.Verify(m => m.SendAsync(It.Is<HttpRequestMessage>(r =>
            r.Method == HttpMethod.Post &&
            r.RequestUri.ToString().Contains($"/{id}/")
        ), CancellationToken.None), Times.Once());
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.BadGateway)]
    public void Fail_throws_when_http_status_code_is_not_success(HttpStatusCode statusCode)
    {
        _httpClient.Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent("") });

        var dto = new FailExternalTaskDto
        {
            WorkerId = "worker",
            ErrorMessage = "message",
            ErrorDetails = "details"
        };

        var exception = Assert.ThrowsAsync<HttpRequestException>(() =>
            _sut.Fail(Guid.NewGuid(), dto, CancellationToken.None)) ?? new HttpRequestException();

#if NET5_0_OR_GREATER
        Assert.That(exception.StatusCode, Is.EqualTo(statusCode));
#endif
    }

    [Test]
    public async Task BpmnError_sends_request_with_correct_values()
    {
        var id = Guid.NewGuid();

        _httpClient.Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });

        await _sut.BpmnError(id, new BpmnErrorExternalTaskDto
        {
            WorkerId = "worker",
            ErrorCode = "code",
            ErrorMessage = "message"
        }, CancellationToken.None);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        _httpClient.Verify(m => m.SendAsync(It.Is<HttpRequestMessage>(r =>
            r.Method == HttpMethod.Post &&
            r.RequestUri.ToString().Contains($"/{id}/")
        ), CancellationToken.None), Times.Once());
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.BadGateway)]
    public void BpmnError_throws_when_http_status_code_is_not_success(HttpStatusCode statusCode)
    {
        _httpClient.Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent("") });

        var dto = new BpmnErrorExternalTaskDto
        {
            WorkerId = "worker",
            ErrorCode = "code",
            ErrorMessage = "message"
        };

        var exception = Assert.ThrowsAsync<HttpRequestException>(() =>
            _sut.BpmnError(Guid.NewGuid(), dto, CancellationToken.None)) ?? new HttpRequestException();

#if NET5_0_OR_GREATER
        Assert.That(exception.StatusCode, Is.EqualTo(statusCode));
#endif
    }
}
