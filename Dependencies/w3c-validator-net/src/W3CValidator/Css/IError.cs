namespace W3CValidator.Css;

/// <summary>
///   <para>Represents an error of CSS document's validation.</para>
/// </summary>
public interface IError
{
  /// <summary>
  ///   <para>The actual error message.</para>
  /// </summary>
  string Message { get; }

  /// <summary>
  ///   <para>Base type of error.</para>
  /// </summary>
  string Type { get; }

  /// <summary>
  ///   <para>Subtype of error's type.</para>
  /// </summary>
  string Subtype { get; }

  /// <summary>
  ///   <para>Erroneous property name.</para>
  /// </summary>
  string Property { get; }

  /// <summary>
  ///   <para>Within the source code of the validated document, refers to the line where the error was detected.</para>
  /// </summary>
  int? Line { get; }

  /// <summary>
  ///   <para>Context of error (surrounding text).</para>
  /// </summary>
  string Context { get; }

  /// <summary>
  ///   <para>String that was skipped, if any.</para>
  /// </summary>
  string SkippedString { get; }
}