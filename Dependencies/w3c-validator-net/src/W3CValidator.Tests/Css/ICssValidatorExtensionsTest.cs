using Catharsis.Extensions;
using W3CValidator.Css;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace W3CValidator.Tests.Css;

/// <summary>
///   <para>Tests set for class <see cref="ICssValidatorExtensions"/>.</para>
/// </summary>
public sealed class ICssValidatorExtensionsTest : Test
{
  /// <summary>
  ///   <para>Performs testing of <see cref="ICssValidatorExtensions.Request(ICssValidator, Action{ICssValidationRequest})"/> method.</para>
  /// </summary>
  [Fact]
  public void Request_Method()
  {
    using (new AssertionScope())
    {
      AssertionExtensions.Should(() => ICssValidatorExtensions.Request(null)).ThrowExactly<ArgumentNullException>().WithParameterName("validator");

      Test(_ => {}, new CssValidator());
    }

    return;

    static void Test(Action<ICssValidationRequest> request, ICssValidator validator)
    {
      using var executor = validator.Request(request);

      executor.Should().BeOfType<CssRequestExecutor>().And.Subject.GetPropertyValue<ICssValidationRequest>("Request").Should().NotBeNull();
    }
  }
}