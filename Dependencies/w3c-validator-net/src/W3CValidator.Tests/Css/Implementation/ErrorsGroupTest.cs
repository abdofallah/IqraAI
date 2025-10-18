using Catharsis.Extensions;
using Catharsis.Fixture;
using W3CValidator.Css;
using FluentAssertions;
using Xunit;
using FluentAssertions.Execution;

namespace W3CValidator.Tests.Css;

/// <summary>
///   <para>Tests set for class <see cref="ErrorsGroup"/>.</para>
/// </summary>
public sealed class ErrorsGroupTest : Test
{
  /// <summary>
  ///   <para>Performs testing of class constructor(s).</para>
  /// </summary>
  /// <seealso cref="ErrorsGroup()"/>
  /// <seealso cref="ErrorsGroup(string, IEnumerable{IError})"/>
  /// <seealso cref="ErrorsGroup(string, IError[])"/>
  [Fact]
  public void Constructors()
  {
    typeof(ErrorsGroup).Should().BeDerivedFrom<object>().And.Implement<IErrorsGroup>();

    using (new AssertionScope())
    {
      var group = new ErrorsGroup();
      group.Uri.Should().BeNull();
      group.Errors.Should().BeEmpty();
    }

    using (new AssertionScope())
    {
      var error = new Error();

      var group = new ErrorsGroup("uri", new List<IError> { error });
      group.Uri.Should().Be("uri");
      group.Errors.Should().Equal(error);
    }

    using (new AssertionScope())
    {
      var error = new Error();

      var group = new ErrorsGroup("uri", error);
      group.Uri.Should().Be("uri");
      group.Errors.Should().Equal(error);
    }
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="ErrorsGroup.Uri"/> property.</para>
  /// </summary>
  [Fact]
  public void Uri_Property() => new ErrorsGroup { Uri = "uri" }.Uri.Should().Be("uri");

  /// <summary>
  ///   <para>Performs testing of <see cref="ErrorsGroup.ErrorsList"/> property.</para>
  /// </summary>
  [Fact]
  public void ErrorsList_Property() => Array.Empty<IError>().With(errors => new ErrorsGroup { ErrorsList = errors }.Errors.Should().BeSameAs(errors));

  /// <summary>
  ///   <para>Performs testing of <see cref="ErrorsGroup.Errors"/> property.</para>
  /// </summary>
  [Fact]
  public void Errors_Property() => new ErrorsGroup().With(group => group.Errors.Should().BeSameAs(group.ErrorsList));

  /// <summary>
  ///   <para>Performs testing of <see cref="ErrorsGroup.ToString()"/> method.</para>
  /// </summary>
  [Fact]
  public void ToString_Method()
  {
    using (new AssertionScope())
    {
      Test(string.Empty, new ErrorsGroup());
      Test(string.Empty, new ErrorsGroup { Uri = string.Empty });
      Test("uri", new ErrorsGroup { Uri = "uri" });
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
      Test(new ErrorsGroup());
      Test(Fixture<IErrorsGroup>.Create());
    }

    return;

    static void Test(IErrorsGroup instance) => instance.To<object>().Should().BeDataContractSerializable().And.BeXmlSerializable();
  }
}