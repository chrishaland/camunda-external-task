using System.Xml.Linq;
using Newtonsoft.Json;

namespace Haland.CamundaExternalTask;

public abstract partial record Variable
{
    public static Variable Null() => new NullVariable();
    public static Variable From(bool? value) => value == null ? new NullVariable() : new BooleanVariable(value.Value);
    public static Variable From(byte[]? value) => value == null ? new NullVariable() : new BytesVariable(value);
    public static Variable From(short? value) => value == null ? new NullVariable() : new ShortVariable(value.Value);
    public static Variable From(int? value) => value == null ? new NullVariable() : new IntegerVariable(value.Value);
    public static Variable From(long? value) => value == null ? new NullVariable() : new LongVariable(value.Value);
    public static Variable From(double? value) => value == null ? new NullVariable() : new DoubleVariable(value.Value);
    public static Variable From(DateTime? value) => value == null ? new NullVariable() : new DateVariable(value.Value);
    public static Variable From(string? value) => value == null ? new NullVariable() : new StringVariable(value);
    public static Variable From(byte[]? value, string fileName, string? mimeType, string? encoding = "UTF-8") =>
        value == null ? new NullVariable() : new FileVariable(value, new ValueInfo(null, null, fileName, mimeType, encoding));
    public static Variable From(object? value) => value == null ? new NullVariable() : new StringVariable(JsonConvert.SerializeObject(value));
    public static Variable From(object[]? value) => value == null ? new NullVariable() : new StringVariable(JsonConvert.SerializeObject(value));
    public static Variable From(XDocument? value) => value == null ? new NullVariable() : new StringVariable(value.ToString(SaveOptions.DisableFormatting));

    internal static Variable From(VariableDto dto)
    {
        if (dto == null) return new NullVariable();

        var valueInfo = new ValueInfo(
            ObjectTypeName: dto.ValueInfo?.ObjectTypeName,
            SerializationDataFormat: dto.ValueInfo?.SerializationDataFormat,
            FileName: dto.ValueInfo?.FileName,
            MimeType: dto.ValueInfo?.MimeType,
            Encoding: dto.ValueInfo?.Encoding
        );

        return dto.Type switch
        {
            ValueTypes.Null => Null(),
            ValueTypes.File => Null() with { ValueInfo = valueInfo },
            ValueTypes.Boolean => From((bool?)dto.Value) with { ValueInfo = valueInfo },
            ValueTypes.Bytes => From((byte[]?)dto.Value) with { ValueInfo = valueInfo },
            ValueTypes.Short => From((short?)dto.Value) with { ValueInfo = valueInfo },
            ValueTypes.Integer => From((int?)dto.Value) with { ValueInfo = valueInfo },
            ValueTypes.Long => From((long?)dto.Value) with { ValueInfo = valueInfo },
            ValueTypes.Double => From((double?)dto.Value) with { ValueInfo = valueInfo },
            ValueTypes.Date => From((DateTime?)dto.Value) with { ValueInfo = valueInfo },
            ValueTypes.String => From((string?)dto.Value) with { ValueInfo = valueInfo },
            _ => new UnsupportedVariable(dto.Value, dto.Type, valueInfo)
        };
    }
}

internal static class VariableExtentions
{
    internal static IDictionary<string, VariableDto> ToDto(this IDictionary<string, Variable> variables) => variables.ToDictionary((kv) => kv.Key, kv =>
        new VariableDto(
            Value: kv.Value.Token,
            Type: kv.Value.Type,
            ValueInfo: new ValueInfoDto
            {
                Encoding = kv.Value.ValueInfo?.Encoding,
                FileName = kv.Value.ValueInfo?.FileName,
                MimeType = kv.Value.ValueInfo?.MimeType,
                ObjectTypeName = kv.Value.ValueInfo?.ObjectTypeName,
                SerializationDataFormat = kv.Value.ValueInfo?.SerializationDataFormat
            }
        ));
}
