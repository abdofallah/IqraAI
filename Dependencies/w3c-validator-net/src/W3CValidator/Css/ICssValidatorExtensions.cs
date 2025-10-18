namespace W3CValidator.Css;

/// <summary>
///   <para>A set of extension methods for the <see cref="ICssValidator"/> interface.</para>
/// </summary>
/// <seealso cref="ICssValidator"/>
public static class ICssValidatorExtensions
{
  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="validator"></param>
  /// <param name="action"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentNullException">If <paramref name="validator"/> is <see langword="null"/>.</exception>
  public static ICssRequestExecutor Request(this ICssValidator validator, Action<ICssValidationRequest> action = null)
  {
    if (validator is null) throw new ArgumentNullException(nameof(validator));

    var request = new CssValidationRequest();

    action?.Invoke(request);

    return validator.Request(request);
  }
}