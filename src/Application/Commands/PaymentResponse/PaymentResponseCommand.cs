﻿using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Data;
using Application.Entities;
using Keys = Application.Commands.PaymentResponseParameterKeys;
using System;
using LocalGovImsApiClient.Model;
using Application.Clients.CybersourceRestApiClient.Interfaces;
using System.Linq;

namespace Application.Commands
{
    public class PaymentResponseCommand : IRequest<PaymentResponseCommandResult>
    {
        public string InternalReference { get; set; }
        public string Result { get; set; }
    }

    public class PaymentResponseCommandHandler : IRequestHandler<PaymentResponseCommand, PaymentResponseCommandResult>
    {
        private readonly IConfiguration _configuration;
        private readonly IAsyncRepository<Payment> _paymentRepository;
        private readonly LocalGovImsApiClient.Api.IPendingTransactionsApi _pendingTransactionsApi;
        private readonly ICybersourceRestApiClient _cybersourceRestApiClient;

        private ProcessPaymentModel _processPaymentModel;
        private PaymentResponseCommandResult _result;
        private ProcessPaymentResponse _processPaymentResponse;
        private Payment _payment;

        public PaymentResponseCommandHandler(
            IConfiguration configuration,
            IAsyncRepository<Payment> paymentRepository,
            LocalGovImsApiClient.Api.IPendingTransactionsApi pendingTransactionsApi,
            ICybersourceRestApiClient cybersourceRestApiClient)
        {
            _configuration = configuration;
            _pendingTransactionsApi = pendingTransactionsApi;
            _paymentRepository = paymentRepository;
            _cybersourceRestApiClient = cybersourceRestApiClient;
        }

        public async Task<PaymentResponseCommandResult> Handle(PaymentResponseCommand request, CancellationToken cancellationToken)
        {
            // TODO - Handle manual responses e.g. failed - cancelled
            await BuildProcessPaymentModel(request.InternalReference, request.Result);

            await GetIntegrationPayment(_processPaymentModel);

            await ProcessPayment();

            await UpdateIntegrationPaymentStatus();

            BuildResult();

            return _result;
        }

        private async Task BuildProcessPaymentModel(string internalReference, string result)
        {

            switch (result)
            {
                case AuthorisationResult.Authorised:
                    var paymentCardDetails = await _cybersourceRestApiClient.SearchPayments(internalReference, 1);
                    _processPaymentModel = new ProcessPaymentModel()
                    {
                        AuthResult = LocalGovIMSResults.Authorised,
                        PspReference = paymentCardDetails.FirstOrDefault().Reference,
                        MerchantReference = internalReference,
                    //    PaymentMethod = paymentCardDetails.FirstOrDefault(). paramaters.GetValueOrDefault(Keys.PaymentMethod),
                        CardPrefix = paymentCardDetails.FirstOrDefault().CardPrefix,
                        CardSuffix = paymentCardDetails.FirstOrDefault().CardSuffix,
                        AmountPaid = paymentCardDetails.FirstOrDefault().Amount
                    };
                    break;
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
            //     _processPaymentResponseModel = await _localGovImsPaymentApiClient.ProcessPayment(_processPaymentModel.MerchantReference, _processPaymentModel);
            _processPaymentResponse = await _pendingTransactionsApi.PendingTransactionsProcessPaymentAsync(_processPaymentModel.MerchantReference, _processPaymentModel);
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
        private void BuildResult()
        {
            _result = new PaymentResponseCommandResult()
            {
                NextUrl = _processPaymentResponse.RedirectUrl,
                Success = _processPaymentResponse.Success
            };
        }
    }
}
