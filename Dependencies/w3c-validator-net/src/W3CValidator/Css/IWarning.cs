namespace W3CValidator.Css;

/// <summary>
///   <para>Represents a warning of CSS document's validation.</para>
/// </summary>
public interface IWarning
{
  /// <summary>
  ///   <para>The actual warning message.</para>
  /// </summary>
  string Message { get; }

  /// <summary>
  ///   <para>The level of the warning's severity.</para>
  /// </summary>
  int? Level { get; }

  /// <summary>
  ///   <para>Within the source code of the validated document, refers to the line where the warning was detected.</para>
  /// </summary>
  int? Line { get; }

  /// <summary>
  ///   <para>Context of warning (surrounding text).</para>
  /// </summary>
  string Context { get; }
}