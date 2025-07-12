namespace McpTemplate.Common.Interfaces;

public interface IOAuthAuthorizationHandler
{
    /// <summary>
    /// Handles the OAuth authorization URL by starting a local HTTP server and opening a browser.
    /// This implementation demonstrates how SDK consumers can provide their own authorization flow.
    /// </summary>
    /// <param name="authorizationUrl">The authorization URL to open in the browser.</param>
    /// <param name="redirectUri">The redirect URI where the authorization code will be sent.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authorization code extracted from the callback, or null if the operation failed.</returns>
    Task<string?> HandleAuthorizationUrlAsync(Uri authorizationUrl, Uri redirectUri, CancellationToken cancellationToken);
}