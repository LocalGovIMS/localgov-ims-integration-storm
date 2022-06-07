using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Application.Clients.CybersourceRestApiClient.Interfaces;
using Application.Commands;
using CyberSource.Api;
using CyberSource.Client;
using CyberSource.Model;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Application.Entities;


namespace Infrastructure.Clients
{
    public class CybersourceRestApiClient : ICybersourceRestApiClient
    {
        private readonly string _restApiEndpoint;
        private readonly string _merchantId;
        private readonly string _restSharedSecretId;
        private readonly string _restSharedSecretKey;
        private List<Payment> _uncapturedPayments = new();

        private readonly Dictionary<string, string> _configurationDictionary = new();

        public CybersourceRestApiClient(IConfiguration configuration)
        {
            _restApiEndpoint = configuration.GetValue<string>("CybersourceMoto:RestApiEndpoint");
            _merchantId = configuration.GetValue<string>("CybersourceMoto:MerchantId");
            _restSharedSecretId = configuration.GetValue<string>("CybersourceMoto:RestSharedSecretId");
            _restSharedSecretKey = configuration.GetValue<string>("CybersourceMoto:RestSharedSecretKey");

            SetupConfigDictionary();
        }

        public async Task<List<Payment>> GetPayments(string clientReference)
        {
            var clientConfig = new Configuration(merchConfigDictObj: _configurationDictionary);
            var apiInstance = new TransactionDetailsApi(clientConfig);
            var searchResults = await apiInstance.GetTransactionAsync(clientReference);

            return _uncapturedPayments;
        }

        public async Task<List<Payment>> SearchPayments(string clientReference, int daysAgo)
        {
            var requestObj = new CreateSearchRequest(
                Save: false,
                Name: "MRN",
                Timezone: "Europe/London",
                Query: BuildSearchQuery(clientReference, daysAgo),
                Offset: 0,
                Limit: 1000,
                Sort: "submitTimeUtc:desc"
            );

            try
            {
                var clientConfig = new Configuration(merchConfigDictObj: _configurationDictionary);

                var apiInstance = new SearchTransactionsApi(clientConfig);
                var searchResult = await apiInstance.CreateSearchAsync(requestObj);

                if (searchResult == null || searchResult.Count == 0)
                    return _uncapturedPayments;

                if (searchResult.Embedded.TransactionSummaries.All(x
                        => string.IsNullOrWhiteSpace(x.ProcessorInformation.ApprovalCode)))
                    return _uncapturedPayments;

                var activeResults = searchResult.Embedded.TransactionSummaries.Where(x =>
                    !string.IsNullOrWhiteSpace(x.ProcessorInformation.ApprovalCode));

                foreach (var matchingResult in activeResults)
                {
                    _uncapturedPayments.Add(new Payment
                    {
                        CreatedDate = DateTime.Now,
                        Identifier = Guid.NewGuid(),
                        Reference = matchingResult.Id,
                        Amount = decimal.Parse(matchingResult.OrderInformation.AmountDetails.TotalAmount),
                        PaymentId = matchingResult.ClientReferenceInformation.Code,
                        CapturedDate = Convert.ToDateTime(matchingResult.SubmitTimeUtc),
                        CardPrefix = matchingResult.PaymentInformation.Card.Prefix,
                        CardSuffix = matchingResult.PaymentInformation.Card.Suffix,
                    });
                }
                return _uncapturedPayments;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception on calling the API : " + e.Message);
                return _uncapturedPayments; // TODO: fix
            }
        }

        private static string BuildSearchQuery(string clientReference, int daysAgo)
        {
            var query = "";
            var submitTimeUtcQuery = "[NOW/DAY-" + daysAgo + "DAY" + ((daysAgo > 1) ? "S" : "") + " TO NOW/DAY+1DAY}";

            if (clientReference != "")
            {
                query = "clientReferenceInformation.code:" + clientReference +
                        " AND submitTimeUtc:" + submitTimeUtcQuery;
            }
            else
            {
                query = "submitTimeUtc:" + submitTimeUtcQuery;
            }

            return query;
        }

        private void SetupConfigDictionary()
        {
            // General configuration
            _configurationDictionary.Add("authenticationType", "HTTP_SIGNATURE");
            _configurationDictionary.Add("merchantID", _merchantId);
            _configurationDictionary.Add("merchantKeyId", _restSharedSecretId);
            _configurationDictionary.Add("merchantsecretKey", _restSharedSecretKey);
            _configurationDictionary.Add("runEnvironment", _restApiEndpoint);
            _configurationDictionary.Add("timeout", "300000");

            // Configs related to meta key
            _configurationDictionary.Add("portfolioID", string.Empty);
            _configurationDictionary.Add("useMetaKey", "false");

            // Configs related to OAuth
            _configurationDictionary.Add("enableClientCert", "false");
        }
    }
}