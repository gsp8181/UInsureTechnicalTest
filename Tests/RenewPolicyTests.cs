using Xunit;
using Moq;
using UInsureTechnicalTest.DTOs;
using UInsureTechnicalTest.Models;
using UInsureTechnicalTest.Services;

namespace UInsureTechnicalTest.Tests
{
    public class RenewPolicyTests
    {
        [Fact]
        public async Task RenewPolicyAsync_ThrowsInvalidOperationException_WhenMoreThan30DaysBeforeEndDate()
        {
            // Arrange
            var policyReference = "POL123";
            var policy = new Policy
            {
                PolicyReference = policyReference,
                StartDate = DateTime.Now.AddYears(-1),
                EndDate = DateTime.Now.AddDays(31),
                Amount = 100m
            };
            var mockRepo = new Mock<IPolicyRepository>();
            mockRepo.Setup(r => r.GetPolicyAsync(policyReference)).ReturnsAsync(policy);
            var service = new PolicyService(mockRepo.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RenewPolicyAsync(policyReference));

            Assert.Equal("A policy can only be renewed 30 days before the end date of the current policy.", exception.Message);
        }

        [Fact]
        public async Task RenewPolicyAsync_ThrowsInvalidOperationException_WhenAfterEndDate()
        {
            // Arrange
            var policyReference = "POL456";
            var policy = new Policy
            {
                PolicyReference = policyReference,
                StartDate = DateTime.Now.AddYears(-1),
                EndDate = DateTime.Now.AddDays(-1),
                Amount = 100m
            };
            var mockRepo = new Mock<IPolicyRepository>();
            mockRepo.Setup(r => r.GetPolicyAsync(policyReference)).ReturnsAsync(policy);
            var service = new PolicyService(mockRepo.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RenewPolicyAsync(policyReference));

            Assert.Equal("A policy cannot be renewed after the end date.", exception.Message);
        }

        [Fact]
        public async Task RenewPolicyAsync_CreatesPayment_WhenAutoRenewIsTrue()
        {
            // Arrange
            var policyReference = "POL789";
            var policy = new Policy
            {
                PolicyReference = policyReference,
                StartDate = DateTime.Now.AddYears(-1),
                EndDate = DateTime.Now.AddDays(10),
                Amount = 100m,
                AutoRenew = true,
                Payments = new List<Payment>
                {
                    new Payment { PaymentReference = "PAY-1", Amount = 100m, Type = PaymentMethod.Card }
                }
            };
            var mockRepo = new Mock<IPolicyRepository>();
            mockRepo.Setup(r => r.GetPolicyAsync(policyReference)).ReturnsAsync(policy);
            var service = new PolicyService(mockRepo.Object);

            // Act
            await service.RenewPolicyAsync(policyReference);

            // Assert
            Assert.Equal(2, policy.Payments.Count);
            Assert.Equal(100m, policy.Payments[1].Amount);
            Assert.Equal(PaymentMethod.Card, policy.Payments[1].Type);
        }

        [Fact]
        public async Task RenewPolicyAsync_DoesNotCreatePayment_WhenAutoRenewIsFalse()
        {
            // Arrange
            var policyReference = "POL101";
            var policy = new Policy
            {
                PolicyReference = policyReference,
                StartDate = DateTime.Now.AddYears(-1),
                EndDate = DateTime.Now.AddDays(10),
                Amount = 100m,
                AutoRenew = false,
                Payments = new List<Payment>
                {
                    new Payment { PaymentReference = "PAY-1", Amount = 100m, Type = PaymentMethod.Card }
                }
            };
            var mockRepo = new Mock<IPolicyRepository>();
            mockRepo.Setup(r => r.GetPolicyAsync(policyReference)).ReturnsAsync(policy);
            var service = new PolicyService(mockRepo.Object);

            // Act
            await service.RenewPolicyAsync(policyReference);

            // Assert
            Assert.Single(policy.Payments);
        }

        [Fact]
        public async Task RenewPolicyAsync_RejectsChequePayment_WhenAutoRenewIsTrue()
        {
            // Arrange
            var policyReference = "POL202";
            var policy = new Policy
            {
                PolicyReference = policyReference,
                StartDate = DateTime.Now.AddYears(-1),
                EndDate = DateTime.Now.AddDays(10),
                Amount = 100m,
                AutoRenew = true,
                Payments = new List<Payment>
                {
                    new Payment { PaymentReference = "PAY-1", Amount = 100m, Type = PaymentMethod.Cheque }
                }
            };
            var initialPaymentCount = policy.Payments.Count;
            var originalEndDate = policy.EndDate;
            var mockRepo = new Mock<IPolicyRepository>();
            mockRepo.Setup(r => r.GetPolicyAsync(policyReference)).ReturnsAsync(policy);
            var service = new PolicyService(mockRepo.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RenewPolicyAsync(policyReference));

            Assert.Equal("An auto-renewal policy cannot be paid for by cheque.", exception.Message);
            Assert.Equal(initialPaymentCount, policy.Payments.Count);
            Assert.Equal(originalEndDate, policy.EndDate);
            mockRepo.Verify(r => r.UpdatePolicyAsync(It.IsAny<Policy>()), Times.Never);
        }

    }
}