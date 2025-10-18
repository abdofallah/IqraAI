using W3CValidator.Css;

namespace W3CValidator.Markup;

internal sealed class MarkupValidator : IMarkupValidator
{
  public IMarkupRequestExecutor Request(IMarkupValidationRequest request = null) => new MarkupRequestExecutor(request);
}