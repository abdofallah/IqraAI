using Catharsis.Extensions;
using Catharsis.Fixture;
using W3CValidator.Css;
using FluentAssertions;
using Xunit;
using FluentAssertions.Execution;

namespace W3CValidator.Tests.Css;

/// <summary>
///   <para>Tests set for class <see cref="WarningsGroup"/>.</para>
/// </summary>
public sealed class WarningsGroupTest : Test
{
  /// <summary>
  ///   <para>Performs testing of class constructor(s).</para>
  /// </summary>
  /// <seealso cref="WarningsGroup()"/>
  /// <seealso cref="WarningsGroup(string, IEnumerable{IWarning})"/>
  /// <seealso cref="WarningsGroup(string, IWarning[])"/>
  [Fact]
  public void Constructors()
  {
    typeof(WarningsGroup).Should().BeDerivedFrom<object>().And.Implement<IWarningsGroup>();

    using (new AssertionScope())
    {
      var group = new WarningsGroup();
      group.Uri.Should().BeNull();
      group.Warnings.Should().BeEmpty();
    }

    using (new AssertionScope())
    {
      var warning = new Warning();

      var group = new WarningsGroup("uri", new List<IWarning> { warning });
      group.Uri.Should().Be("uri");
      group.Warnings.Should().Equal(warning);
    }

    using (new AssertionScope())
    {
      var warning = new Warning();

      var group = new WarningsGroup("uri", [warning]);
      group.Uri.Should().Be("uri");
      group.Warnings.Should().Equal(warning);
    }
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="WarningsGroup.Uri"/> property.</para>
  /// </summary>
  [Fact]
  public void Uri_Property() => new WarningsGroup { Uri = "uri" }.Uri.Should().Be("uri");

  /// <summary>
  ///   <para>Performs testing of <see cref="WarningsGroup.WarningsList"/> property.</para>
  /// </summary>
  [Fact]
  public void WarningsList_Property() => Array.Empty<IWarning>().With(warnings => new WarningsGroup { WarningsList = warnings }.Warnings.Should().BeSameAs(warnings));

  /// <summary>
  ///   <para>Performs testing of <see cref="WarningsGroup.Warnings"/> property.</para>
  /// </summary>
  [Fact]
  public void Warnings_Property() => new WarningsGroup().With(group => group.Warnings.Should().BeSameAs(group.WarningsList));

  /// <summary>
  ///   <para>Performs testing of <see cref="WarningsGroup.ToString()"/> method.</para>
  /// </summary>
  [Fact]
  public void ToString_Method()
  {
    using (new AssertionScope())
    {
      Test(string.Empty, new WarningsGroup());
      Test(string.Empty, new WarningsGroup { Uri = string.Empty });
      Test("uri", new WarningsGroup { Uri = "uri" });
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
      Test(new WarningsGroup());
      Test(Fixture<IWarningsGroup>.Create());
    }

    return;

    static void Test(IWarningsGroup instance) => instance.To<object>().Should().BeDataContractSerializable().And.BeXmlSerializable();
  }
}