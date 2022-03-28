using System.Text;
using Microsoft.AspNetCore.StaticFiles;

namespace Tests.IntegrationTests.Handlers;

public class GetRidiculedHandler : ExternalTaskHandler
{
    private readonly IWebHostEnvironment _environment;

    public GetRidiculedHandler(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public override string Topic => "ridiculed";

    public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
    {
        await Task.CompletedTask;

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
            { "object", Variable.From(new { a = 1, b= 2 }) },
            { "array", Variable.From(new object[]
                { 
                    new { a = 1, b= 2 },
                    new { a = 3, b= 4 },
                    new { a = 5, b= 6 },
                }
            ) },
            { "xml", Variable.From(new XDocument(new XElement("root", new XElement("star", new XElement("gate"))))) }
        });
    }
}
