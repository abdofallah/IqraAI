using System.Runtime.Serialization;

namespace W3CValidator.Markup;

/// <summary>
///   <para>Logical group of validation errors.</para>
/// </summary>
[DataContract(Name = "errors")]
public sealed class ErrorsGroup : IErrorsGroup
{
  /// <summary>
  ///   <para>URI address of validated document or fragment.</para>
  /// </summary>
  [DataMember(Name = "errorcount", IsRequired = true)]
  public int? Count { get; set; }

  /// <summary>
  ///   <para>Collection of validation errors.</para>
  /// </summary>
  [DataMember(Name = "errorlist", IsRequired = true)]
  public ErrorsCollection ErrorsCollection { get; init; } = [];

  /// <summary>
  ///   <para>Collection of validation errors.</para>
  /// </summary>
  public IEnumerable<IIssue> Errors => ErrorsCollection;

  /// <summary>
  ///   <para></para>
  /// </summary>
  public ErrorsGroup()
  {
  }

  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="count"></param>
  /// <param name="errors"></param>
  public ErrorsGroup(int? count, IEnumerable<IIssue> errors)
  {
    Count = count;
    ErrorsCollection = new ErrorsCollection(errors);
  }

  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="count"></param>
  /// <param name="errors"></param>
  public ErrorsGroup(int? count, params IIssue[] errors) : this(count, errors as IEnumerable<IIssue>)
  {
  }
}