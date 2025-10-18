using System.Runtime.Serialization;
using Catharsis.Extensions;

namespace W3CValidator.Markup;

/// <summary>
///   <para></para>
/// </summary>
[CollectionDataContract(Name = "errorlist", ItemName = "error")]
public sealed class ErrorsCollection : List<IIssue>
{
  /// <summary>
  ///   <para></para>
  /// </summary>
  public ErrorsCollection()
  {
  }

  /// <summary>
  ///   <para></para>
  /// </summary>
  /// <param name="errors"></param>
  public ErrorsCollection(IEnumerable<IIssue> errors) => this.With(errors ?? []);
}