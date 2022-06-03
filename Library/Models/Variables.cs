using Newtonsoft.Json;

namespace Haland.CamundaExternalTask;

/// <summary>
/// Additional, value-type-dependent properties.
/// </summary>
/// <param name="ObjectTypeName">A string representation of the object's type name.</param>
/// <param name="SerializationDataFormat">The serialization format used to store the variable (Java, XML, JSON).<br/>See: https://docs.camunda.org/manual/latest/user-guide/data-formats/</param>
/// <param name="FileName">The name of the file. This is not the variable name but the name that will be used when downloading the file again.</param>
/// <param name="MimeType">The MIME type of the file that is being uploaded.</param>
/// <param name="Encoding">The encoding of the file that is being uploaded.</param>
public record ValueInfo(string? ObjectTypeName = null, string? SerializationDataFormat = null, string? FileName = null, string? MimeType = null, string? Encoding = null);

/// <summary>
/// See https://docs.camunda.org/manual/7.8/user-guide/process-engine/variables
/// </summary>
/// <param name="Token">Represents the value of the variable. As it is stored in the http response from Camunda (JSON property value)</param>
/// <param name="Type">The value type of the variable.<br/>See: https://docs.camunda.org/manual/7.8/user-guide/process-engine/variables/#supported-variable-values</param>
/// <param name="ValueInfo">Additional, value-type-dependent properties.</param>
public abstract partial record Variable(JToken? Token, string? Type, ValueInfo ValueInfo = default!)
{
    public bool? Boolean { get { return (bool?)(Token as JValue)?.Value; } }
    public byte[]? Bytes { get { return (byte[]?)(Token as JValue)?.Value; } }
    public short? Short { get { return short.TryParse((Token as JValue)?.Value?.ToString(), out var value) ? value : null; } }
    public int? Integer { get { return int.TryParse((Token as JValue)?.Value?.ToString(), out var value) ? value : null; } }
    public long? Long { get { return long.TryParse((Token as JValue)?.Value?.ToString(), out var value) ? value : null; } }
    public double? Double { get { return double.TryParse((Token as JValue)?.Value?.ToString(), out var value) ? value : null; } }
    public DateTime? Date { get { return (DateTime?)(Token as JValue)?.Value; } }
    public string? String { get { return (string?)(Token as JValue)?.Value; } }
    public (string? FileName, string? MimeType, string? Encoding) File { get { return (ValueInfo.FileName, ValueInfo.MimeType, ValueInfo.Encoding); } }
    public T? As<T>() where T : class
    {
        if (String == null) return null;
        return JsonConvert.DeserializeObject<T>(String);
    }
};

internal sealed class ValueTypes
{
    internal const string Boolean = "Boolean";
    internal const string Bytes = "Bytes";
    internal const string Short = "Short";
    internal const string Integer = "Integer";
    internal const string Long = "Long";
    internal const string Double = "Double";
    internal const string Date = "Date";
    internal const string String = "String";
    internal const string File = "File";
    internal const string Null = "Null";
    internal const string Object = "Object";
}

internal record BooleanVariable(bool Value, ValueInfo ValueInfo = default!) : Variable(new JValue(Value), ValueTypes.Boolean, ValueInfo);
internal record BytesVariable(byte[] Value, ValueInfo ValueInfo = default!) : Variable(new JValue(Value), ValueTypes.Bytes, ValueInfo);
internal record ShortVariable(short Value, ValueInfo ValueInfo = default!) : Variable(new JValue(Value), ValueTypes.Short, ValueInfo);
internal record IntegerVariable(int Value, ValueInfo ValueInfo = default!) : Variable(new JValue(Value), ValueTypes.Integer, ValueInfo);
internal record LongVariable(long Value, ValueInfo ValueInfo = default!) : Variable(new JValue(Value), ValueTypes.Long, ValueInfo);
internal record DoubleVariable(double Value, ValueInfo ValueInfo = default!) : Variable(new JValue(Value), ValueTypes.Double, ValueInfo);
internal record DateVariable(DateTime Value, ValueInfo ValueInfo = default!) : Variable(new JValue(Value), ValueTypes.Date, ValueInfo);
internal record StringVariable(string Value, ValueInfo ValueInfo = default!) : Variable(new JValue(Value), ValueTypes.String, ValueInfo);
internal record ObjectVariable(string Value, ValueInfo ValueInfo = default!) : Variable(new JValue(Value), ValueTypes.Object, ValueInfo);
internal record FileVariable(byte[] Value, ValueInfo ValueInfo) : Variable(Value, ValueTypes.File, ValueInfo);
internal record NullVariable() : Variable(null, ValueTypes.Null);
internal record UnsupportedVariable(JToken? Value, string? Type, ValueInfo ValueInfo) : Variable(Value, Type, ValueInfo);
