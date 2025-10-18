namespace W3CValidator.Css;

/// <summary>
///   <para>Logical group of validation errors.</para>
/// </summary>
public interface IErrorsGroup
{
  /// <summary>
  ///   <para>URI address of validated document or fragment.</para>
  /// </summary>
  string Uri { get; }

  /// <summary>
  ///   <para>Collection of validation errors.</para>
  /// </summary>
  IEnumerable<IError> Errors { get; }
}