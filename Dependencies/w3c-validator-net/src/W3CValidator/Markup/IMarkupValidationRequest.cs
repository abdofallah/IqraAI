namespace W3CValidator.Markup;

/// <summary>
///   <para>Represents configurable request to W3C markup validation web service.</para>
/// </summary>
public interface IMarkupValidationRequest : IValidationRequest
{
  /// <summary>
  ///   <para>Specifies the Document Type (DOCTYPE) to use when parsing the document.</para>
  /// </summary>
  /// <param name="doctype">Markup document's DOCTYPE value.</param>
  /// <returns>Back reference to the current validation request.</returns>
  IMarkupValidationRequest Doctype(string doctype);

  /// <summary>
  ///   <para>Specifies the character encoding to use when parsing the document.</para>
  /// </summary>
  /// <param name="encoding">Character encoding.</param>
  /// <returns>Back reference to the current validation request.</returns>
  IMarkupValidationRequest Encoding(string encoding);
}