using W3CValidator.Markup;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace W3CValidator.Tests.Markup;

/// <summary>
///   <para>Tests set for class <see cref="MarkupException"/>.</para>
/// </summary>
public sealed class MarkupExceptionTest : Test
{
  /// <summary>
  ///   <para>Performs testing of class constructor(s).</para>
  ///   <seealso cref="MarkupException(string, Exception)"/>
  /// </summary>
  [Fact]
  public void Constructors()
  {
    typeof(MarkupException).Should().BeDerivedFrom<ValidationException>();

    using (new AssertionScope())
    {
      var exception = new MarkupException();
      exception.InnerException.Should().BeNull();
      exception.Message.Should().NotBeEmpty();
    }

    using (new AssertionScope())
    {
      var inner = new Exception();
      var exception = new MarkupException("message", inner);
      exception.InnerException.Should().BeSameAs(inner);
      exception.Message.Should().Be("message");
    }
  }
}