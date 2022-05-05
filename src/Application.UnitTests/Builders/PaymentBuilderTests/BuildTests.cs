using Application.Builders;
using Application.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using Xunit;

namespace Application.UnitTests.Builders.PaymentBuilderTests
{
    public class BuildTests
    {
        private IBuilder<PaymentBuilderArgs, Payment> _builder;
        private PaymentBuilderArgs _args;

        private void Arrange()
        {
            SetupBuilder();
            SetupArgs();
        }
        
        private void SetupBuilder()
        {
            _builder = new PaymentBuilder();
        }

        private void SetupArgs()
        {
            _args = new PaymentBuilderArgs()
            {
                Amount = 0.10M,
                Reference = "TestReference",
                CardSelfServiceMopCode = "00",
                Transaction = TestData.GetPendingTransactionModel()
            };
        }

        [Fact]
        public void Build_sets_PaymentAmount()
        {
            // Arrange
            Arrange();

            // Act 
            var result = _builder.Build(_args);

            // Assert
            result.Amount.Should().Be(_args.Amount);
        }

        [Fact]
        public void Build_sets_MerchantReference()
        {
            // Arrange
            Arrange();

            // Act 
            var result = _builder.Build(_args);

            // Assert
            result.InternalReference.Should().Be("TestReference");
        }
        
    }
}
