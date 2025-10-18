namespace W3CValidator.Css;

/// <summary>
///   <para>Level of validation warnings severity for filtering.</para>
/// </summary>
public enum WarningsLevel
{
  /// <summary>
  ///   <para>All warnings.</para>
  /// </summary>
  All = 2,

  /// <summary>
  ///   <para>Normal warnings.</para>
  /// </summary>
  Normal = 1,

  /// <summary>
  ///   <para>Important warnings.</para>
  /// </summary>
  Important = 0,

  /// <summary>
  ///   <para>No warnings.</para>
  /// </summary>
  None = -1,
}