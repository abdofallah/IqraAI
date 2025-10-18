namespace W3CValidator.Css;

internal sealed class CssValidator : ICssValidator
{
  public ICssRequestExecutor Request(ICssValidationRequest request = null) => new CssRequestExecutor(request);
}