using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using Samples.Models;

namespace Tests.IntegrationTests.Handlers;

public class GetRidiculedHandler : ExternalTaskHandler
{
    private const int _retries = 5;
    private readonly IWebHostEnvironment _environment;

    public GetRidiculedHandler(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public override string Topic => "ridiculed";
    public override int? Retries => _retries;
    public override Func<int, TimeSpan> RetryTimeout => retries => TimeSpan.FromMinutes(Math.Pow(2, _retries - retries + 1));

    public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
    {
        await Task.CompletedTask;

        var random = new Random();
        var value = random.Next(0, 10);

        if (value > 5) throw new Exception("Random exception for demonstrating retry policy");

        var fileInfo = new FileInfo(Path.Combine(_environment.ContentRootPath, "Files", "data.xml"));
        new FileExtensionContentTypeProvider().TryGetContentType(fileInfo.Name, out var contentType);

        return new ExternalTaskCompleteResult(new Dictionary<string, Variable>
        {
            { "contact", Variable.From(new Random().Next(0, 2) == 0) },
            { "boolean", Variable.From(true) },
            { "bytes", Variable.From(Encoding.UTF8.GetBytes("bytes")) },
            { "short", Variable.From(short.MaxValue) },
            { "integer", Variable.From(int.MaxValue) },
            { "long", Variable.From(long.MaxValue) },
            { "double", Variable.From(1d) },
            { "date", Variable.From(DateTime.Now) },
            { "string", Variable.From("Text") },
            { "null", Variable.Null() },
            { "file", Variable.From(
                    value: File.ReadAllBytes(fileInfo.FullName),
                    fileName: fileInfo.Name,
                    mimeType: contentType
            ) },
            { "object", Variable.From(new DataDto { Property = "object" }) },
            { "array", Variable.From(new DataDto[]
                {
                    new DataDto { Property = "object 1" },
                    new DataDto { Property = "object 2" },
                    new DataDto { Property = "object 3" },
                }
            ) },
            { "xml", Variable.From(new XDocument(new XElement("root", new XElement("star", new XElement("gate"))))) }
        });
    }
}
