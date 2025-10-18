using W3CValidator.Css;
using W3CValidator.Markup;

namespace W3CValidator;

/// <summary>
///   <para>Entry point to access W3C validation services.</para>
/// </summary>
public static class Validator
{
  /// <summary>
  ///   <para>Returns validation provider instance to perform validating operations.</para>
  /// </summary>
  public static IValidationProvider For { get; } = new ValidationProvider();

  private sealed class ValidationProvider : IValidationProvider
  {
    public ICssValidator Css { get; } = new CssValidator();

    public IMarkupValidator Markup { get; } = new MarkupValidator();
  }
}