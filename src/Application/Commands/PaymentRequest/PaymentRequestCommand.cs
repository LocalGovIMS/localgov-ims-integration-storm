﻿using Application.Builders;
using Application.Models;
using Domain.Exceptions;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Cryptography;
using Application.Data;
using Application.Entities;
using System;
using LocalGovImsApiClient.Client;
using LocalGovImsApiClient.Model;

namespace Application.Commands
{
    public class PaymentRequestCommand : IRequest<StormPayment>
    {
        public string Reference { get; set; }

        public string Hash { get; set; }
    }

    public class PaymentRequestCommandHandler : IRequestHandler<PaymentRequestCommand, StormPayment>
    {
        private readonly IBuilder<PaymentBuilderArgs, StormPayment> _paymentBuilder;
        private readonly ICryptographyService _cryptographyService;
        private readonly IAsyncRepository<Payment> _paymentRepository;
        private readonly LocalGovImsApiClient.Api.IPendingTransactionsApi _pendingTransactionsApi;
        private readonly LocalGovImsApiClient.Api.IProcessedTransactionsApi _processedTransactionsApi;

        private List<PendingTransactionModel> _pendingTransactions;
        private PendingTransactionModel _pendingTransaction;
        private StormPayment _result;
        private Payment _payment;

        public PaymentRequestCommandHandler(
            ICryptographyService cryptographyService,
            IBuilder<PaymentBuilderArgs, StormPayment> paymentBuilder,
            IAsyncRepository<Payment> paymentRepository,
            LocalGovImsApiClient.Api.IPendingTransactionsApi pendingTransactionsApi,
            LocalGovImsApiClient.Api.IProcessedTransactionsApi processedTransactionsApi  )
        {
            _paymentBuilder = paymentBuilder;
            _cryptographyService = cryptographyService;
            _paymentRepository = paymentRepository;
            _pendingTransactionsApi = pendingTransactionsApi;
            _processedTransactionsApi = processedTransactionsApi;
        }

        public async Task<StormPayment> Handle(PaymentRequestCommand request, CancellationToken cancellationToken)
        {
            await ValidateRequest(request);

            GetPendingTransaction();

            await CreateIntegrationPayment(request);

            BuildPayment(request);

            return _result;
        }

        private async Task ValidateRequest(PaymentRequestCommand request)
        {
            ValidateRequestValue(request);
            await CheckThatProcessedTransactionsDoNotExist(request);
            await CheckThatAPendingTransactionExists(request);
        }

        private void ValidateRequestValue(PaymentRequestCommand request)
        {
            if (request.Reference == null || request.Hash == null || request.Hash != _cryptographyService.GetHash(request.Reference))
            {
                throw new PaymentException("The reference provided is not valid");
            }
        }

        private async Task CheckThatProcessedTransactionsDoNotExist(PaymentRequestCommand request)
        {
            if (request.Reference == null || request.Hash == null || request.Hash != _cryptographyService.GetHash(request.Reference))
            {
                throw new PaymentException("The reference provided is not valid");
            }

            try
            {
                var processedTransactions = await _processedTransactionsApi.ProcessedTransactionsSearchAsync(
                    string.Empty,
                    null,
                    string.Empty,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    request.Reference,
                    string.Empty);

                if (processedTransactions != null && processedTransactions.Any())
                {
                    throw new PaymentException("The reference provided is no longer a valid pending payment");
                }
            }
            catch (ApiException ex)
            {
                if (ex.ErrorCode == 404) return; // If no processed transactions are found the API will return a 404 (Not Found) - so that's fine

                throw;
            }
        }

        private async Task CheckThatAPendingTransactionExists(PaymentRequestCommand request)
        {
            try
            {
                var result = await _pendingTransactionsApi.PendingTransactionsGetAsync(request.Reference);

                if (result == null || !result.Any())
                {
                    throw new PaymentException("The reference provided is no longer a valid pending payment");
                }

                _pendingTransactions = result.ToList();
            }
            catch (ApiException ex)
            {
                if (ex.ErrorCode == 404)
                    throw new PaymentException("The reference provided is no longer a valid pending payment");

                throw;
            }
        }

        private void GetPendingTransaction()
        {
            _pendingTransaction = _pendingTransactions.FirstOrDefault();
        }


        private async Task CreateIntegrationPayment(PaymentRequestCommand request)
        {
            _payment = (await _paymentRepository.Add(new Payment()
            {
                Amount = Convert.ToDecimal(_pendingTransactions.Sum(x => x.Amount)),
                CreatedDate = DateTime.Now,
                Identifier = Guid.NewGuid(),
                Reference = request.Reference,
                FailureUrl = _pendingTransaction.FailUrl
            })).Data;
        }

        private void BuildPayment(PaymentRequestCommand request)
        {
            _result = _paymentBuilder.Build(new PaymentBuilderArgs()
            {
                Reference = request.Reference,
                Amount = _pendingTransactions.Sum(x => x.Amount ?? 0),
                Transaction = _pendingTransaction
            });
        }
    }
}
