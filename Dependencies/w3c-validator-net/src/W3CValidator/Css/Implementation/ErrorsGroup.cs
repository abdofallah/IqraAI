using System.Runtime.Serialization;

namespace W3CValidator.Css;

/// <summary>
///   <para>Logical group of validation errors, representing the <errors> element.</para>
/// </summary>
public sealed class ErrorsGroup : IErrorsGroup
{
    public string Uri { get; set; }
    public List<Error> ErrorsList { get; set; } = []; // Changed to setter

    public IEnumerable<IError> Errors => ErrorsList;

    public override string ToString() => Uri ?? string.Empty;
}