using W3CValidator.Markup;

namespace W3CValidator.Css;

/// <summary>
///   <para></para>
/// </summary>
public interface IMarkupRequestExecutor : IDisposable
{
  /// <summary>
  ///   <para>Validates markup of a document which is specified by its URL address, using W3C markup validation web service.</para>
  /// </summary>
  /// <param name="url">URL address of document to be validated.</param>
  /// <param name="cancellation"></param>
  /// <returns>Markup validation result instance.</returns>
  Task<IMarkupValidationResult> UrlAsync(Uri url, CancellationToken cancellation = default);
}