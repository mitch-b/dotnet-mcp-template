using McpTemplate.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Web;

namespace McpTemplate.Console.Handlers;

public class OAuthAuthorizationHandler(ILogger<OAuthAuthorizationHandler> logger) : IOAuthAuthorizationHandler
{
    private readonly ILogger<OAuthAuthorizationHandler> _logger = logger;

    /// <summary>
    /// Opens the specified URL in the default web browser.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    private void OpenBrowser(Uri url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url.ToString(),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to open browser: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the OAuth authorization URL by starting a local HTTP server and opening a browser.
    /// This implementation demonstrates how SDK consumers can provide their own authorization flow.
    /// </summary>
    /// <param name="authorizationUrl">The authorization URL to open in the browser.</param>
    /// <param name="redirectUri">The redirect URI where the authorization code will be sent.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authorization code extracted from the callback, or null if the operation failed.</returns>
    public async Task<string?> HandleAuthorizationUrlAsync(Uri authorizationUrl, Uri redirectUri, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting OAuth authorization flow...");
        // Remove any '&resource=...' or '?resource=...' from the authorizationUrl
        var uriBuilder = new UriBuilder(authorizationUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        if (query["resource"] != null)
        {
            query.Remove("resource");
            uriBuilder.Query = query.ToString();
            _logger.LogInformation($"Removed 'resource' from authorization URL. New URL: {uriBuilder.Uri}");
        }
        else
        {
            _logger.LogInformation($"Opening browser to: {authorizationUrl}");
        }
        var cleanAuthorizationUrl = uriBuilder.Uri;

        var listenerPrefix = redirectUri.GetLeftPart(UriPartial.Authority);
        if (!listenerPrefix.EndsWith("/"))
        {
            listenerPrefix += "/";
        }

        using var listener = new HttpListener();
        listener.Prefixes.Add(listenerPrefix);

        try
        {
            listener.Start();
            _logger.LogInformation($"Listening for OAuth callback on: {listenerPrefix}");

            OpenBrowser(cleanAuthorizationUrl);

            var context = await listener.GetContextAsync();
            var callbackQuery = HttpUtility.ParseQueryString(context.Request.Url?.Query ?? string.Empty);
            var code = callbackQuery["code"];
            var error = callbackQuery["error"];

            string responseHtml = "<html><body><h1>Authentication complete</h1><p>You can close this window now.</p></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "text/html";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();

            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogError($"Auth error: {error}");
                return null;
            }

            if (string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("No authorization code received");
                return null;
            }

            _logger.LogInformation("Authorization code received successfully.");
            return code;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting auth code: {ex.Message}");
            return null;
        }
        finally
        {
            if (listener.IsListening)
            {
                listener.Stop();
            }
        }
    }
}