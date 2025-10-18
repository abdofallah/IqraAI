namespace W3CValidator;

/// <summary>
///   <para>Represents a custom request for any W3C validation service.</para>
/// </summary>
public interface IValidationRequest
{
  /// <summary>
  ///   <para>Map of parameters (names/values) for the request.</para>
  /// </summary>
  IReadOnlyDictionary<string, object> Parameters { get; }

  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="name"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  IValidationRequest WithParameter(string name, object value);
}