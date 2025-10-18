using System.Text;
using W3CValidator.Markup;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Catharsis.Extensions;

namespace W3CValidator.Tests.Markup;

/// <summary>
///   <para>Tests set for class <see cref="IMarkupValidationRequestExtensions"/>.</para>
/// </summary>
public sealed class IMarkupValidationRequestExtensionsTest : Test
{
  /// <summary>
  ///   <para>Performs testing of <see cref="IMarkupValidationRequestExtensions.Encoding(IMarkupValidationRequest, Encoding)"/> method.</para>
  /// </summary>
  [Fact]
  public void Encoding_Method()
  {
    using (new AssertionScope())
    {
      AssertionExtensions.Should(() => IMarkupValidationRequestExtensions.Encoding(null, Encoding.Default)).ThrowExactly<ArgumentNullException>().WithParameterName("request");

      Test(null, new MarkupValidationRequest());
      Encoding.GetEncodings().ForEach(encoding => Test(encoding.GetEncoding(), new MarkupValidationRequest()));
    }

    return;

    static void Test(Encoding encoding, IMarkupValidationRequest request) => request.Encoding(encoding).Should().BeSameAs(request).And.BeOfType<MarkupValidationRequest>().Which.Parameters["charset"].Should().Be(encoding?.WebName);
  }
}