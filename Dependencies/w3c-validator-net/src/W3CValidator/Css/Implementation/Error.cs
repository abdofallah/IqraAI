using Catharsis.Extensions;

namespace W3CValidator.Css;

public sealed class Error : IError
{
  public string Message { get; set; }
  public string Type { get; set; }
  public string Subtype { get; set; }
  public string Property { get; set; }
  public int? Line { get; set; }
  public string Context { get; set; }
  public string SkippedString { get; set; }

  public override string ToString() => !Message.IsUnset() ? Line is not null? $"{Line}:{Message}" : Message : string.Empty;
}