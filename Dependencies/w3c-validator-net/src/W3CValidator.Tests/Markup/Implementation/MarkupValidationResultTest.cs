using W3CValidator.Markup;
using FluentAssertions;
using Xunit;
using FluentAssertions.Execution;
using System.Runtime.Serialization;
using Catharsis.Extensions;
using Catharsis.Fixture;

namespace W3CValidator.Tests.Markup;

/// <summary>
///   <para>Tests set for class <see cref="MarkupValidationResult"/>.</para>
/// </summary>
public sealed class MarkupValidationResultTest : Test
{
  /// <summary>
  ///   <para>Performs testing of class constructor(s).</para>
  /// </summary>
  /// <seealso cref="MarkupValidationResult()"/>
  [Fact]
  public void Constructors()
  {
    typeof(MarkupValidationResult).Should().BeDerivedFrom<object>().And.Implement<IComparable<IMarkupValidationResult>>().And.Implement<IEquatable<IMarkupValidationResult>>().And.Implement<IMarkupValidationResult>().And.BeDecoratedWith<DataContractAttribute>();

    using (new AssertionScope())
    {
      var result = new MarkupValidationResult();

      result.Uri.Should().BeNull();
      result.Valid.Should().BeNull();
      result.Date.Should().BeNull();
      result.CheckedBy.Should().BeNull();
      result.Doctype.Should().BeNull();
      result.Encoding.Should().BeNull();
      result.ErrorsGroup.Should().BeNull();
      result.WarningsGroup.Should().BeNull();
    }
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupValidationResult.Uri"/> property.</para>
  /// </summary>
  [Fact]
  public void Uri_Property() => new MarkupValidationResult { Uri = "uri" }.Uri.Should().Be("uri");
  
  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupValidationResult.Valid"/> property.</para>
  /// </summary>
  [Fact]
  public void Valid_Property() => new MarkupValidationResult { Valid = true }.Valid.Should().BeTrue();

  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupValidationResult.Date"/> property.</para>
  /// </summary>
  [Fact]
  public void Date_Property() => new MarkupValidationResult { Date = DateTimeOffset.MaxValue }.Date.Should().Be(DateTimeOffset.MaxValue);

  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupValidationResult.CheckedBy"/> property.</para>
  /// </summary>
  [Fact]
  public void CheckedBy_Property() => new MarkupValidationResult { CheckedBy = "checkedBy" }.CheckedBy.Should().Be("checkedBy");

  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupValidationResult.Doctype"/> property.</para>
  /// </summary>
  [Fact]
  public void Doctype_Property() => new MarkupValidationResult { Doctype = "doctype" }.Doctype.Should().Be("doctype");
  
  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupValidationResult.Encoding"/> property.</para>
  /// </summary>
  [Fact]
  public void Encoding_Property() => new MarkupValidationResult { Encoding = "encoding" }.Encoding.Should().Be("encoding");

  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupValidationResult.ErrorsGroup"/> property.</para>
  /// </summary>
  [Fact]
  public void ErrorsGroup_Property() => Fixture<IErrorsGroup>.Create().With(group => new MarkupValidationResult { ErrorsGroup = group }.ErrorsGroup.Should().BeSameAs(group));

  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupValidationResult.WarningsGroup"/> property.</para>
  /// </summary>
  [Fact]
  public void WarningsGroup_Property() => Fixture<IWarningsGroup>.Create().With(group => new MarkupValidationResult { WarningsGroup = group }.WarningsGroup.Should().BeSameAs(group));

  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupValidationResult.CompareTo(IMarkupValidationResult)"/> method.</para>
  /// </summary>
  [Fact]
  public void CompareTo_Method() => TestCompareTo<MarkupValidationResult, DateTimeOffset>(nameof(MarkupValidationResult.Date), DateTimeOffset.MinValue, DateTimeOffset.MaxValue); 

  /// <summary>
  ///   <para>Performs testing of following methods :</para>
  ///   <list type="bullet">
  ///     <item><description><see cref="MarkupValidationResult.Equals(IMarkupValidationResult)"/></description></item>
  ///     <item><description><see cref="MarkupValidationResult.Equals(object)"/></description></item>
  ///   </list>
  /// </summary>
  [Fact]
  public void Equals_Methods() => TestEquality<MarkupValidationResult, string>(nameof(MarkupValidationResult.Uri), "<", ">"); 

  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupValidationResult.GetHashCode()"/> method.</para>
  /// </summary>
  [Fact]
  public void GetHashCode_Method() => TestHashCode<MarkupValidationResult, string>(nameof(MarkupValidationResult.Uri), "<", ">"); 

  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupValidationResult.ToString()"/> method.</para>
  /// </summary>
  [Fact]
  public void ToString_Method()
  {
    using (new AssertionScope())
    {
      Test(string.Empty, new MarkupValidationResult());
      Test(string.Empty, new MarkupValidationResult { Uri = string.Empty });
      Test("uri", new MarkupValidationResult { Uri = "uri" });
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
      Test(new MarkupValidationResult());
      Test(Fixture<IMarkupValidationResult>.Create());
    }

    return;

    static void Test(IMarkupValidationResult instance) => instance.To<object>().Should().BeDataContractSerializable().And.BeXmlSerializable();
  }
}