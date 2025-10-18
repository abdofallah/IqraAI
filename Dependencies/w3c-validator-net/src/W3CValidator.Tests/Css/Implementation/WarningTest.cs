using W3CValidator.Css;
using FluentAssertions;
using Xunit;
using Catharsis.Extensions;
using Catharsis.Fixture;
using FluentAssertions.Execution;

namespace W3CValidator.Tests.Css;

/// <summary>
///   <para>Tests set for class <see cref="Warning"/>.</para>
/// </summary>
public sealed class WarningTest : Test
{
  /// <summary>
  ///   <para>Performs testing of class constructor(s).</para>
  /// </summary>
  /// <seealso cref="Warning()"/>
  [Fact]
  public void Constructors()
  {
    typeof(Warning).Should().BeDerivedFrom<object>().And.Implement<IWarning>();

    using (new AssertionScope())
    {
      var warning = new Warning();
      warning.Message.Should().BeNull();
      warning.Level.Should().BeNull();
      warning.Line.Should().BeNull();
      warning.Context.Should().BeNull();
    }
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="Warning.Message"/> property.</para>
  /// </summary>
  [Fact]
  public void Message_Property() => new Warning { Message = "message" }.Message.Should().Be("message");

  /// <summary>
  ///   <para>Performs testing of <see cref="Warning.Level"/> property.</para>
  /// </summary>
  [Fact]
  public void Level_Property() => new Warning { Level = int.MaxValue }.Level.Should().Be(int.MaxValue);

  /// <summary>
  ///   <para>Performs testing of <see cref="Warning.Line"/> property.</para>
  /// </summary>
  [Fact]
  public void Line_Property() => new Warning { Line = int.MaxValue }.Line.Should().Be(int.MaxValue);

  /// <summary>
  ///   <para>Performs testing of <see cref="Warning.Context"/> property.</para>
  /// </summary>
  [Fact]
  public void Context_Property() => new Warning { Context = "context" }.Context.Should().Be("context");

  /// <summary>
  ///   <para>Performs testing of <see cref="Warning.ToString()"/> method.</para>
  /// </summary>
  [Fact]
  public void ToString_Method()
  {
    using (new AssertionScope())
    {
      Test("1:2 message", new Warning { Level = 2, Line = 1, Message = "message" });
    }

    return;

    static void Test(string value, object instance) => instance.ToString().Should().Be(value);
  }

  /// <summary>
  ///   <para>Performs testing of XML serialization/deserialization process.</para>
  /// </summary>
  [Fact]
  public void Serialization()
  {
    using (new AssertionScope())
    {
      Test(new Warning());
      Test(Fixture<IWarning>.Create());
    }

    return;

    static void Test(IWarning instance) => instance.To<object>().Should().BeDataContractSerializable().And.BeXmlSerializable();
  }
}