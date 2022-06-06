using Application.Clients.LocalGovImsPaymentApi;
using Application.Models;

namespace Application.Builders
{
    public class PaymentBuilder : IBuilder<PaymentBuilderArgs, StormPayment>
    {
        private StormPayment _payment;

        public StormPayment Build(PaymentBuilderArgs args)
        {
            CreatePayment(args);            

            return _payment;
        }

        private void CreatePayment(PaymentBuilderArgs args)
        {
            _payment = new StormPayment
            {
                Amount = args.Amount,
                InternalReference = args.Reference,
            };
        }

    }

    public class PaymentBuilderArgs
    {
        public string Reference { get; set; }
        public decimal Amount { get; set; }
        public string CardSelfServiceMopCode { get; set; }
        public PendingTransactionModel Transaction { get; set; }
    }
}
