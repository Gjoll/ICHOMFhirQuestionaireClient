using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BulkStoreApp
{
    internal class BulkStore
    {
        public class AuthorizationMessageHandler : HttpClientHandler
        {
            AuthenticationHeaderValue? authorization;

            public AuthorizationMessageHandler(String bearerToken)
            {
                authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }


            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Authorization = authorization;
                return base.SendAsync(request, cancellationToken);
            }
        }


        private void StoreFile(String file,
            String baseStoreUrl,
            String patientId,
            String apiToken)
        {
            FhirJsonParser fjp = new FhirJsonParser();
            Resource r = fjp.Parse<Resource>(File.ReadAllText(file));

            AuthorizationMessageHandler auth = null;
            if (String.IsNullOrEmpty(apiToken) == false)
                auth = new AuthorizationMessageHandler(apiToken);
            var client = new FhirClient(baseStoreUrl, new() { PreferredFormat = ResourceFormat.Json }, auth);
            Resource rNew = client.Update(r);
            Console.WriteLine($"Created {r.TypeName}/{r.Id}");
        }

        private void Store(String dir, 
            String baseStoreUrl,
            String patientId,
            String apiToken)
        {
            foreach (String file in Directory.GetFiles(dir, "*.json"))
                StoreFile(file, baseStoreUrl, patientId, apiToken);
        }

        public void StoreAzureHealthCare(String dir)
        {
            String baseUrl =
                "https://hristo-ouch.fhir.azurehealthcareapis.com";
            String apiToken =
                "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiJodHRwczovL2hyaXN0by1vdWNoLmZoaXIuYXp1cmVoZWFsdGhjYXJlYXBpcy5jb20iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83NzRkZDg0OS04OGEyLTRjZDQtODc4My0yMjVhN2I3ZjVlMzcvIiwiaWF0IjoxNjYzNDQ5MjY2LCJuYmYiOjE2NjM0NDkyNjYsImV4cCI6MTY2MzQ1MzE2NiwiYWlvIjoiRTJaZ1lMQ09XUGNvbXNualM3eDNhM3o0cjFjcEFBPT0iLCJhcHBpZCI6IjIxMGU5MmI1LWQ3NWYtNGUyZC04MzE3LWJjYzhmZjA4YTJiNyIsImFwcGlkYWNyIjoiMSIsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0Lzc3NGRkODQ5LTg4YTItNGNkNC04NzgzLTIyNWE3YjdmNWUzNy8iLCJvaWQiOiIzNTAxMGQ3Yi0wYmI2LTRjYTctOTFlYy0yNjk0Yjc4NWJmOTIiLCJyaCI6IjAuQVRrQVNkaE5kNktJMUV5SGd5SmFlMzllTjloNFowX3ZXdHhEb2Ytd2MzSkxsSlU1QUFBLiIsInN1YiI6IjM1MDEwZDdiLTBiYjYtNGNhNy05MWVjLTI2OTRiNzg1YmY5MiIsInRpZCI6Ijc3NGRkODQ5LTg4YTItNGNkNC04NzgzLTIyNWE3YjdmNWUzNyIsInV0aSI6Ik1jSlFKWlhvcDB5dElrQTFMM1JuQUEiLCJ2ZXIiOiIxLjAifQ.hhtA1FcQIkmMohLhAHAqe6w_DM8Nf4KtMlIJWrSonbtGBw9TOtVLVjBllOD6uZAkmTOf0Su0ujOnCqEZFj7oxfs50oBJgtxBmtpWz37cfq4C4dKQSBVuNUYYDdJ2Sv7C86ZQ94wdbUicsW_xjMshkbOfryUYBo_QoUKCsTUfIx_Kuv6X_kHXOvuPJ6WO89ZGGTXEy0UKITWSOvevHXHO6BlgZzzr_tLQhVOAQvuWEdDLYIfDFgiZPbo8OOY5o6oq9BHf1C5hlabrYjbY9RczIrFIsDp0AzyhcykqJffM3rat6eImdzjfp_x6ftvVYlbHR5mSjjfGeu3gw2jBv0KMdA";
            String patientId = "BreastCancerPatient121";
            Store(dir, baseUrl, patientId, apiToken);
        }

        public void StoreFirely(String dir)
        {
            Store(dir, "http://server.fire.ly", "186c5ca5-a858-411a-8166-7b2c374e9f4b", String.Empty);
        }
    }
}
