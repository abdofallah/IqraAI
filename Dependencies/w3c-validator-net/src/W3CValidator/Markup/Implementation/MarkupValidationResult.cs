using System.Runtime.Serialization;
using Catharsis.Extensions;

namespace W3CValidator.Markup;

/// <summary>
///   <para>Representation of response from W3C Markup validation web service that contains detailed information concerning validation results.</para>
/// </summary>
[DataContract(Name = "markupvalidationresponse", Namespace = "http://www.w3.org/2005/10/markup-validator")]
public sealed class MarkupValidationResult : IComparable<IMarkupValidationResult>, IEquatable<IMarkupValidationResult>, IMarkupValidationResult
{
  /// <summary>
  ///   <para>The address of the document validated.</para>
  /// </summary>
  [DataMember(Name = "uri", IsRequired = true)]
  public string Uri { get; set; }

  /// <summary>
  ///   <para>Whether or not the document validated passed or not formal validation.</para>
  /// </summary>
  [DataMember(Name = "validity", IsRequired = true)]
  public bool? Valid { get; set; }

  /// <summary>
  ///   <para>The actual date of the validation.</para>
  /// </summary>
  public DateTimeOffset? Date { get; set; }

  /// <summary>
  ///   <para>Location of the service which provided the validation result.</para>
  /// </summary>
  [DataMember(Name = "checkedby", IsRequired = true)]
  public string CheckedBy { get; set; }

  /// <summary>
  ///   <para>Detected (or forced) Document Type for the validated document.</para>
  /// </summary>
  [DataMember(Name = "doctype", IsRequired = true)]
  public string Doctype { get; set; }

  /// <summary>
  ///   <para>Detected (or forced) Character Encoding for the validated document.</para>
  /// </summary>
  [DataMember(Name = "charset", IsRequired = true)]
  public string Encoding { get; set; }

  /// <summary>
  ///   <para>Collection of errors encountered through the validation process.</para>
  /// </summary>
  [DataMember(Name = "errors", IsRequired = true)]
  public IErrorsGroup ErrorsGroup { get; set; }

  /// <summary>
  ///   <para>Collection of warnings encountered through the validation process.</para>
  /// </summary>
  [DataMember(Name = "warnings", IsRequired = true)]
  public IWarningsGroup WarningsGroup { get; set; }

  /// <summary>
  ///   <para>Compares the current <see cref="IMarkupValidationResult"/> instance with another.</para>
  /// </summary>
  /// <returns>A value that indicates the relative order of the instances being compared.</returns>
  /// <param name="other">The <see cref="IMarkupValidationResult"/> to compare with this instance.</param>
  public int CompareTo(IMarkupValidationResult other) => Nullable.Compare(Date, other?.Date);

  /// <summary>
  ///   <para>Determines whether two <see cref="IMarkupValidationResult"/> instances are equal.</para>
  /// </summary>
  /// <param name="other">The instance to compare with the current one.</param>
  /// <returns><c>true</c> if specified instance is equal to the current, <c>false</c> otherwise.</returns>
  public bool Equals(IMarkupValidationResult other) => this.Equality(other, nameof(Uri));

  /// <summary>
  ///   <para>Determines whether the specified <see cref="object"/> is equal to the current <see cref="object"/>.</para>
  /// </summary>
  /// <param name="other">The object to compare with the current object.</param>
  /// <returns><c>true</c> if the specified object is equal to the current object, <c>false</c>.</returns>
  public override bool Equals(object other) => Equals(other as IMarkupValidationResult);

  /// <summary>
  ///   <para>Returns hash code for the current object.</para>
  /// </summary>
  /// <returns>Hash code of current instance.</returns>
  public override int GetHashCode() => this.HashCode(nameof(Uri));

  /// <summary>
  ///   <para>Returns a <see cref="string"/> that represents the current <see cref="MarkupValidationResult"/> instance.</para>
  /// </summary>
  /// <returns>A string that represents the current <see cref="MarkupValidationResult"/>.</returns>
  public override string ToString() => Uri ?? string.Empty;
}