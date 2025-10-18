using System.Runtime.Serialization;

namespace W3CValidator.Markup;

/// <summary>
///   <para>Represents an issue (error/warning) of document's markup validation.</para>
/// </summary>
public sealed class Issue : IIssue
{
  /// <summary>
  ///   <para>The number/identifier of the issue, as addressed internally by the validator.</para>
  /// </summary>
  [DataMember(Name = "messageid", IsRequired = true)]
  public string MessageId { get; set; }

  /// <summary>
  ///   <para>The actual issue message.</para>
  /// </summary>
  [DataMember(Name = "message", IsRequired = true)]
  public string Message { get; set; }

  /// <summary>
  ///   <para>Within the source code of the validated document, refers to the line where the issue was detected.</para>
  /// </summary>
  [DataMember(Name = "line", IsRequired = true)]
  public int? Line { get; set; }

  /// <summary>
  ///   <para>Within the source code of the validated document, refers to the column of the line where the issue was detected.</para>
  /// </summary>
  [DataMember(Name = "col", IsRequired = true)]
  public int? Column { get; set; }

  /// <summary>
  ///   <para>Snippet of the source where the issue was found. Given as HTML fragment within CDATA block.</para>
  /// </summary>
  [DataMember(Name = "source", IsRequired = true)]
  public string Source { get; set; }

  /// <summary>
  ///   <para>Explanation for the issue. Given as HTML fragment within CDATA block.</para>
  /// </summary>
  [DataMember(Name = "explanation", IsRequired = true)]
  public string Explanation { get; set; }

  /// <summary>
  ///   <para>Returns a <see cref="string"/> that represents the current <see cref="Issue"/> instance.</para>
  /// </summary>
  /// <returns>A string that represents the current <see cref="Issue"/>.</returns>
  public override string ToString() => $"{Line}:{Column} {Message}";
}