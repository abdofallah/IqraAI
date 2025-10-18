namespace W3CValidator.Css;

/// <summary>
///   <para>Represents configurable request to W3C CSS validation web service.</para>
/// </summary>
public interface ICssValidationRequest : IValidationRequest
{
  /// <summary>
  ///   <para>Specifies language to be used for description of validation issues.</para>
  /// </summary>
  /// <param name="language">Text language.</param>
  /// <returns>Back reference to the current validation request.</returns>
  ICssValidationRequest Language(string language);

  /// <summary>
  ///   <para>Specifies CSS medium to use in validation process.</para>
  /// </summary>
  /// <param name="medium">CSS medium name.</param>
  /// <returns>Back reference to the current validation request.</returns>
  ICssValidationRequest Medium(string medium);

  /// <summary>
  ///   <para>Specifies CSS standard version (profile) to be used in validation process.</para>
  /// </summary>
  /// <param name="profile">CSS profile.</param>
  /// <returns>Back reference to the current validation request.</returns>
  ICssValidationRequest Profile(string profile);

  /// <summary>
  ///   <para>Specifies level of warnings to be included. Only the ones whose level is under or equal to the value specified in the request will be displayed.</para>
  /// </summary>
  /// <param name="level">Level of warnings severity.</param>
  /// <returns>Back reference to the current validation request.</returns>
  ICssValidationRequest Warnings(int? level);
}