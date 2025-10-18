namespace W3CValidator.Css;

/// <summary>
///   <para>Logical group of validation warnings.</para>
/// </summary>
public interface IWarningsGroup
{
  /// <summary>
  ///   <para>URI address of validated document or fragment.</para>
  /// </summary>
  string Uri { get; }

  /// <summary>
  ///   <para>Collection of validation warnings.</para>
  /// </summary>
  IEnumerable<IWarning> Warnings { get; }
}