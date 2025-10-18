using W3CValidator.Markup;
using FluentAssertions;
using Xunit;
using Catharsis.Extensions;
using Catharsis.Fixture;
using FluentAssertions.Execution;

namespace W3CValidator.Tests.Markup;

/// <summary>
///   <para>Tests set for class <see cref="WarningsGroup"/>.</para>
/// </summary>
public sealed class WarningsGroupTest : Test
{
  /// <summary>
  ///   <para>Performs testing of class constructor(s).</para>
  /// </summary>
  /// <seealso cref="WarningsGroup()"/>
  /// <seealso cref="WarningsGroup(int?, IEnumerable{IIssue})"/>
  /// <seealso cref="WarningsGroup(int?, IIssue[])"/>
  [Fact]
  public void Constructors()
  {
    typeof(WarningsGroup).Should().BeDerivedFrom<object>().And.Implement<IWarningsGroup>();

    using (new AssertionScope())
    {
      var group = new WarningsGroup();
      group.Count.Should().BeNull();
      group.Warnings.Should().BeEmpty();
    }

    using (new AssertionScope())
    {
      var warning = new Issue();

      var group = new WarningsGroup(int.MaxValue, new List<IIssue> { warning });
      group.Count.Should().Be(int.MaxValue);
      group.Warnings.Should().Equal(warning);
    }

    using (new AssertionScope())
    {
      var warning = new Issue();

      var group = new WarningsGroup(int.MaxValue, warning);
      group.Count.Should().Be(int.MaxValue);
      group.Warnings.Should().Equal(warning);
    }
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="WarningsGroup.Count"/> property.</para>
  /// </summary>
  [Fact]
  public void Count_Property() => new WarningsGroup { Count = int.MaxValue }.Count.Should().Be(int.MaxValue);

  /// <summary>
  ///   <para>Performs testing of <see cref="WarningsGroup.WarningsCollection"/> property.</para>
  /// </summary>
  [Fact]
  public void WarningsCollection_Property()
  {
    var warnings = new WarningsCollection();

    var group = new WarningsGroup { WarningsCollection = warnings };
    group.WarningsCollection.Should().BeSameAs(warnings);
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