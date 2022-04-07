using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Tests;

[TestFixture]
public class VariableTests
{
    [Test]
    public void Should_create_a_null_variable()
    {
        var variable = Variable.Null();
        Assert.That(variable.String, Is.Null);
    }

    [Test]
    public void Should_create_a_boolean_variable()
    {
        var variable = Variable.From(true);
        Assert.That(variable.Boolean, Is.True);
    }

    [Test]
    public void Should_create_a_null_variable_if_boolean_is_null()
    {
        var variable = Variable.From((bool?)null);
        Assert.That(variable.Boolean, Is.Null);
    }

    [Test]
    public void Should_create_a_byte_array_variable()
    {
        var bytes = Encoding.UTF8.GetBytes("bytes");
        var variable = Variable.From(bytes);
        CollectionAssert.AreEqual(bytes, variable.Bytes);
    }

    [Test]
    public void Should_create_a_null_variable_if_byte_array_is_null()
    {
        var variable = Variable.From((byte[]?)null);
        Assert.That(variable.Boolean, Is.Null);
    }

    [Test]
    public void Should_create_a_short_variable()
    {
        var variable = Variable.From(short.MaxValue);
        Assert.That(variable.Short, Is.EqualTo(short.MaxValue));
    }

    [Test]
    public void Should_create_a_null_variable_if_short_is_null()
    {
        var variable = Variable.From((short?)null);
        Assert.That(variable.Boolean, Is.Null);
    }

    [Test]
    public void Should_create_a_integer_variable()
    {
        var variable = Variable.From(int.MaxValue);
        Assert.That(variable.Integer, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void Should_create_a_null_variable_if_integer_is_null()
    {
        var variable = Variable.From((int?)null);
        Assert.That(variable.Boolean, Is.Null);
    }

    [Test]
    public void Should_create_a_long_variable()
    {
        var variable = Variable.From(long.MaxValue);
        Assert.That(variable.Long, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void Should_create_a_null_variable_if_long_is_null()
    {
        var variable = Variable.From((long?)null);
        Assert.That(variable.Boolean, Is.Null);
    }

    [Test]
    public void Should_create_a_double_variable()
    {
        var variable = Variable.From(double.MaxValue);
        Assert.That(variable.Double, Is.EqualTo(double.MaxValue));
    }

    [Test]
    public void Should_create_a_null_variable_if_double_is_null()
    {
        var variable = Variable.From((double?)null);
        Assert.That(variable.Boolean, Is.Null);
    }

    [Test]
    public void Should_create_a_date_variable()
    {
        var variable = Variable.From(DateTime.Today);
        Assert.That(variable.Date, Is.EqualTo(DateTime.Today));
    }

    [Test]
    public void Should_create_a_null_variable_if_date_is_null()
    {
        var variable = Variable.From((DateTime?)null);
        Assert.That(variable.Boolean, Is.Null);
    }

    [Test]
    public void Should_create_a_string_variable()
    {
        var text = "text";
        var variable = Variable.From(text);
        Assert.That(variable.String, Is.EqualTo(text));
    }

    [Test]
    public void Should_create_a_null_variable_if_string_is_null()
    {
        var variable = Variable.From((string?)null);
        Assert.That(variable.Boolean, Is.Null);
    }

    [Test]
    public void Should_create_a_object_variable()
    {
        var variable = Variable.From(new { a = 1 });
        Assert.That(variable.String, Is.EqualTo(@"{""a"":1}"));
    }

    [Test]
    public void Should_create_a_null_variable_if_object_is_null()
    {
        var variable = Variable.From((object?)null);
        Assert.That(variable.Boolean, Is.Null);
    }

    [Test]
    public void Should_create_a_object_array_variable()
    {
        var variable = Variable.From(new[] { new { a = 1 }, new { a = 2 } });
        Assert.That(variable.String, Is.EqualTo(@"[{""a"":1},{""a"":2}]"));
    }

    [Test]
    public void Should_create_a_null_variable_if_object_array_is_null()
    {
        var variable = Variable.From((object[]?)null);
        Assert.That(variable.Boolean, Is.Null);
    }

    [Test]
    public void Should_create_a_xml_variable()
    {
        var variable = Variable.From(new XDocument(new XElement("root")));
        Assert.That(variable.String, Is.EqualTo("<root />"));
    }

    [Test]
    public void Should_create_a_null_variable_if_xml_is_null()
    {
        var variable = Variable.From((XDocument?)null);
        Assert.That(variable.Boolean, Is.Null);
    }

    [Test]
    public void Should_create_a_file_variable()
    {
        var cwd = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        var file = File.ReadAllBytes(Path.Combine(cwd, "TestHost", "data.xml"));
        var variable = Variable.From(file, "data.xml", "application/xml");
        Assert.Multiple(() =>
        {
            Assert.That(variable.Bytes, Is.EqualTo(file));
            Assert.That(variable.File.FileName, Is.EqualTo("data.xml"));
            Assert.That(variable.ValueInfo.FileName, Is.EqualTo("data.xml"));
            Assert.That(variable.File.MimeType, Is.EqualTo("application/xml"));
            Assert.That(variable.ValueInfo.MimeType, Is.EqualTo("application/xml"));
        });
    }

    [Test]
    public void Should_create_a_null_variable_if_file_is_null()
    {
        var variable = Variable.From(null, "data.xml", "application/xml");
        Assert.That(variable.Boolean, Is.Null);
    }
}
