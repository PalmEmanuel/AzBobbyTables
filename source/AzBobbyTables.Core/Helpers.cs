using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace PipeHow.AzBobbyTables.Core;

public static class Helpers
{
    private const string ImdsTokenEndpoint = "http://169.254.169.254/metadata/identity/oauth2/token";
    private const string ImdsApiVersion = "2018-02-01";
    private const string ArcImdsApiVersion = "2019-11-01";
    private const string AppServiceApiVersion = "2019-08-01";
    private const string ArcChallengePrefix = "Basic realm=";

    public static string GetManagedIdentityToken(string accountName, string? clientId = null)
    {
        // Get token for managed identity for Storage resource
        string resource = $"https://{accountName}.table.core.windows.net";

        HttpWebRequest request = CreateManagedIdentityRequest(resource, clientId, out bool isArc);

        try
        {
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                HttpWebResponse? challengeResponse = ex.Response as HttpWebResponse;
                if (!isArc || challengeResponse?.StatusCode != HttpStatusCode.Unauthorized)
                {
                    throw;
                }

                using (challengeResponse)
                {
                    string secret = ReadArcSecret(challengeResponse.Headers[HttpResponseHeader.WwwAuthenticate]);
                    request = CreateManagedIdentityRequest(resource, clientId, out _);
                    request.Headers[HttpRequestHeader.Authorization] = $"Basic {secret}";
                    response = (HttpWebResponse)request.GetResponse();
                }
            }

            StreamReader streamResponse = new(response.GetResponseStream());
            string stringResponse = streamResponse.ReadToEnd();

            Dictionary<string, string> tokenDict = JsonSerializer.Deserialize<Dictionary<string, string>>(stringResponse);
            return tokenDict["access_token"];
        }
        catch (Exception ex)
        {
            string errorText = string.Format("{0} \n\n{1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "Acquire token failed");
            throw new WebException(errorText, ex);
        }
    }

    private static HttpWebRequest CreateManagedIdentityRequest(string resource, string? clientId, out bool isArc)
    {
        string? identityEndpoint = Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT");
        string? identityHeader = Environment.GetEnvironmentVariable("IDENTITY_HEADER");
        string? imdsEndpoint = Environment.GetEnvironmentVariable("IMDS_ENDPOINT");

        HttpWebRequest request;
        isArc = string.IsNullOrWhiteSpace(identityHeader) &&
            IsLocalHttpEndpoint(identityEndpoint) &&
            IsLocalHttpEndpoint(imdsEndpoint);

        if (!string.IsNullOrWhiteSpace(identityEndpoint) &&
            (!string.IsNullOrWhiteSpace(identityHeader) || isArc))
        {
            string apiVersion = isArc ? ArcImdsApiVersion : AppServiceApiVersion;
            string uri = $"{identityEndpoint}?api-version={apiVersion}&resource={resource}";
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                uri += $"&client_id={clientId}";
            }
            request = (HttpWebRequest)WebRequest.Create(uri);
            if (isArc)
            {
                request.Headers["Metadata"] = "true";
            }
            else
            {
                request.Headers["X-IDENTITY-HEADER"] = identityHeader;
            }
        }
        else
        {
            // Fall back to VM Instance Metadata Service (IMDS) endpoint
            string uri = $"{ImdsTokenEndpoint}?api-version={ImdsApiVersion}&resource={resource}";
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                uri += $"&client_id={clientId}";
            }
            request = (HttpWebRequest)WebRequest.Create(uri);
            request.Headers["Metadata"] = "true";
        }

        request.Method = "GET";
        return request;
    }

    private static bool IsLocalHttpEndpoint(string? endpoint)
    {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri) &&
            uri.Scheme == Uri.UriSchemeHttp &&
            uri.IsLoopback;
    }

    private static string ReadArcSecret(string? challenge)
    {
        if (challenge == null ||
            string.IsNullOrWhiteSpace(challenge) ||
            !challenge.StartsWith(ArcChallengePrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new WebException("Azure Arc managed identity endpoint returned an invalid authentication challenge.");
        }

        string secretFile = Path.GetFullPath(challenge.Substring(ArcChallengePrefix.Length).Trim().Trim('"'));
        string tokenDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AzureConnectedMachineAgent", "Tokens")
            : "/var/opt/azcmagent/tokens";
        tokenDirectory = Path.GetFullPath(tokenDirectory) + Path.DirectorySeparatorChar;

        StringComparison comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!secretFile.StartsWith(tokenDirectory, comparison))
        {
            throw new WebException("Azure Arc managed identity endpoint returned an invalid secret file path.");
        }

        return File.ReadAllText(secretFile);
    }
}
