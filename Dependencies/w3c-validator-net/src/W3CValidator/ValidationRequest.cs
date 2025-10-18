using Catharsis.Extensions;

namespace W3CValidator;

/// <summary>
///   <para>Abstract base implementation of <see cref="IValidationRequest"/> contract.</para>
/// </summary>
public abstract class ValidationRequest : IValidationRequest
{
  private readonly Dictionary<string, object> _parameters = new();

  /// <summary>
  ///   <para>Map of parameters (names/values) for the request.</para>
  /// </summary>
  public IReadOnlyDictionary<string, object> Parameters => _parameters;

  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="name"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentNullException">If <paramref name="name"/> is <see langword="null"/>.</exception>
  /// <exception cref="ArgumentException">If <paramref name="name"/> is invalid string.</exception>
  public IValidationRequest WithParameter(string name, object value)
  {
    if (name is null) throw new ArgumentNullException(nameof(name));
    if (name.IsEmpty()) throw new ArgumentException(nameof(name));

    _parameters[name] = value;

    return this;
  }
}