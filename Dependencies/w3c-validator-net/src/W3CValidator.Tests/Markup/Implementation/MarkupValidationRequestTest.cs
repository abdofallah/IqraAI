using System.Text;
using Catharsis.Extensions;
using Catharsis.Fixture;
using W3CValidator.Markup;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace W3CValidator.Tests.Markup;

/// <summary>
///   <para>Tests set for class <see cref="MarkupValidationRequest"/>.</para>
/// </summary>
public sealed class MarkupValidationRequestTest : Test
{
  /// <summary>
  ///   <para>Performs testing of class constructor(s).</para>
  /// </summary>
  /// <seealso cref="MarkupValidationRequest()"/>
  [Fact]
  public void Constructors()
  {
    typeof(MarkupValidationRequest).Should().BeDerivedFrom<ValidationRequest>().And.Implement<IMarkupValidationRequest>();

    using (new AssertionScope())
    {
      var request = new MarkupValidationRequest();

      request.Parameters.Should().BeOfType<Dictionary<string, object>>().And.BeEmpty();
    }
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupValidationRequest.Doctype(string)"/> method.</para>
  /// </summary>
  [Fact]
  public void Doctype_Method()
  {
    using (new AssertionScope())
    {
      Test(null, Fixture<IMarkupValidationRequest>.Create());
      Test("html", Fixture<IMarkupValidationRequest>.Create());
    }

    return;

    static void Test(string doctype, IMarkupValidationRequest request) => request.Doctype(doctype).Should().BeSameAs(request).And.BeOfType<MarkupValidationRequest>().Which.Parameters["doctype"].Should().Be(doctype);
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupValidationRequest.Encoding(string)"/> method.</para>
  /// </summary>
  [Fact]
  public void Encoding_Method()
  {
    using (new AssertionScope())
    {
      Test(null, Fixture<IMarkupValidationRequest>.Create());
      Encoding.GetEncodings().ForEach(encoding => Test(encoding.Name, Fixture<IMarkupValidationRequest>.Create()));
    }

    return;

    static void Test(string encoding, IMarkupValidationRequest request) => request.Encoding(encoding).Should().BeSameAs(request).And.BeOfType<MarkupValidationRequest>().Which.Parameters["charset"].Should().Be(encoding);
  }
}