using System.Diagnostics.CodeAnalysis;

namespace Application.Clients.LocalGovImsPaymentApi
{
    [ExcludeFromCodeCoverage]
    public class ProcessPaymentModel
    {
        public string AuthResult { get; set; }
        public string PspReference { get; set; }
        public string MerchantReference { get; set; }
        public string PaymentMethod { get; set; }
        //
        // Summary:
        //     Gets or Sets CardPrefix
        public string CardPrefix { get; set; }
        //
        // Summary:
        //     Gets or Sets CardSuffix
        public string CardSuffix { get; set; }
    }
}
