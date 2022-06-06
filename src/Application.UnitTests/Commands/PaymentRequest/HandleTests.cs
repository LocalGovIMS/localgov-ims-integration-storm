using Application.Builders;
using Application.Clients.LocalGovImsPaymentApi;
using Application.Cryptography;
using Application.Models;
using Application.Data;
using Application.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Command = Application.Commands.PaymentRequestCommand;
using Handler = Application.Commands.PaymentRequestCommandHandler;

namespace Application.UnitTests.Commands.PaymentRequest
{
    public class HandleTests
    {
        private readonly Handler _commandHandler;
        private Command _command;

        private readonly Mock<ICryptographyService> _mockCryptographyService = new Mock<ICryptographyService>();
        private readonly Mock<ILocalGovImsPaymentApiClient> _mockLocalGovImsPaymentApiClient = new Mock<ILocalGovImsPaymentApiClient>();
        private readonly Mock<IBuilder<PaymentBuilderArgs, StormPayment>> _mockBuilder = new Mock<IBuilder<PaymentBuilderArgs, StormPayment>>();
        private readonly Mock<IAsyncRepository<Payment>> _mockPaymentRepository = new Mock<IAsyncRepository<Payment>>();

        public HandleTests()
        {
            _commandHandler = new Handler(
                _mockCryptographyService.Object,
                _mockLocalGovImsPaymentApiClient.Object,
                _mockBuilder.Object,
                _mockPaymentRepository.Object);

            SetupClient(System.Net.HttpStatusCode.OK);
            SetupCommand("reference", "hash");
        }

        private void SetupClient(System.Net.HttpStatusCode statusCode)
        {
            _mockLocalGovImsPaymentApiClient.Setup(x => x.GetProcessedTransactions(It.IsAny<string>()))
                .ReturnsAsync((List<ProcessedTransactionModel>)null);

            _mockLocalGovImsPaymentApiClient.Setup(x => x.GetPendingTransactions(It.IsAny<string>()))
                .ReturnsAsync(new List<PendingTransactionModel>() {
                    new PendingTransactionModel()
                    {
                        Reference = "Test"
                    }
                });

            _mockBuilder.Setup(x => x.Build(It.IsAny<PaymentBuilderArgs>()))
                .Returns(new StormPayment());
        }

        private void SetupCommand(string reference, string hash)
        {
            _command = new Command() { Reference = reference, Hash = hash };
        }

        [Fact]
        public async Task Handle_throws_PaymentException_when_the_reference_is_null()
        {
            // Arrange
            SetupCommand(null, "hash");

            // Act
            async Task task() => await _commandHandler.Handle(_command, new System.Threading.CancellationToken());

            // Assert
            var result = await Assert.ThrowsAsync<PaymentException>(task);
            result.Message.Should().Be("The reference provided is not valid");
        }

        [Fact]
        public async Task Handle_throws_PaymentException_when_the_hash_is_null()
        {
            // Arrange
            SetupCommand("reference", null);

            // Act
            async Task task() => await _commandHandler.Handle(_command, new System.Threading.CancellationToken());

            // Assert
            var result = await Assert.ThrowsAsync<PaymentException>(task);
            result.Message.Should().Be("The reference provided is not valid");
        }
        

        [Fact]
        public async Task Handle_throws_PaymentException_when_processed_transactions_exists_for_the_reference()
        {
            // Arrange
            _mockLocalGovImsPaymentApiClient.Setup(x => x.GetProcessedTransactions(It.IsAny<string>()))
                .ReturnsAsync(new List<ProcessedTransactionModel>() {
                    new ProcessedTransactionModel()
                    {
                        Reference = "Test"
                    }
                });

            // Act
            async Task task() => await _commandHandler.Handle(_command, new System.Threading.CancellationToken());

            // Assert
            var result = await Assert.ThrowsAsync<PaymentException>(task);
            result.Message.Should().Be("The reference provided is not valid");
        }

        [Fact]
        public async Task Handle_throws_PaymentException_when_pending_transactions_do_not_exist_for_the_reference()
        {
            // Arrange
            _mockLocalGovImsPaymentApiClient.Setup(x => x.GetPendingTransactions(It.IsAny<string>()))
                .ReturnsAsync((List<PendingTransactionModel>)null);

            // Act
            async Task task() => await _commandHandler.Handle(_command, new System.Threading.CancellationToken());

            // Assert
            var result = await Assert.ThrowsAsync<PaymentException>(task);
            result.Message.Should().Be("The reference provided is not valid");
        }

    }
}
