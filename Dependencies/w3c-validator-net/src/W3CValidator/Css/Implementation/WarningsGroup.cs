using System.Runtime.Serialization;

namespace W3CValidator.Css;

/// <summary>
///   <para>Logical group of validation warnings, representing the <warnings> element.</para>
/// </summary>
public sealed class WarningsGroup : IWarningsGroup
{
    public string Uri { get; set; }
    public List<Warning> WarningsList { get; set; } = [];

    public IEnumerable<IWarning> Warnings => WarningsList;

    public override string ToString() => Uri ?? string.Empty;
}