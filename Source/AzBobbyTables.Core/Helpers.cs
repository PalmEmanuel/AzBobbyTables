using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;

namespace PipeHow.AzBobbyTables.Core;

public static class Helpers
{
    public static string GetManagedIdentityToken(string accountName)
    {
        // Get token for managed identity for Storage resource
        string resource = $"https://{accountName}.table.core.windows.net";
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT")}?api-version=2019-08-01&resource={resource}");
        request.Headers["X-IDENTITY-HEADER"] = Environment.GetEnvironmentVariable("IDENTITY_HEADER");
        request.Method = "GET";

        try
        {
            // request token
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // extract token from responsestream
            StreamReader streamResponse = new(response.GetResponseStream());
            string stringResponse = streamResponse.ReadToEnd();

            // deserialize token from JSON
            Dictionary<string, string> tokenDict = JsonSerializer.Deserialize<Dictionary<string, string>>(stringResponse);
            return tokenDict["access_token"];
        }
        catch (Exception ex)
        {
            string errorText = string.Format("{0} \n\n{1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "Acquire token failed");
            throw new WebException(errorText, ex);
        }
    }
}
