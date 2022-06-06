using Application.Clients.LocalGovImsPaymentApi;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Data;
using Application.Entities;
using Keys = Application.Commands.PaymentResponseParameterKeys;
using System;

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
        private readonly IAsyncRepository<Payment> _paymentRepository;

        private ProcessPaymentModel _processPaymentModel;
        private ProcessPaymentResponseModel _processPaymentResponseModel;
        private Payment _payment;

        public PaymentResponseCommandHandler(
            IConfiguration configuration,
            ILocalGovImsPaymentApiClient localGovImsPaymentApiClient,
            IAsyncRepository<Payment> paymentRepository)
        {
            _configuration = configuration;
            _localGovImsPaymentApiClient = localGovImsPaymentApiClient;
            _paymentRepository = paymentRepository;
        }

        public async Task<ProcessPaymentResponseModel> Handle(PaymentResponseCommand request, CancellationToken cancellationToken)
        {
            // TODO - Handle manual responses e.g. failed - cancelled
            BuildProcessPaymentModel(request.InternalReference, request.Result);

            await GetIntegrationPayment(_processPaymentModel);

            await ProcessPayment();

            await UpdateIntegrationPaymentStatus();

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

        private async Task GetIntegrationPayment(ProcessPaymentModel _processPaymentModel)
        {
            _payment = (await _paymentRepository.Get(x => x.Reference == _processPaymentModel.MerchantReference)).Data;
        }

        private async Task ProcessPayment()
        {
            _processPaymentResponseModel = await _localGovImsPaymentApiClient.ProcessPayment(_processPaymentModel.MerchantReference, _processPaymentModel);
        }
        private async Task UpdateIntegrationPaymentStatus()
        {
            _payment.Status = _processPaymentModel.AuthResult;
            _payment.CardPrefix = _processPaymentModel.CardPrefix;
            _payment.CardSuffix = _processPaymentModel.CardSuffix;
            _payment.CapturedDate = DateTime.Now;
            _payment.Finished = true;
            _payment = (await _paymentRepository.Update(_payment)).Data;
        }
    }
}
