using W3CValidator.Css;
using FluentAssertions;
using Xunit;
using Catharsis.Extensions;
using Catharsis.Fixture;
using FluentAssertions.Execution;
using W3CValidator.Markup;
using ErrorsGroup = W3CValidator.Markup.ErrorsGroup;
using WarningsGroup = W3CValidator.Markup.WarningsGroup;

namespace W3CValidator.Tests.Markup;

/// <summary>
///   <para>Tests set for class <see cref="MarkupRequestExecutor"/>.</para>
/// </summary>
public sealed class MarkupRequestExecutorTest : Test
{
  /// <summary>
  ///   <para>Performs testing of <see cref="MarkupRequestExecutor.UrlAsync(Uri, CancellationToken)"/> method.</para>
  /// </summary>
  [Fact]
  public void UrlAsync_Method()
  {
    using (new AssertionScope())
    {
      AssertionExtensions.Should(() => Validator.For.Markup.Request().UrlAsync(null)).ThrowExactlyAsync<ArgumentNullException>().WithParameterName("url").Await();
      AssertionExtensions.Should(() => Validator.For.Markup.Request().UrlAsync(Fixture<Uri>.Create(), Fixture<CancellationToken>.Create())).ThrowExactlyAsync<TaskCanceledException>().Await();

      Test(new MarkupValidationResult
      {
        Valid = true,
        Uri = "http://www.w3.org/",
        CheckedBy = "http://validator.w3.org/",
        Doctype = "-//W3C//DTD XHTML 1.0 Strict//EN",
        Encoding = "utf-8",
        ErrorsGroup = new ErrorsGroup(0),
        WarningsGroup = new WarningsGroup(0)
      }, "http://www.w3.org/".ToUri(), Validator.For.Markup.Request());
    }

    return;

    static void Test(IMarkupValidationResult result, Uri url, IMarkupRequestExecutor executor)
    {
      using (executor)
      {
        var task = executor.UrlAsync(url);
        task.Should().BeAssignableTo<Task<IMarkupValidationResult>>();

        var validation = task.Await();
        validation.Should().BeOfType<MarkupValidationResult>();
        validation.Valid.Should().Be(result.Valid);
        validation.Uri.Should().Be(result.Uri);
        validation.CheckedBy.Should().Be(result.CheckedBy);
        validation.Date.Should().BeAfter(DateTimeOffset.MinValue);
        validation.Doctype.Should().Be(result.Doctype);
        validation.Encoding.Should().Be(result.Encoding);
        validation.ErrorsGroup.Count.Should().Be(result.ErrorsGroup.Count);
        validation.ErrorsGroup.Errors.Should().Equal(result.ErrorsGroup.Errors);
        validation.WarningsGroup.Count.Should().Be(result.WarningsGroup.Count);
        validation.WarningsGroup.Warnings.Should().Equal(result.WarningsGroup.Warnings);
      }
    }
  }
}