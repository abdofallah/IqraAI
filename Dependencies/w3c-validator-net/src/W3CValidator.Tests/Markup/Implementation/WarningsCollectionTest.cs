using W3CValidator.Markup;
using FluentAssertions;
using Xunit;
using Catharsis.Extensions;
using Catharsis.Fixture;
using FluentAssertions.Execution;

namespace W3CValidator.Tests.Markup;

/// <summary>
///   <para>Tests set for class <see cref="WarningsCollection"/>.</para>
/// </summary>
public sealed class WarningsCollectionTest : Test
{
  /// <summary>
  ///   <para>Performs testing of class constructor(s).</para>
  /// </summary>
  /// <seealso cref="WarningsCollection()"/>
  /// <seealso cref="WarningsCollection(IEnumerable{IIssue})"/>
  [Fact]
  public void Constructors()
  {
    typeof(WarningsCollection).Should().BeDerivedFrom<List<IIssue>>();

    using (new AssertionScope())
    {
      var collection = new WarningsCollection();
      collection.Should().BeEmpty();
    }

    using (new AssertionScope())
    {
      var collection = new WarningsCollection([]);
      collection.Should().BeEmpty();

      var warning = new Issue();
      collection = new WarningsCollection([warning]);
      collection.Should().Equal(warning);
    }
  }

  /// <summary>
  ///   <para>Performs testing of serialization/deserialization process.</para>
  /// </summary>
  [Fact]
  public void Serialization()
  {
    using (new AssertionScope())
    {
      Test(new WarningsCollection());
      Test(Fixture<WarningsCollection>.Create());
    }

    return;

    static void Test(IList<IIssue> instance) => instance.To<object>().Should().BeDataContractSerializable().And.BeXmlSerializable();
  }
}