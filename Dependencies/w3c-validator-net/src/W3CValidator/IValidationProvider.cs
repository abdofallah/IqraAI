using W3CValidator.Css;
using W3CValidator.Markup;

namespace W3CValidator;

/// <summary>
///   <para>Representation of access point for different types of W3C content validators.</para>
/// </summary>
public interface IValidationProvider
{
  /// <summary>
  ///   <para>Returns instance of CSS validator to perform validation of CSS documents through W3C web service.</para>
  /// </summary>
  /// <returns>CSS validator.</returns>
  ICssValidator Css { get; }

  /// <summary>
  ///   <para>Returns instance of markup validator to perform validation of hypertext documents through W3C web service.</para>
  /// </summary>
  /// <returns>Markup validator.</returns>
  IMarkupValidator Markup { get; }
}