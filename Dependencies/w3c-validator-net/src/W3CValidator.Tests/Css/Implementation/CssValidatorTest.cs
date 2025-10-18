using Catharsis.Extensions;
using W3CValidator.Css;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace W3CValidator.Tests.Css;

/// <summary>
///   <para>Tests set for class <see cref="CssValidator"/>.</para>
/// </summary>
public sealed class CssValidatorTest : Test
{
  /// <summary>
  ///   <para>Performs testing of <see cref="CssValidator.Request(ICssValidationRequest)"/> method.</para>
  /// </summary>
  [Fact]
  public void Request_Method()
  {
    using (new AssertionScope())
    {
      var validator = Validator.For.Css;

      Test(validator);
      Test(validator, new CssValidationRequest());
    }

    return;

    static void Test(ICssValidator validator, ICssValidationRequest request = null)
    {
      using var executor = validator.Request(request);

      executor.Should().BeOfType<CssRequestExecutor>().And.Subject.GetPropertyValue<ICssValidationRequest>("Request").Should().BeSameAs(request);
    }
  }
}