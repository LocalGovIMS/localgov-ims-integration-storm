using Application.Clients.LocalGovImsPaymentApi;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Keys = Application.Commands.PaymentResponseParameterKeys;

namespace Application.Commands
{
    public class PaymentResponseCommand : IRequest<ProcessPaymentResponseModel>
    {
        public Dictionary<string, string> Paramaters { get; set; }
        public string InternalReference { get; set; }
        public string Result { get; set; }
    }

    public class PaymentResponseCommandHandler : IRequestHandler<PaymentResponseCommand, ProcessPaymentResponseModel>
    {
        private readonly IConfiguration _configuration;
        private readonly ILocalGovImsPaymentApiClient _localGovImsPaymentApiClient;

        private ProcessPaymentModel _processPaymentModel;
        private ProcessPaymentResponseModel _processPaymentResponseModel;

        public PaymentResponseCommandHandler(
            IConfiguration configuration,
            ILocalGovImsPaymentApiClient localGovImsPaymentApiClient)
        {
            _configuration = configuration;
            _localGovImsPaymentApiClient = localGovImsPaymentApiClient;
        }

        public async Task<ProcessPaymentResponseModel> Handle(PaymentResponseCommand request, CancellationToken cancellationToken)
        {
            // TODO - Handle manual responses e.g. failed - cancelled
            BuildProcessPaymentModel(request.InternalReference, request.Result);

            await ProcessPayment();

            return _processPaymentResponseModel;
        }

        private void BuildProcessPaymentModel(string internalReference, string result)
        {

            switch (result)
            {
                case AuthorisationResult.Cancelled:
                    _processPaymentModel = new ProcessPaymentModel()
                    {
                        AuthResult = LocalGovIMSResults.Cancelled,
                        MerchantReference = internalReference
                    };
                    break;
                case AuthorisationResult.Failed:
                    _processPaymentModel = new ProcessPaymentModel()
                    {
                        AuthResult = LocalGovIMSResults.Refused,
                        MerchantReference = internalReference
                    };
                    break;
                default:
                    _processPaymentModel = new ProcessPaymentModel()
                    {
                        AuthResult = LocalGovIMSResults.Error,
                        MerchantReference = internalReference
                    };
                    break;
            }
        }

        private async Task ProcessPayment()
        {
            _processPaymentResponseModel = await _localGovImsPaymentApiClient.ProcessPayment(_processPaymentModel.MerchantReference, _processPaymentModel);
        }
    }
}
