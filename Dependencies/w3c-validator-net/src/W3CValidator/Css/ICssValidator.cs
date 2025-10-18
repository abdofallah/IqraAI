namespace W3CValidator.Css;

/// <summary>
///   <para>Represents a validator for CSS documents or code fragments that makes use of W3C validation web service.</para>
/// </summary>
/// <seealso cref="http://jigsaw.w3.org/css-validator/api.html"/>
public interface ICssValidator
{
  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="request"></param>
  /// <returns></returns>
  ICssRequestExecutor Request(ICssValidationRequest request = null);
}