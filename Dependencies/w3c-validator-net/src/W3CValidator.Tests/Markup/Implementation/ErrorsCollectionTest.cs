using Catharsis.Extensions;
using Catharsis.Fixture;
using W3CValidator.Markup;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace W3CValidator.Tests.Markup;

/// <summary>
///   <para>Tests set for class <see cref="ErrorsCollection"/>.</para>
/// </summary>
public sealed class ErrorsCollectionTest : Test
{
  /// <summary>
  ///   <para>Performs testing of class constructor(s).</para>
  /// </summary>
  /// <seealso cref="ErrorsCollection()"/>
  /// <seealso cref="ErrorsCollection(IEnumerable{IIssue})"/>
  [Fact]
  public void Constructors()
  {
    typeof(ErrorsCollection).Should().BeDerivedFrom<List<IIssue>>();

    using (new AssertionScope())
    {
      var collection = new ErrorsCollection();
      collection.Should().BeEmpty();
    }

    using (new AssertionScope())
    {
      var collection = new ErrorsCollection([]);
      collection.Should().BeEmpty();

      var error = new Issue();
      collection = new ErrorsCollection([error]);
      collection.Should().Equal(error);
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
      Test(new ErrorsCollection());
      Test(Fixture<ErrorsCollection>.Create());
    }

    return;

    static void Test(IList<IIssue> instance) => instance.To<object>().Should().BeDataContractSerializable().And.BeXmlSerializable();
  }
}