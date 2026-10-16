using Xunit;
using Moq;
using UInsureTechnicalTest.DTOs;
using UInsureTechnicalTest.Models;
using UInsureTechnicalTest.Services;

namespace UInsureTechnicalTest.Tests
{
    public class CancelPolicyTests
    {
        // Cancellation before the policy start date results in a full refund
        [Fact]
        public async Task CancelPolicy_BeforeStartDate_ReturnsFullRefund()
        {
            // Arrange
            var policyReference = "POL123";
            var policy = new Policy
            {
                PolicyReference = policyReference,
                StartDate = DateTime.UtcNow.AddDays(1), // Start date is in the future
                EndDate = DateTime.UtcNow.AddYears(1),
                Amount = 100m,
                Payments = new List<Payment>
                {
                    new Payment { PaymentReference = "abc123",Amount = 100m, PaymentDate = DateTime.UtcNow.AddDays(-1), Type = PaymentMethod.Card }
                },
                IsCancelled = false
            };

            // Arrange
            var mockRepo = new Mock<IPolicyRepository>();

            mockRepo.Setup(r => r.GetPolicyAsync(policyReference))
                    .ReturnsAsync(policy);

            var service = new PolicyService(mockRepo.Object);

            // Act
            var refund = await service.CancelPolicyAsync(policyReference);

            // Assert
            Assert.NotNull(refund);
            Assert.Equal(100m, refund.Amount); // Full refund
            Assert.Equal(PaymentMethod.Card, refund.Type); // Refund type matches original payment
            Assert.True(policy.IsCancelled);
            Assert.NotNull(policy.CancellationDate);
        }

        // Cancellation on or after the policy start date and within the 14 day cooling off period results in a full refund
        [Fact]
        public async Task CancelPolicy_WithinCoolingOffPeriod_ReturnsFullRefund()
        {
            // Arrange
            var policyReference = "POL456";
            var policy = new Policy
            {
                PolicyReference = policyReference,
                StartDate = DateTime.UtcNow.AddDays(-5), // Start date is in the past but within 14 days
                EndDate = DateTime.UtcNow.AddYears(1),
                Amount = 200m,
                Payments = new List<Payment>
                {
                    new Payment { PaymentReference = "def456", Amount = 200m, PaymentDate = DateTime.UtcNow.AddDays(-6), Type = PaymentMethod.Card }
                },
                IsCancelled = false
            };
            // Arrange
            var mockRepo = new Mock<IPolicyRepository>();

            mockRepo.Setup(r => r.GetPolicyAsync(policyReference))
                    .ReturnsAsync(policy);

            var service = new PolicyService(mockRepo.Object);

            // Act
            var refund = await service.CancelPolicyAsync(policyReference);

            // Assert
            Assert.NotNull(refund);
            Assert.Equal(200m, refund.Amount); // Full refund
            Assert.Equal(PaymentMethod.Card, refund.Type); // Refund type matches original payment
            Assert.True(policy.IsCancelled);
            Assert.NotNull(policy.CancellationDate);
        }

        // Cancellation after the 14 day cooling off period results in a pro rata refund based on the number of days of insurance not used

        [Fact]
        public async Task CancelPolicy_AfterCoolingOffPeriod_ReturnsProRataRefund()
        {
            // Arrange
            var policyReference = "POL789";
            var policy = new Policy
            {
                PolicyReference = policyReference,
                StartDate = DateTime.UtcNow.AddDays(-20), // Start date is in the past and beyond the 14 day cooling off period
                EndDate = DateTime.UtcNow.AddDays(345), // 1 year policy
                Amount = 365m, // For simplicity, assume $1 per day
                Payments = new List<Payment>
                {
                    new Payment { PaymentReference = "ghi789", Amount = 365m, PaymentDate = DateTime.UtcNow.AddDays(-21), Type = PaymentMethod.Card }
                },
                IsCancelled = false
            };

            // Arrange
            var mockRepo = new Mock<IPolicyRepository>();

            mockRepo.Setup(r => r.GetPolicyAsync(policyReference))
                    .ReturnsAsync(policy);

            var service = new PolicyService(mockRepo.Object);

            // Act
            var refund = await service.CancelPolicyAsync(policyReference);

            // Assert
            Assert.NotNull(refund);
            var expectedProRataAmount = 345m;
            Assert.Equal(expectedProRataAmount, refund.Amount); // Pro rata refund
            Assert.Equal(PaymentMethod.Card, refund.Type); // Refund type matches original payment
            Assert.True(policy.IsCancelled);
            Assert.NotNull(policy.CancellationDate);
        }

        // The refund can only be made to the same method as the original payment
        [Fact]
        public async Task CancelPolicy_RefundTypeMatchesOriginalPayment()
        {
            // Arrange
            var policyReference = "POL101";
            var policy = new Policy
            {
                PolicyReference = policyReference,
                StartDate = DateTime.UtcNow.AddDays(-5), // Start date is in the past but within 14 days
                EndDate = DateTime.UtcNow.AddYears(1),
                Amount = 150m,
                Payments = new List<Payment>
                {
                    new Payment { PaymentReference = "jkl101", Amount = 150m, PaymentDate = DateTime.UtcNow.AddDays(-6), Type = PaymentMethod.Card }
                },
                IsCancelled = false
            };

            // Arrange
            var mockRepo = new Mock<IPolicyRepository>();

            mockRepo.Setup(r => r.GetPolicyAsync(policyReference))
                    .ReturnsAsync(policy);

            var service = new PolicyService(mockRepo.Object);

            // Act
            var refund = await service.CancelPolicyAsync(policyReference);

            // Assert
            Assert.NotNull(refund);
            Assert.Equal(150m, refund.Amount); // Full refund
            Assert.Equal(PaymentMethod.Card, refund.Type); // Refund type matches original payment
            Assert.True(policy.IsCancelled);
            Assert.NotNull(policy.CancellationDate);
        }

        // Not issue a refund if the policy has had a claim made against it
        [Fact]
        public async Task CancelPolicy_WithClaims_ReturnsNullRefund()
        {
            // Arrange
            var policyReference = "POL202";
            var policy = new Policy
            {
                PolicyReference = policyReference,
                StartDate = DateTime.UtcNow.AddDays(-5), // Start date is in the past but within 14 days
                EndDate = DateTime.UtcNow.AddYears(1),
                Amount = 200m,
                HasClaims = true, // Policy has claims
                Payments = new List<Payment>
                {
                    new Payment { PaymentReference = "mno202", Amount = 200m, PaymentDate = DateTime.UtcNow.AddDays(-6), Type = PaymentMethod.Card }
                },
                IsCancelled = false
            };

            // Arrange
            var mockRepo = new Mock<IPolicyRepository>();
            mockRepo.Setup(r => r.GetPolicyAsync(policyReference))
                    .ReturnsAsync(policy);

            var service = new PolicyService(mockRepo.Object);

            // Act
            var refund = await service.CancelPolicyAsync(policyReference);

            // Assert
            Assert.Null(refund); // No refund should be issued
            Assert.True(policy.IsCancelled);
            Assert.NotNull(policy.CancellationDate);
        }
    }
}