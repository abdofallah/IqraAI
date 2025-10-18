using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace W3CValidator.Tests;

/// <summary>
///   <para>Tests set for class <see cref="ValidationException"/>.</para>
/// </summary>
public sealed class ValidationExceptionTest : Test
{
  /// <summary>
  ///   <para>Performs testing of class constructor(s).</para>
  ///   <seealso cref="ValidationException(string, Exception)"/>
  /// </summary>
  [Fact]
  public void Constructors()
  {
    typeof(ValidationException).Should().BeDerivedFrom<Exception>();

    using (new AssertionScope())
    {
      var exception = new ValidationException();
      exception.InnerException.Should().BeNull();
      exception.Message.Should().NotBeEmpty();
    }

    using (new AssertionScope())
    {
      var inner = new Exception();
      var exception = new ValidationException("message", inner);
      exception.InnerException.Should().BeSameAs(inner);
      exception.Message.Should().Be("message");
    }
  }
}