using Catharsis.Extensions;
using W3CValidator.Markup;

namespace W3CValidator.Css;

internal sealed class MarkupRequestExecutor : IMarkupRequestExecutor
{
  private Uri EndpointUrl { get; } = "http://validator.w3.org/check".ToUri();
  private IMarkupValidationRequest Request { get; }
  private HttpClient HttpClient { get; } = new();
  private bool Disposed { get; set; }

  public MarkupRequestExecutor(IMarkupValidationRequest request)
  {
    Request = request;
    HttpClient.DefaultRequestHeaders.Add("User-Agent", "W3CValidator.NET client library");
  }

  public async Task<IMarkupValidationResult> UrlAsync(Uri url, CancellationToken cancellation = default)
  {
    if (url is null) throw new ArgumentNullException(nameof(url));

    var parameters = new Dictionary<string, object> {{"uri", url}};

    if (Request is not null)
    {
      parameters.With(Request.Parameters);
    }

    return await Call(parameters, cancellation).ConfigureAwait(false);
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  private void Dispose(bool disposing)
  {
    if (!disposing || Disposed)
    {
      return;
    }

    HttpClient.Dispose();

    Disposed = true;
  }

  private async Task<IMarkupValidationResult> Call(IReadOnlyDictionary<string, object> parameters, CancellationToken cancellation = default)
  {
    if (parameters is null) throw new ArgumentNullException(nameof(parameters));

    if (!parameters.Any())
    {
      throw new MarkupException("No request parameters were specified");
    }

    try
    {
      var uri = EndpointUrl.ToUriBuilder().WithQuery(("output", "soap12")).WithQuery(parameters).Uri;

      using var reader = (await HttpClient.ToStreamAsync(uri, cancellation).ConfigureAwait(false)).ToXmlReader();

      reader.ReadToFollowing("markupvalidationresponse", "http://www.w3.org/2005/10/markup-validator");

      return reader.DeserializeAsXml<MarkupValidationResult>(typeof(MarkupValidationResult));
    }
    catch (Exception exception)
    {
      throw new MarkupException($@"Cannot validate markup of document or document fragment at endpoint URL ""{EndpointUrl}""", exception);
    }
  }
}