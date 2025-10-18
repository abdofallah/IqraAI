namespace W3CValidator.Css;

public sealed class Warning : IWarning
{
  public string Message { get; set; }
  public int? Level { get; set; }
  public int? Line { get; set; }
  public string Context { get; set; }

  public override string ToString() => $"{Line}:{Level} {Message}";
}