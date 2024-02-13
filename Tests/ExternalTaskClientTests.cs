using Newtonsoft.Json.Linq;

namespace Tests;

[TestFixture]
public class ExternalTaskClientTests
{
    private static readonly HttpClient _httpClient = Substitute.For<HttpClient>();
    private readonly ExternalTaskClient _sut = new(_httpClient);

    [Test]
    public async Task FetchAndLock_returns_list_of_locked_external_tasks()
    {
        var id = Guid.NewGuid();

        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK) 
                    { 
                        Content = new StringContent(@$"
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
                    ")
                    }
                )
            );

        var response = await _sut.FetchAndLock(new FetchExternalTasksDto 
        {
        }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.Length, Is.EqualTo(1));
            Assert.That(response[0].Id, Is.EqualTo(id));
            Assert.That(response[0].TopicName, Is.EqualTo("topic"));
            Assert.That(response[0].WorkerId, Is.EqualTo("worker"));
            Assert.That(response[0].Variables["var"].Value?.ToString(), Is.EqualTo("<root/>"));
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
        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new HttpResponseMessage(statusCode) { Content = new StringContent("") }
                )
            );

        var exception = Assert.ThrowsAsync<HttpRequestException>(() => 
            _sut.FetchAndLock(new FetchExternalTasksDto(), CancellationToken.None)) ?? new HttpRequestException();

        Assert.That(exception.StatusCode, Is.EqualTo(statusCode));
    }

    [Test]
    public async Task Complete_sends_request_with_correct_values()
    {
        var id = Guid.NewGuid();

        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") }
                )
            );

        await _sut.Complete(id, new CompleteExternalTaskDto
        {
            WorkerId = "worker",
            Variables = new Dictionary<string, VariableDto>()
        }, CancellationToken.None);

        _ = _httpClient.Received(1).SendAsync(Arg.Is<HttpRequestMessage>(r =>
            r.Method == HttpMethod.Post &&
            r.RequestUri != null &&
            r.RequestUri.ToString().Contains($"/{id}/")
        ), CancellationToken.None);
    }

    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.BadGateway)]
    public void Complete_throws_when_http_status_code_is_not_success(HttpStatusCode statusCode)
    {
        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new HttpResponseMessage(statusCode) { Content = new StringContent("") }
                )
            );

        var dto = new CompleteExternalTaskDto
        {
            WorkerId = "worker",
            Variables = new Dictionary<string, VariableDto>() 
            {
                { "var", new VariableDto(
                    Value: new JValue("value"),
                    Type: "String",
                    ValueInfo: new ValueInfoDto 
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

        Assert.That(exception.StatusCode, Is.EqualTo(statusCode));
    }

    [Test]
    public async Task Fail_sends_request_with_correct_values()
    {
        var id = Guid.NewGuid();

        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") }
                )
            );

        await _sut.Fail(id, new FailExternalTaskDto
        {
            WorkerId = "worker",
            ErrorDetails = "details",
            ErrorMessage = "message"
        }, CancellationToken.None);

        _ = _httpClient.Received(1).SendAsync(Arg.Is<HttpRequestMessage>(r =>
            r.Method == HttpMethod.Post &&
            r.RequestUri != null &&
            r.RequestUri.ToString().Contains($"/{id}/")
        ), CancellationToken.None);
    }

    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.BadGateway)]
    public void Fail_throws_when_http_status_code_is_not_success(HttpStatusCode statusCode)
    {
        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new HttpResponseMessage(statusCode) { Content = new StringContent("") }
                )
            );

        var dto = new FailExternalTaskDto
        {
            WorkerId = "worker",
            ErrorMessage = "message",
            ErrorDetails = "details"
        };

        var exception = Assert.ThrowsAsync<HttpRequestException>(() =>
            _sut.Fail(Guid.NewGuid(), dto, CancellationToken.None)) ?? new HttpRequestException();

        Assert.That(exception.StatusCode, Is.EqualTo(statusCode));
    }

    [Test]
    public async Task BpmnError_sends_request_with_correct_values()
    {
        var id = Guid.NewGuid();

        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") }
                )
            );

        await _sut.BpmnError(id, new BpmnErrorExternalTaskDto
        {
            WorkerId = "worker",
            ErrorCode = "code",
            ErrorMessage = "message"
        }, CancellationToken.None);

        _ = _httpClient.Received(1).SendAsync(Arg.Is<HttpRequestMessage>(r =>
            r.Method == HttpMethod.Post &&
            r.RequestUri != null &&
            r.RequestUri.ToString().Contains($"/{id}/")
        ), CancellationToken.None);
    }

    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.BadGateway)]
    public void BpmnError_throws_when_http_status_code_is_not_success(HttpStatusCode statusCode)
    {
        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new HttpResponseMessage(statusCode) { Content = new StringContent("") }
                )
            );

        var dto = new BpmnErrorExternalTaskDto
        {
            WorkerId = "worker",
            ErrorCode = "code",
            ErrorMessage = "message"
        };

        var exception = Assert.ThrowsAsync<HttpRequestException>(() =>
            _sut.BpmnError(Guid.NewGuid(), dto, CancellationToken.None)) ?? new HttpRequestException();

        Assert.That(exception.StatusCode, Is.EqualTo(statusCode));
    }

    [TestCase(42, "Integer")]
    [TestCase(42d, "Double")]
    [TestCase(true, "Boolean")]
    [TestCase("text", "String")]
    public void Post_should_be_able_to_serialize_primitive_values(object value, string type)
    {
        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") }
                )
            );

        Assert.DoesNotThrowAsync(() =>
            _sut.Post(
                requestUri: "/", 
                dto: new CompleteExternalTaskDto 
                {
                    WorkerId = "worker",
                    Variables = new Dictionary<string, VariableDto>
                    {
                        { "variable", new VariableDto(
                            Value: new JValue(value), 
                            Type: type, 
                            ValueInfo: new ValueInfoDto()) 
                        }
                    }
                },
                cancellationToken: CancellationToken.None
            ));
    }

    [Test]
    public void Post_should_be_able_to_serialize_array_values()
    {
        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") }
                )
            );

        Assert.DoesNotThrowAsync(() =>
            _sut.Post(
                requestUri: "/",
                dto: new CompleteExternalTaskDto
                {
                    WorkerId = "worker",
                    Variables = new Dictionary<string, VariableDto>
                    {
                        { "variable", new VariableDto(
                            Value: new JArray(new[]{ "1", "2", "3" }), 
                            Type: null, 
                            ValueInfo: new ValueInfoDto())
                        }
                    }
                },
                cancellationToken: CancellationToken.None
            ));
    }

    [Test]
    public void Post_should_be_able_to_serialize_object_values()
    {
        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") }
                )
            );

        Assert.DoesNotThrowAsync(() =>
            _sut.Post(
                requestUri: "/",
                dto: new CompleteExternalTaskDto
                {
                    WorkerId = "worker",
                    Variables = new Dictionary<string, VariableDto>
                    {
                        { "variable", new VariableDto(
                            Value: JToken.FromObject(new { a = 1, b = 2, c = 3 }), 
                            Type: null, 
                            ValueInfo: new ValueInfoDto())
                        }
                    }
                },
                cancellationToken: CancellationToken.None
            ));
    }
}
