using W3CValidator.Markup;

namespace W3CValidator.Css;

/// <summary>
///   <para>A set of extension methods for the <see cref="IMarkupRequestExecutor"/> interface.</para>
/// </summary>
/// <seealso cref="IMarkupRequestExecutor"/>
public static class IMarkupRequestExecutorExtensions
{
  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="executor"></param>
  /// <param name="url"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentNullException">If either <paramref name="executor"/> or <paramref name="url"/> is <see langword="null"/>.</exception>
  public static IMarkupValidationResult Url(this IMarkupRequestExecutor executor, Uri url)
  {
    if (executor is null) throw new ArgumentNullException(nameof(executor));
    if (url is null) throw new ArgumentNullException(nameof(url));

    return executor.UrlAsync(url).Result;
  }
}