using System;
using System.Net;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Xml.Linq;


namespace SustainabilityAPI
{
    public class Metadata
    {

        private readonly ILogger _logger;
        private readonly HttpClient _client;
        private readonly string _instanceId;
        private readonly string _enrollmentId;
        private readonly string _ocpKey;
        private readonly TokenCredential _credential;


        public Metadata(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Metadata>();
            _client = new HttpClient();
            _instanceId = Environment.GetEnvironmentVariable("INSTANCE_ID");
            _enrollmentId = Environment.GetEnvironmentVariable("ENROLLMENT_ID");
            _ocpKey = Environment.GetEnvironmentVariable("Ocp_Apim_Subscription_Key");

        }

        [Function("Metadata")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {

            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var apiUrl = $"https://api.mcfs.microsoft.com/api/v1.0/instances/{_instanceId}/enrollments/{_enrollmentId}/$metadata";
            var userAssignedClientId = Environment.GetEnvironmentVariable("MSI_CLIENT_ID");
            var _credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId });
            var accessToken = _credential.GetToken(new TokenRequestContext(new[] { "https://vault.azure.net" }));

            _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken.Token);
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _ocpKey);

            
            var response = await _client.GetAsync(apiUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            XElement incomingXml = XElement.Parse(responseContent);
            _logger.LogInformation($"response is {incomingXml}");

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
           
            httpResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");


            return httpResponse;
        }
    }
}





