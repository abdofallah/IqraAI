namespace W3CValidator.Css;

/// <summary>
///   <para>List of issues (errors/warnings) encountered during the validation process.</para>
/// </summary>
public interface IIssues
{
  /// <summary>
  ///   <para>Collection of validation errors groups.</para>
  /// </summary>
  IEnumerable<IErrorsGroup> ErrorsGroups { get; }

  /// <summary>
  ///   <para>Collection of validation warnings groups.</para>
  /// </summary>
  IEnumerable<IWarningsGroup> WarningsGroups { get; }

  /// <summary>
  ///   <para></para>
  /// </summary>
  IEnumerable<IError> Errors => ErrorsGroups.SelectMany(group => group.Errors);

  /// <summary>
  ///   <para></para>
  /// </summary>
  IEnumerable<IWarning> Warnings => WarningsGroups.SelectMany(group => group.Warnings);
}