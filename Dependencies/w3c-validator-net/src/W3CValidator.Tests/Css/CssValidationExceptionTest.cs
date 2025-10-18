using W3CValidator.Css;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace W3CValidator.Tests.Css;

/// <summary>
///   <para>Tests set for class <see cref="CssException"/>.</para>
/// </summary>
public sealed class CssValidationExceptionTest : Test
{
  /// <summary>
  ///   <para>Performs testing of class constructor(s).</para>
  /// </summary>
  /// <seealso cref="CssException(string, Exception)"/>
  [Fact]
  public void Constructors()
  {
    typeof(CssException).Should().BeDerivedFrom<ValidationException>();

    using (new AssertionScope())
    {
      var exception = new CssException();
      exception.InnerException.Should().BeNull();
      exception.Message.Should().NotBeEmpty();
    }

    using (new AssertionScope())
    {
      var inner = new Exception();
      var exception = new CssException("message", inner);
      exception.InnerException.Should().BeSameAs(inner);
      exception.Message.Should().Be("message");
    }
  }
}