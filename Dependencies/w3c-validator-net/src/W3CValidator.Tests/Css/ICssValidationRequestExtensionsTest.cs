using System.Globalization;
using W3CValidator.Css;
using FluentAssertions;
using Xunit;
using Catharsis.Extensions;
using FluentAssertions.Execution;

namespace W3CValidator.Tests.Css;

/// <summary>
///   <para>Tests set for class <see cref="ICssValidationRequestExtensions"/>.</para>
/// </summary>
public sealed class ICssValidationRequestExtensionsTest : Test
{
  /// <summary>
  ///   <para>Performs testing of <see cref="ICssValidationRequestExtensions.Language(ICssValidationRequest, CultureInfo)"/> method.</para>
  /// </summary>
  [Fact]
  public void Language_Method()
  {
    using (new AssertionScope())
    {
      AssertionExtensions.Should(() => ICssValidationRequestExtensions.Language(null, null)).ThrowExactly<ArgumentNullException>().WithParameterName("request");

      Test(null, new CssValidationRequest());
      CultureInfo.GetCultures(CultureTypes.AllCultures).ForEach(culture => Test(culture, new CssValidationRequest()));
    }

    return;

    static void Test(CultureInfo culture, ICssValidationRequest request) => request.Language(culture).Should().BeSameAs(request).And.BeOfType<CssValidationRequest>().Which.Parameters["lang"].Should().Be(culture?.TwoLetterISOLanguageName);
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="ICssValidationRequestExtensions.Medium(ICssValidationRequest, CssMedium?)"/> method.</para>
  /// </summary>
  [Fact]
  public void Medium_Method()
  {
    using (new AssertionScope())
    {
      AssertionExtensions.Should(() => ICssValidationRequestExtensions.Medium(null, CssMedium.All)).ThrowExactly<ArgumentNullException>().WithParameterName("request");

      Test(null, new CssValidationRequest());
      Enum.GetValues<CssMedium>().ForEach(medium => Test(medium, new CssValidationRequest()));
    }

    return;
    
    static void Test(CssMedium? medium, ICssValidationRequest request) => request.Medium(medium).Should().BeSameAs(request).And.BeOfType<CssValidationRequest>().Which.Parameters["user-medium"].Should().Be(medium?.ToInvariantString());
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="ICssValidationRequestExtensions.Profile(ICssValidationRequest, CssProfile?)"/> method.</para>
  /// </summary>
  [Fact]
  public void Profile_Method()
  {
    using (new AssertionScope())
    {
      AssertionExtensions.Should(() => ICssValidationRequestExtensions.Profile(null, CssProfile.Css1)).ThrowExactly<ArgumentNullException>().WithParameterName("request");

      Test(null, new CssValidationRequest());
      Enum.GetValues<CssProfile>().ForEach(profile => Test(profile, new CssValidationRequest()));
    }

    return;

    static void Test(CssProfile? profile, ICssValidationRequest request) => request.Profile(profile).Should().BeSameAs(request).And.BeOfType<CssValidationRequest>().Which.Parameters["profile"].Should().Be(profile?.ToString().ToLowerInvariant());
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="ICssValidationRequestExtensions.Warnings(ICssValidationRequest, WarningsLevel?)"/> method.</para>
  /// </summary>
  [Fact]
  public void Warnings_Method()
  {
    using (new AssertionScope())
    {
      AssertionExtensions.Should(() => ICssValidationRequestExtensions.Warnings(null, WarningsLevel.All)).ThrowExactly<ArgumentNullException>().WithParameterName("request");

      Test(null, new CssValidationRequest());
      Enum.GetValues<WarningsLevel>().ForEach(level => Test(level, new CssValidationRequest()));
    }

    return;

    static void Test(WarningsLevel? level, ICssValidationRequest request) => request.Warnings(level).Should().BeSameAs(request).And.BeOfType<CssValidationRequest>().Which.Parameters["warning"].Should().Be((int?) level);
  }
}