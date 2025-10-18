using System.Runtime.Serialization;
using Catharsis.Extensions;

namespace W3CValidator.Markup;

/// <summary>
///   <para></para>
/// </summary>
[CollectionDataContract(Name = "warninglist", ItemName = "warning")]
public sealed class WarningsCollection : List<IIssue>
{
  /// <summary>
  ///   <para></para>
  /// </summary>
  public WarningsCollection()
  {
  }

  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="warnings"></param>
  public WarningsCollection(IEnumerable<IIssue> warnings) => this.With(warnings ?? []);
}