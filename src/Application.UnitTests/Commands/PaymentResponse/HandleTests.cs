using Application.Commands;
using Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LocalGovImsApiClient.Model;
using Xunit;
using Command = Application.Commands.PaymentResponseCommand;
using Handler = Application.Commands.PaymentResponseCommandHandler;
using System.Threading;
using Application.Data;
using Application.Entities;
using Application.Clients.CybersourceRestApiClient.Interfaces;
using System.Linq.Expressions;
using Application.Result;
using System;

namespace Application.UnitTests.Commands.PaymentResponse
{
    public class HandleTests
    {
        private readonly Handler _commandHandler;
        private Command _command;

        private readonly Mock<IConfiguration> _mockConfiguration = new Mock<IConfiguration>();
        private readonly Mock<LocalGovImsApiClient.Api.IPendingTransactionsApi> _mockPendingTransactionsApi = new Mock<LocalGovImsApiClient.Api.IPendingTransactionsApi>();
        private readonly Mock<IAsyncRepository<Payment>> _mockPaymentRepository = new Mock<IAsyncRepository<Payment>>();
        private readonly Mock<ICybersourceRestApiClient> _mockCybersourceRestApiClient = new Mock<ICybersourceRestApiClient>();

        public HandleTests()
        {
            _commandHandler = new Handler(
                _mockConfiguration.Object,
                _mockPaymentRepository.Object,
                _mockPendingTransactionsApi.Object,
                _mockCybersourceRestApiClient.Object);

            SetupConfig();
            SetupClient(System.Net.HttpStatusCode.OK);
            SetupCommand("", "");
        }

        private void SetupConfig()
        {
            _mockCybersourceRestApiClient.Setup(x => x.SearchPayments(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Payment>() {
                    new Payment()
                    {
                        Amount = 0,
                        Reference = "Test"
                    }
                });

            _mockPaymentRepository.Setup(x => x.Get(It.IsAny<Expression<Func<Payment, bool>>>()))
                .ReturnsAsync(new OperationResult<Payment>(true) { Data = new Payment() { Identifier = Guid.NewGuid(), Reference = "Test" } });

            _mockPaymentRepository.Setup(x => x.Update(It.IsAny<Payment>()))
                .ReturnsAsync(new OperationResult<Payment>(true) { Data = new Payment() { Identifier = Guid.NewGuid(), PaymentId = "paymentId", Reference = "refernce" } });

        }

        private void SetupClient(System.Net.HttpStatusCode statusCode)
        {
            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsProcessPaymentAsync(It.IsAny<string>(), It.IsAny<ProcessPaymentModel>(), 0, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessPaymentResponse() { Success = true });
        }

        private void SetupCommand(string internalReference, string result)
        {
            _command = new Command() { InternalReference = internalReference, Result = result };
        }

        [Fact]
        public async Task Handles_PaymentException_when_request_is_not_valid()
        {
            // Arrange
            SetupCommand("","");

            // Act
            var result =  await _commandHandler.Handle(_command, new System.Threading.CancellationToken());

            // Assert
            result.Should().BeOfType<PaymentResponseCommandResult>();
            result.Success.Should().Be(true);
        }


    }
}
