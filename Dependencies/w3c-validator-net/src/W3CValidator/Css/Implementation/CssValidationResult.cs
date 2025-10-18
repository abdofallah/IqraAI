using Catharsis.Extensions;

namespace W3CValidator.Css;

public sealed class CssValidationResult : ICssValidationResult
{
    public string Uri { get; set; }
    public bool? Valid { get; set; }
    public DateTimeOffset? Date { get; set; }
    public string CheckedBy { get; set; }
    public string CssLevel { get; set; }
    public Issues Issues { get; set; }

    IIssues ICssValidationResult.Issues => Issues;

    // ... other methods are fine ...
    public int CompareTo(ICssValidationResult other) => Nullable.Compare(Date, other?.Date);
    public bool Equals(ICssValidationResult other) => this.Equality(other, nameof(Uri));
    public override bool Equals(object other) => Equals(other as ICssValidationResult);
    public override int GetHashCode() => this.HashCode(nameof(Uri));
    public override string ToString() => Uri ?? string.Empty;
}