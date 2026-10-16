using Xunit;
using Moq;
using UInsureTechnicalTest.DTOs;
using UInsureTechnicalTest.Models;
using UInsureTechnicalTest.Services;

namespace UInsureTechnicalTest.Tests
{
    public class IssuePolicyTests
    {
        // Policy Reference must be unique
        [Fact]
        public async Task SellPolicyAsync_ThrowsArgumentException_WhenPolicyReferenceAlreadyExists()
        {
            // Arrange
            var mockRepo = new Mock<IPolicyRepository>();

            // Arrange: Tell the mock repo to return an existing policy when "existing-ref" is looked up
            mockRepo.Setup(r => r.GetPolicyAsync("existing-ref"))
                    .ReturnsAsync(new Policy { PolicyReference = "existing-ref" });

            var service = new PolicyService(mockRepo.Object);

            // Create a request that uses the duplicate reference
            var request = new SellPolicyRequest(
                PolicyReference: "existing-ref",
                Type: "Household",
                StartDate: DateTime.UtcNow.AddDays(10),
                Amount: 100m,
                AutoRenew: true,
                HasClaims: false,
                Policyholders: new List<PolicyholderDto> { new("John", "Doe", new DateTime(1990, 1, 1)) },
                Property: new PropertyDto("123 Street", null, null, "M1 1AE"),
                InitialPayment: new PaymentDto("PAY-1", "Card")
            );

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.SellPolicyAsync(request));

            Assert.Equal("Policy Reference must be unique.", exception.Message);
        }

        // A policy can only be sold up to 60 days in advance
        [Fact]
        public async Task SellPolicyAsync_ThrowsArgumentException_WhenStartDateIsMoreThan60Days()
        {
            // Arrange
            var mockRepo = new Mock<IPolicyRepository>();
            var service = new PolicyService(mockRepo.Object);

            // Create a request with a start date more than 60 days in the future
            var request = new SellPolicyRequest(
                PolicyReference: "unique-ref",
                Type: "Household",
                StartDate: DateTime.UtcNow.AddDays(61), // More than 60 days
                Amount: 100m,
                AutoRenew: true,
                HasClaims: false,
                Policyholders: new List<PolicyholderDto> { new("John", "Doe", new DateTime(1990, 1, 1)) },
                Property: new PropertyDto("123 Street", null, null, "M1 1AE"),
                InitialPayment: new PaymentDto("PAY-1", "Card")
            );

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.SellPolicyAsync(request));

            Assert.Equal("A policy can only be sold up to 60 days in advance.", exception.Message);
        }

        // A policy must always be 1 year in length
        [Fact]
        public async Task SellPolicyAsync_ThrowsArgumentException_WhenPolicyLengthIsNotOneYear()
        {
            // Arrange
            var mockRepo = new Mock<IPolicyRepository>();
            var service = new PolicyService(mockRepo.Object);

            // Create a request with a policy length that is not one year
            var request = new SellPolicyRequest(
                PolicyReference: "unique-ref",
                Type: "Household",
                StartDate: DateTime.UtcNow.AddDays(10),
                Amount: 100m,
                AutoRenew: true,
                HasClaims: false,
                Policyholders: new List<PolicyholderDto> { new("John", "Doe", new DateTime(1990, 1, 1)) },
                Property: new PropertyDto("123 Street", null, null, "M1 1AE"),
                InitialPayment: new PaymentDto("PAY-1", "Card")
            );

            // Act & Assert that date is exactly 1 year
            var policy = await service.SellPolicyAsync(request);
            Assert.Equal(policy.StartDate.AddYears(1).Date, policy.EndDate.Date);
        }

        // A policy must have at least 1 but no more than 3 policyholders
        [Fact]
        public async Task SellPolicyAsync_ThrowsArgumentException_WhenPolicyholdersCountIsInvalid()
        {
            // Arrange
            var mockRepo = new Mock<IPolicyRepository>();
            var service = new PolicyService(mockRepo.Object);

            // Create a request with an invalid number of policyholders
            var request = new SellPolicyRequest(
                PolicyReference: "unique-ref",
                Type: "Household",
                StartDate: DateTime.UtcNow.AddDays(10),
                Amount: 100m,
                AutoRenew: true,
                HasClaims: false,
                Policyholders: new List<PolicyholderDto> { new("John", "Doe", new DateTime(1990, 1, 1)), new("Jane", "Doe", new DateTime(1990, 1, 1)), new("Bob", "Doe", new DateTime(1990, 1, 1)), new("Alice", "Doe", new DateTime(1990, 1, 1)) },
                Property: new PropertyDto("123 Street", null, null, "M1 1AE"),
                InitialPayment: new PaymentDto("PAY-1", "Card")
            );

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.SellPolicyAsync(request));

            Assert.Equal("A policy must have at least 1 but no more than 3 policyholders.", exception.Message);

            // Test for less than 1 policyholder
            request = new SellPolicyRequest(
                PolicyReference: "unique-ref",
                Type: "Household",
                StartDate: DateTime.UtcNow.AddDays(10),
                Amount: 100m,
                AutoRenew: true,
                HasClaims: false,
                Policyholders: new List<PolicyholderDto>(), // No policyholders
                Property: new PropertyDto("123 Street", null, null, "M1 1AE"),
                InitialPayment: new PaymentDto("PAY-1", "Card")
            );

            // Act & Assert
            var exception2 = await Assert.ThrowsAsync<ArgumentException>(() => service.SellPolicyAsync(request));

            Assert.Equal("A policy must have at least 1 but no more than 3 policyholders.", exception2.Message);
        }

        // All policyholders must be over 16 years old on the start date of the policy
        [Fact]
        public async Task SellPolicyAsync_ThrowsArgumentException_WhenPolicyholderIsUnder16()
        {
            // Arrange
            var mockRepo = new Mock<IPolicyRepository>();
            var service = new PolicyService(mockRepo.Object);

            // Create a request with a policyholder under 16
            var request = new SellPolicyRequest(
                PolicyReference: "unique-ref",
                Type: "Household",
                StartDate: DateTime.UtcNow.AddDays(10),
                Amount: 100m,
                AutoRenew: true,
                HasClaims: false,
                Policyholders: new List<PolicyholderDto> { new("John", "Doe",
    new DateTime(DateTime.UtcNow.Year - 15, 1, 1)) }, // 15 years old
                Property: new PropertyDto("123 Street", null, null, "M1 1AE"),
                InitialPayment: new PaymentDto("PAY-1", "Card")
            );

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.SellPolicyAsync(request));

            Assert.Equal("All policyholders must be over 16 years old on the start date of the policy.", exception.Message);
        }
    }
}