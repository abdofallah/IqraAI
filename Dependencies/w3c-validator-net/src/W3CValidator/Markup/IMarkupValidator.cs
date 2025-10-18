using W3CValidator.Css;

namespace W3CValidator.Markup;

/// <summary>
///   <para>Represents a markup validator for hypertext documents or code fragments that makes use of W3C validation web service.</para>
/// </summary>
/// <seealso cref="http://validator.w3.org/docs/api.html"/>
public interface IMarkupValidator
{
  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="request"></param>
  /// <returns></returns>
  IMarkupRequestExecutor Request(IMarkupValidationRequest request = null);
}