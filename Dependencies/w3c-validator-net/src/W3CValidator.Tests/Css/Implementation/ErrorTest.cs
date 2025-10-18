using W3CValidator.Css;
using FluentAssertions;
using Xunit;
using Catharsis.Extensions;
using Catharsis.Fixture;
using FluentAssertions.Execution;

namespace W3CValidator.Tests.Css;

/// <summary>
///   <para>Tests set for class <see cref="Error"/>.</para>
/// </summary>
public sealed class ErrorTest : Test
{
  /// <summary>
  ///   <para>Performs testing of class constructor(s).</para>
  /// </summary>
  /// <seealso cref="Error()"/>
  [Fact]
  public void Constructors()
  {
    typeof(Error).Should().BeDerivedFrom<object>().And.Implement<IError>();

    using (new AssertionScope())
    {
      var error = new Error();

      error.Message.Should().BeNull();
      error.Type.Should().BeNull();
      error.Subtype.Should().BeNull();
      error.Property.Should().BeNull();
      error.Line.Should().BeNull();
      error.Context.Should().BeNull();
      error.SkippedString.Should().BeNull();
    }
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="Error.Message"/> property.</para>
  /// </summary>
  [Fact]
  public void Message_Property() => new Error { Message = "message" }.Message.Should().Be("message");

  /// <summary>
  ///   <para>Performs testing of <see cref="Error.Type"/> property.</para>
  /// </summary>
  [Fact]
  public void Type_Property() => new Error { Type = "type" }.Type.Should().Be("type");

  /// <summary>
  ///   <para>Performs testing of <see cref="Error.Subtype"/> property.</para>
  /// </summary>
  [Fact]
  public void Subtype_Property() => new Error { Subtype = "subtype" }.Subtype.Should().Be("subtype");

  /// <summary>
  ///   <para>Performs testing of <see cref="Error.Property"/> property.</para>
  /// </summary>
  [Fact]
  public void Property_Property() => new Error { Property = "property" }.Property.Should().Be("property");

  /// <summary>
  ///   <para>Performs testing of <see cref="Error.Line"/> property.</para>
  /// </summary>
  [Fact]
  public void Line_Property() => new Error { Line = int.MaxValue }.Line.Should().Be(int.MaxValue);

  /// <summary>
  ///   <para>Performs testing of <see cref="Error.Context"/> property.</para>
  /// </summary>
  [Fact]
  public void Context_Property() => new Error { Context = "context" }.Context.Should().Be("context");

  /// <summary>
  ///   <para>Performs testing of <see cref="Error.SkippedString"/> property.</para>
  /// </summary>
  [Fact]
  public void SkippedString_Property() => new Error { SkippedString = "skippedString" }.SkippedString.Should().Be("skippedString");

  /// <summary>
  ///   <para>Performs testing of <see cref="Error.ToString()"/> method.</para>
  /// </summary>
  [Fact]
  public void ToString_Method()
  {
    using (new AssertionScope())
    {
      Test(string.Empty, new Error());
      Test(string.Empty, new Error { Line = 1 });
      Test("message", new Error { Message = "message" });
      Test("1:message", new Error { Line = 1, Message = "message" });
    }

    return;

    static void Test(string value, object instance) => instance.ToString().Should().Be(value);
  }

  /// <summary>
  ///   <para>Performs testing of serialization/deserialization process.</para>
  /// </summary>
  [Fact]
  public void Serialization()
  {
    using (new AssertionScope())
    {
      Test(new Error());
      Test(Fixture<IError>.Create());
    }

    return;

    static void Test(IError instance) => instance.To<object>().Should().BeDataContractSerializable().And.BeXmlSerializable();
  }
}