namespace W3CValidator.Css;

internal sealed class CssValidationRequest : ValidationRequest, ICssValidationRequest
{
  public ICssValidationRequest Language(string language)
  {
    WithParameter("lang", language);

    return this;
  }

  public ICssValidationRequest Medium(string medium)
  {
    WithParameter("user-medium", medium);

    return this;
  }

  public ICssValidationRequest Profile(string profile)
  {
    WithParameter("profile", profile);

    return this;
  }

  public ICssValidationRequest Warnings(int? warnings)
  {
    WithParameter("warning", warnings);

    return this;
  }
}