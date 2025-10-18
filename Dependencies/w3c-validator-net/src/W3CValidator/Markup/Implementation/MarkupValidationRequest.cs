namespace W3CValidator.Markup;

internal sealed class MarkupValidationRequest : ValidationRequest, IMarkupValidationRequest
{
  public IMarkupValidationRequest Doctype(string doctype)
  {
    WithParameter("doctype", doctype);

    return this;
  }

  public IMarkupValidationRequest Encoding(string encoding)
  {
    WithParameter("charset", encoding);

    return this;
  }
}