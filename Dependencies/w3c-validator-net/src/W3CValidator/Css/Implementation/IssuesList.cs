using System.Runtime.Serialization;
using Catharsis.Commons;

namespace W3CValidator.Css;

/// <summary>
///   <para>List of errors and warnings encountered during the validation process.</para>
/// </summary>
public sealed class IssuesList : IIssuesList
{
  /// <summary>
  ///   <para>Collection of validation errors groups.</para>
  /// </summary>
  public IEnumerable<IErrorsList> ErrorsGroups { get; }

  /// <summary>
  ///   <para>Collection of validation errors groups.</para>
  /// </summary>
  public IEnumerable<IWarningsList> WarningsGroups { get; }

  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="info"></param>
  public IssuesList(Info info)
  {
    ErrorsGroups = info.ErrorsGroups ?? new List<ErrorsList.Info>();
    WarningsGroups = info.WarningsGroups ?? new List<WarningsList.Info>();
  }

  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="info"></param>
  public IssuesList(object info) : this(new Info().Properties(info)) {}

  /// <summary>
  ///   <para></para>
  /// </summary>
  [DataContract(Name = "result")]
  public sealed record Info : IResultable<IIssuesList>
  {
    /// <summary>
    ///   <para>Collection of validation errors groups.</para>
    /// </summary>
    public List<ErrorsList.Info>? ErrorsGroups { get; init; }

    /// <summary>
    ///   <para>Collection of validation errors groups.</para>
    /// </summary>
    public List<WarningsList.Info>? WarningsGroups { get; init; }

    /// <summary>
    ///   <para></para>
    /// </summary>
    /// <returns></returns>
    public IIssuesList Result() => new IssuesList(this);
  }
}