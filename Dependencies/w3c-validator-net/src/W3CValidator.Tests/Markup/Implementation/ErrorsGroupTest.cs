using Catharsis.Extensions;
using Catharsis.Fixture;
using W3CValidator.Markup;
using FluentAssertions;
using Xunit;
using FluentAssertions.Execution;

namespace W3CValidator.Tests.Markup;

/// <summary>
///   <para>Tests set for class <see cref="ErrorsGroup"/>.</para>
/// </summary>
public sealed class ErrorsGroupTest : Test
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
    typeof(ErrorsGroup).Should().BeDerivedFrom<object>().And.Implement<IErrorsGroup>();

    using (new AssertionScope())
    {
      var group = new ErrorsGroup();
      group.Count.Should().BeNull();
      group.Errors.Should().BeEmpty();
    }

    using (new AssertionScope())
    {
      var error = new Issue();

      var group = new ErrorsGroup(int.MaxValue, new List<IIssue> { error });
      group.Count.Should().Be(int.MaxValue);
      group.Errors.Should().Equal(error);
    }

    using (new AssertionScope())
    {
      var error = new Issue();

      var group = new ErrorsGroup(int.MaxValue, [error]);
      group.Count.Should().Be(int.MaxValue);
      group.Errors.Should().Equal(error);
    }
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="ErrorsGroup.Count"/> property.</para>
  /// </summary>
  [Fact]
  public void Count_Property() => new ErrorsGroup { Count = int.MaxValue }.Count.Should().Be(int.MaxValue);

  /// <summary>
  ///   <para>Performs testing of <see cref="ErrorsGroup.ErrorsCollection"/> property.</para>
  /// </summary>
  [Fact]
  public void ErrorsCollection_Property()
  {
    var errors = new ErrorsCollection();

    var group = new ErrorsGroup { ErrorsCollection = errors };
    group.ErrorsCollection.Should().BeSameAs(errors);
  }

  /// <summary>
  ///   <para>Performs testing of serialization/deserialization process.</para>
  /// </summary>
  [Fact]
  public void Serialization()
  {
    using (new AssertionScope())
    {
      Test(new ErrorsGroup());
      Test(Fixture<IErrorsGroup>.Create());
    }

    return;

    static void Test(IErrorsGroup instance) => instance.To<object>().Should().BeDataContractSerializable().And.BeXmlSerializable();
  }
}