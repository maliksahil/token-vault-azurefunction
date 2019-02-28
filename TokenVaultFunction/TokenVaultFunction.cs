using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Services.AppAuthentication;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using Dropbox.Api;
using System.Linq;

namespace TokenVaultFunction
{
    public static class TokenVaultFunction
    {
        [FunctionName("TokenVaultFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string tokenVaultResource = "https://tokenvault.azure.net";
            // update the below with your resource URL
            string tokenResourceUrl = "https://<yourtokenvault>.westcentralus.tokenvault.azure.net/services/dropbox/tokens/sampleToken";

            try
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                // Get a token to access Token Vault
                string tokenVaultApiToken = await azureServiceTokenProvider.GetAccessTokenAsync(tokenVaultResource);

                // Get Dropbox token from Token Vault
                var request = new HttpRequestMessage(HttpMethod.Post, $"{tokenResourceUrl}/accesstoken");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenVaultApiToken);
                HttpClient client = new HttpClient();
                var response = await client.SendAsync(request);
                var dropboxApiToken = await response.Content.ReadAsStringAsync();

                // ViewBag.Secret = $"Token: {dropboxApiToken}";

                var filesList = new List<string>();

                if (!string.IsNullOrEmpty(dropboxApiToken))
                {
                    using (var dbx = new DropboxClient(dropboxApiToken))
                    {
                        var list = await dbx.Files.ListFolderAsync(string.Empty);

                        // show folders then files
                        foreach (var item in list.Entries.Where(i => i.IsFolder))
                        {
                            filesList.Add($"D  {item.Name}/");
                        }

                        foreach (var item in list.Entries.Where(i => i.IsFile))
                        {
                            filesList.Add($"F  {item.Name}");
                        }
                    }
                }
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (var file in filesList)
                {
                    sb.Append(file);
                }
                return (ActionResult)new OkObjectResult($"Files: {sb.ToString()}");
            }
            catch (Exception exp)
            {
                return (ActionResult)new OkObjectResult($"Error: {exp.ToString()}");
            }
        }
    }
}
