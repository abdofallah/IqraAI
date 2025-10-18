using System.Runtime.Serialization;

namespace W3CValidator.Markup;

/// <summary>
///   <para>Logical group of validation warnings.</para>
/// </summary>
[DataContract(Name = "warnings")]
public sealed class WarningsGroup : IWarningsGroup
{
  /// <summary>
  ///   <para>URI address of validated document or fragment.</para>
  /// </summary>
  [DataMember(Name = "warningcount", IsRequired = true)]
  public int? Count { get; set; }

  /// <summary>
  ///   <para>Collection of validation errors.</para>
  /// </summary>
  [DataMember(Name = "warninglist", IsRequired = true)]
  public WarningsCollection WarningsCollection { get; init; } = [];

  /// <summary>
  ///   <para></para>
  /// </summary>
  public IEnumerable<IIssue> Warnings => WarningsCollection;

  /// <summary>
  ///   <para></para>
  /// </summary>
  public WarningsGroup()
  {
  }

  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="count"></param>
  /// <param name="warnings"></param>
  public WarningsGroup(int? count, IEnumerable<IIssue> warnings)
  {
    Count = count;
    WarningsCollection = [.. warnings];
  }

  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="count"></param>
  /// <param name="warnings"></param>
  public WarningsGroup(int? count, params IIssue[] warnings) : this(count, warnings as IEnumerable<IIssue>)
  {
  }
}