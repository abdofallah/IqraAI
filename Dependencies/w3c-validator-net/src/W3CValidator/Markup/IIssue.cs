namespace W3CValidator.Markup;

/// <summary>
///   <para>Represents an issue (error/warning) of document's markup validation.</para>
/// </summary>
public interface IIssue
{
  /// <summary>
  ///   <para>The number/identifier of the issue, as addressed internally by the validator.</para>
  /// </summary>
  string MessageId { get; }

  /// <summary>
  ///   <para>The actual issue message.</para>
  /// </summary>
  string Message { get; }

  /// <summary>
  ///   <para>Within the source code of the validated document, refers to the line where the issue was detected.</para>
  /// </summary>
  int? Line { get; }

  /// <summary>
  ///   <para>Within the source code of the validated document, refers to the column of the line where the issue was detected.</para>
  /// </summary>
  int? Column { get; }

  /// <summary>
  ///   <para>Snippet of the source where the issue was found. Given as HTML fragment within CDATA block.</para>
  /// </summary>
  string Source { get; }

  /// <summary>
  ///   <para>Explanation for the issue. Given as HTML fragment within CDATA block.</para>
  /// </summary>
  string Explanation { get; }
}