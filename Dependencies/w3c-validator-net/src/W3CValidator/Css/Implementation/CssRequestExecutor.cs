using Catharsis.Extensions;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace W3CValidator.Css;

internal sealed class CssRequestExecutor : ICssRequestExecutor
{
    private Uri EndpointUrl { get; } = "http://jigsaw.w3.org/css-validator/validator".ToUri();
    private ICssValidationRequest Request { get; }
    private HttpClient HttpClient { get; } = new();
    private bool Disposed { get; set; }

    public CssRequestExecutor(ICssValidationRequest request) => Request = request;

    public async Task<ICssValidationResult> DocumentAsync(string document, CancellationToken cancellation = default)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        if (document.IsEmpty()) throw new ArgumentException(nameof(document));

        var parameters = new Dictionary<string, object> { { "text", document } };

        if (Request is not null)
        {
            parameters.With(Request.Parameters);
        }

        return await Call(parameters, cancellation).ConfigureAwait(false);
    }

    public async Task<ICssValidationResult> UrlAsync(Uri url, CancellationToken cancellation = default)
    {
        if (url is null) throw new ArgumentNullException(nameof(url));

        var parameters = new Dictionary<string, object> { { "uri", url } };

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

    private async Task<ICssValidationResult> Call(IReadOnlyDictionary<string, object> parameters, CancellationToken cancellation = default)
    {
        if (parameters is null) throw new ArgumentNullException(nameof(parameters));
        if (!parameters.Any()) throw new CssException("No request parameters were specified");

        try
        {
            var builder = EndpointUrl.ToUriBuilder();
            builder = builder.WithQuery(("output", "soap12"));
            foreach (var param in parameters)
            {
                builder = builder.WithQuery((param.Key, param.Value));
            }
            var uri = builder.Uri;

            // --- Start of Modern Parsing Logic ---

            // 1. Load the entire response into an easy-to-use XDocument
            using var stream = await HttpClient.GetStreamAsync(uri, cancellation).ConfigureAwait(false);
            XDocument doc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellation).ConfigureAwait(false);

            // 2. Define the namespaces. This makes querying the document trivial.
            XNamespace m = "http://www.w3.org/2005/07/css-validator";

            // 3. Find the main <cssvalidationresponse> element.
            var responseElement = doc.Descendants(m + "cssvalidationresponse").FirstOrDefault();
            if (responseElement == null)
            {
                throw new CssException("The <cssvalidationresponse> element was not found in the SOAP response.");
            }

            // 4. Manually and safely map the XML data to your C# objects.
            var result = new CssValidationResult
            {
                Uri = (string)responseElement.Element(m + "uri"),
                CheckedBy = (string)responseElement.Element(m + "checkedby"),
                CssLevel = (string)responseElement.Element(m + "csslevel"),
                Date = (DateTimeOffset?)responseElement.Element(m + "date"),
                Valid = (bool?)responseElement.Element(m + "validity"),
            };

            var resultElement = responseElement.Element(m + "result");
            if (resultElement != null)
            {
                var issues = new Issues();

                // Populate Errors
                var errorsElement = resultElement.Element(m + "errors");
                if (errorsElement != null)
                {
                    // Find all <errorlist> elements and project them into your ErrorsGroup class
                    issues.ErrorsGroupsList = errorsElement.Elements(m + "errorlist")
                        .Select(el => new ErrorsGroup
                        {
                            Uri = (string)el.Element(m + "uri"),
                            ErrorsList = el.Elements(m + "error")
                                .Select(er => new Error
                                {
                                    Line = (int?)er.Element(m + "line"),
                                    Type = (string)er.Element(m + "errortype"),
                                    Message = ((string)er.Element(m + "message"))?.Trim()
                                    // Add other Error properties here if needed
                                }).ToList()
                        }).ToList();
                }

                // Populate Warnings (handles the case of multiple <warninglist> elements)
                var warningsElement = resultElement.Element(m + "warnings");
                if (warningsElement != null)
                {
                    issues.WarningsGroupsList = warningsElement.Elements(m + "warninglist")
                        .Select(wl => new WarningsGroup
                        {
                            Uri = (string)wl.Element(m + "uri"),
                            WarningsList = wl.Elements(m + "warning")
                                .Select(wr => new Warning
                                {
                                    Line = (int?)wr.Element(m + "line"),
                                    Level = (int?)wr.Element(m + "level"),
                                    Message = ((string)wr.Element(m + "message"))?.Trim()
                                }).ToList()
                        }).ToList();
                }

                result.Issues = issues;
            }

            return result;

            // --- End of Modern Parsing Logic ---
        }
        catch (Exception exception)
        {
            throw new CssException($"Cannot validate CSS document. Endpoint URL: \"{EndpointUrl}\"", exception);
        }
    }
}