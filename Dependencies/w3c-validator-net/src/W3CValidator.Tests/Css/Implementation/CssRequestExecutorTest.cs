using W3CValidator.Css;
using FluentAssertions;
using Xunit;
using System.Reflection;
using Catharsis.Extensions;
using Catharsis.Fixture;
using FluentAssertions.Execution;

namespace W3CValidator.Tests.Css;

/// <summary>
///   <para>Tests set for class <see cref="CssRequestExecutor"/>.</para>
/// </summary>
public sealed class CssRequestExecutorTest : Test
{
  /// <summary>
  ///   <para>Performs testing of <see cref="CssRequestExecutor.DocumentAsync(string, CancellationToken)"/> method.</para>
  /// </summary>
  [Fact]
  public void DocumentAsync_Method()
  {
    using (new AssertionScope())
    {
      AssertionExtensions.Should(() => Validator.For.Css.Request().DocumentAsync(null)).ThrowExactlyAsync<ArgumentNullException>().WithParameterName("document").Await();
      AssertionExtensions.Should(() => Validator.For.Css.Request().DocumentAsync(string.Empty, Fixture<CancellationToken>.Create())).ThrowExactlyAsync<OperationCanceledException>().Await();

      var validator = Validator.For.Css;
      var stylesheet = "text";

      using (var executor = validator.Request())
      {
        var result = executor.DocumentAsync(stylesheet).Await();
        result.Should().NotBeNull();
        result.Valid.Should().BeFalse();
        result.Uri.Should().Be("file://localhost/TextArea");
        result.CheckedBy.Should().Be("http://jigsaw.w3.org/css-validator/");
        result.CssLevel.Should().Be("css3");
        result.Date.Should().BeAfter(DateTimeOffset.MinValue);
        var errorsGroup = result.Issues.ErrorsGroups.Single();
        errorsGroup.Uri.Should().Be("file://localhost/TextArea");
        var error = errorsGroup.Errors.Single();
        error.Line.Should().Be(1);
        error.Type.Should().Be("parse-error");
        error.Context.Should().Be("text");
        error.Subtype.Should().Be("unrecognized");
        error.SkippedString.Should().Be("[empty string]");
        error.Message.Should().Be("Parse Error");
        result.Issues.Warnings.Should().BeEmpty();
      }

      stylesheet = Assembly.GetExecutingAssembly().GetManifestResourceStream("W3CValidator.Css.Stylesheet.css").ToTextAsync().Await();
      using (var executor = validator.Request(request => request.Profile(CssProfile.Css2).Language("ru").Warnings(WarningsLevel.Important)))
      {
        var result = executor.DocumentAsync(stylesheet).Await();
        result.Should().NotBeNull();
        result.Uri.Should().Be("file://localhost/TextArea");
        result.CheckedBy.Should().Be("http://jigsaw.w3.org/css-validator/");
        result.CssLevel.Should().Be("css2");
        result.Date.Should().BeAfter(DateTimeOffset.MinValue);
        result.Valid.Should().BeFalse();
        var errorsGroup = result.Issues.ErrorsGroups.Single();
        errorsGroup.Uri.Should().Be("file://localhost/TextArea");
        errorsGroup.Errors.Should().HaveCount(10);
        var error = errorsGroup.Errors.ElementAt(6);
        error.Line.Should().Be(184);
        error.Type.Should().Be("parse-error");
        error.Context.Should().Be(".button");
        error.Subtype.Should().Be("skippedString");
        error.SkippedString.Should().Be(")");
        error.Message.Should().Be("rgba(0,0,0,0.5) не может быть использовано с данной версией CSS css2");
        var warningsGroup = result.Issues.WarningsGroups.Single();
        warningsGroup.Uri.Should().Be("file://localhost/TextArea");
        warningsGroup.Warnings.Should().HaveCount(9);
        var warning = warningsGroup.Warnings.First();
        warning.Line.Should().Be(38);
        warning.Level.Should().Be(0);
        warning.Message.Should().Be("Свойство -moz-inline-stack - неизвестное расширение поставщика");
        warning.Context.Should().BeNull();
      }

      return;

      static void Test()
      {

      }
    }
  }

  /// <summary>
  ///   <para>Performs testing of <see cref="CssRequestExecutor.UrlAsync(Uri, CancellationToken)"/> method.</para>
  /// </summary>
  [Fact]
  public void UrlAsync_Method()
  {
    using (new AssertionScope())
    {
      AssertionExtensions.Should(() => Validator.For.Css.Request().UrlAsync(null)).ThrowExactlyAsync<ArgumentNullException>().WithParameterName("url").Await();
      AssertionExtensions.Should(() => Validator.For.Css.Request().UrlAsync(Fixture<Uri>.Create(), Fixture<CancellationToken>.Create())).ThrowExactlyAsync<OperationCanceledException>().Await();

      Test(new CssValidationResult
      {
        CheckedBy = "http://jigsaw.w3.org/css-validator/",
        CssLevel = "css3",
        Valid = true,
        Issues = new Issues { WarningsGroupsList = [new WarningsGroup()] }
      }, "http://www.w3.org/2008/site/css/minimum".ToUri(), Validator.For.Css.Request());
    }

    return;

    static void Test(ICssValidationResult result, Uri url, ICssRequestExecutor executor)
    {
      using (executor)
      {
        var task = executor.UrlAsync(url);
        task.Should().BeOfType<Task<ICssValidationResult>>();

        var validation = task.Await();
        validation.Should().BeOfType<CssValidationResult>();
        validation.Valid.Should().Be(result.Valid);
        validation.Uri.Should().Be(url.ToString());
        validation.CheckedBy.Should().Be(result.CheckedBy);
        validation.CssLevel.Should().Be(result.CssLevel);
        validation.Date.Should().BeAfter(DateTimeOffset.MinValue);
        validation.Valid.Should().Be(result.Valid);
        validation.Issues.Errors.Should().Equal(result.Issues.Errors);
        validation.Issues.ErrorsGroups.Should().Equal(result.Issues.ErrorsGroups);
        validation.Issues.Warnings.Should().Equal(result.Issues.Warnings);
        validation.Issues.WarningsGroups.Should().Equal(result.Issues.WarningsGroups);
      }
    }
  }
}