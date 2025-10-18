using System.Text;

namespace W3CValidator.Markup;

/// <summary>
///   <para>A set of extension methods for the <see cref="IMarkupValidationRequest"/> interface.</para>
/// </summary>
/// <seealso cref="IMarkupValidationRequest"/>
public static class IMarkupValidationRequestExtensions
{
  /// <summary>
  ///   <para>Specifies the character encoding to use when parsing the document.</para>
  /// </summary>
  /// <param name="request">Validation request instance.</param>
  /// <param name="encoding">Character encoding.</param>
  /// <returns>Back reference to the provided validation <paramref name="request"/>.</returns>
  /// <exception cref="ArgumentNullException">If <paramref name="request"/> is <see langword="null"/>.</exception>
  public static IMarkupValidationRequest Encoding(this IMarkupValidationRequest request, Encoding encoding) => request is not null ? request.Encoding(encoding?.WebName) : throw new ArgumentNullException(nameof(request));
}