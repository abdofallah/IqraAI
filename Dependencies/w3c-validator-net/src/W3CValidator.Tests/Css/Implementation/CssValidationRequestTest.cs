using W3CValidator.Css;
using FluentAssertions;
using Xunit;
using FluentAssertions.Execution;
using Catharsis.Extensions;
using Catharsis.Fixture;

namespace W3CValidator.Tests.Css;

/// <summary>
///   <para>Tests set for class <see cref="CssValidationRequest"/>.</para>
/// </summary>
public sealed class CssValidationRequestTest : Test
{
  /// <summary>
  ///   <para>Performs testing of class constructor(s).</para>
  /// </summary>
  /// <seealso cref="CssValidationRequest()"/>
  [Fact]
  public void Constructors()
  {
    typeof(CssValidationRequest).Should().BeDerivedFrom<ValidationRequest>().And.Implement<ICssValidationRequest>();

    using (new AssertionScope())
    {
      var request = new CssValidationRequest();

      request.Parameters.Should().BeOfType<Dictionary<string, object>>().And.BeEmpty();
    }
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="CssValidationRequest.Language(string)"/> method.</para>
  /// </summary>
  [Fact]
  public void Language_Method()
  {
    using (new AssertionScope())
    {
      Test(null, Fixture<ICssValidationRequest>.Create());
      Test("en", Fixture<ICssValidationRequest>.Create());
    }

    return;

    static void Test(string language, ICssValidationRequest request) => request.Language(language).Should().BeSameAs(request).And.BeOfType<CssValidationRequest>().Which.Parameters["lang"].Should().Be(language);
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="CssValidationRequest.Medium(string)"/> method.</para>
  /// </summary>
  [Fact]
  public void Medium_Method()
  {
    using (new AssertionScope())
    {
      Test(null, Fixture<ICssValidationRequest>.Create());
      Test("screen", Fixture<ICssValidationRequest>.Create());
    }

    return;

    static void Test(string medium, ICssValidationRequest request) => request.Medium(medium).Should().BeSameAs(request).And.BeOfType<CssValidationRequest>().Which.Parameters["user-medium"].Should().Be(medium);
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="CssValidationRequest.Profile(string)"/> method.</para>
  /// </summary>
  [Fact]
  public void Profile_Method()
  {
    using (new AssertionScope())
    {
      Test(null, Fixture<ICssValidationRequest>.Create());
      Test("personal", Fixture<ICssValidationRequest>.Create());
    }

    return;

    static void Test(string profile, ICssValidationRequest request) => request.Profile(profile).Should().BeSameAs(request).And.BeOfType<CssValidationRequest>().Which.Parameters["profile"].Should().Be(profile);
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="CssValidationRequest.Warnings(int?)"/> method.</para>
  /// </summary>
  [Fact]
  public void Warnings_Method()
  {
    using (new AssertionScope())
    {
      Test(null, Fixture<ICssValidationRequest>.Create());
      Enum.GetValues<WarningsLevel>().ForEach(level => Test(level, Fixture<ICssValidationRequest>.Create()));
    }

    return;

    static void Test(WarningsLevel? level, ICssValidationRequest request) => request.Warnings(level).Should().BeSameAs(request).And.BeOfType<CssValidationRequest>().Which.Parameters["warning"].Should().Be((int?) level);
  }
}