using Application.Builders;
using Application.Models;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Cryptography;
using Application.Entities;
using Application.Clients.CybersourceRestApiClient.Interfaces;


namespace Application.Commands
{
    public class PollForUpdateCommand : IRequest<StormPayment>
    {
        public string Reference { get; set; }

        public string Hash { get; set; }

        public decimal Amount { get; set; }
    }

    public class PollForUpdateCommandHandler : IRequestHandler<PollForUpdateCommand, StormPayment>
    {
        private readonly ICybersourceRestApiClient _cybersourceRestApiClient;

        private StormPayment _result;
        private List<Payment> _uncapturedPayments = new();
        private bool success = false;

        public PollForUpdateCommandHandler(
            ICybersourceRestApiClient cybersourceRestApiClient)
        {
            _cybersourceRestApiClient = cybersourceRestApiClient;
        }

        public async Task<StormPayment> Handle(PollForUpdateCommand request, CancellationToken cancellationToken)
        {
            await PollForCompletedPayment(request);

            BuildPayment(request);

            return _result;
        }

        private async Task PollForCompletedPayment(PollForUpdateCommand request)
        {
            _uncapturedPayments = await _cybersourceRestApiClient.SearchPayments(request.Reference,1);
            if (_uncapturedPayments != null && _uncapturedPayments.Count > 0)
            {
                success = true;
            }
        }
        private void BuildPayment(PollForUpdateCommand request)
        {
            _result = new StormPayment
            {
                InternalReference = request.Reference,
                Amount = request.Amount,
                Success = success,
            };

        }

    }
}
