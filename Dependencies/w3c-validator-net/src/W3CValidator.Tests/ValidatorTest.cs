using FluentAssertions;
using W3CValidator.Css;
using W3CValidator.Markup;
using Xunit;

namespace W3CValidator.Tests;

/// <summary>
///   <para>Tests set for class <see cref="Validator"/>.</para>
/// </summary>
public sealed class ValidatorTest : Test
{
  /// <summary>
  ///   <para>Performs testing of <see cref="Validator.For"/> property.</para>
  /// </summary>
  [Fact]
  public void For_Property()
  {
    Validator.For.Should().NotBeNull().And.BeSameAs(Validator.For);
    Validator.For.Css.Should().NotBeNull().And.BeSameAs(Validator.For.Css).And.BeOfType<CssValidator>();
    Validator.For.Markup.Should().NotBeNull().And.BeSameAs(Validator.For.Markup).And.BeOfType<MarkupValidator>();
  }
}