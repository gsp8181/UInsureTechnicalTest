using Microsoft.AspNetCore.Mvc;
using UInsureTechnicalTest.DTOs;
using UInsureTechnicalTest.Models;

namespace UInsureTechnicalTest.Services
{
    public class PolicyService : IPolicyService
    {
        private readonly IPolicyRepository policyRepository;

        public PolicyService(IPolicyRepository policyRepository)
        {
            this.policyRepository = policyRepository;
        }

        public async Task<Refund?> CalculateCancellationCostAsync(Policy policy)
        {
            // Not issue a refund if the policy has had a claim made against it
            if (policy.HasClaims)
            {
                return null;
            }

            var previousRefund = policy.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();

            //If there is no previous payments on file, we cant issue a refund

            if (previousRefund == null)
            {
                return null;
            }

            // Prevent an auto renewal policy renewal being paid for by cheque
            if (policy.AutoRenew && previousRefund.Type == PaymentMethod.Cheque)
            {
                return null;
            }

            //1. The refund amount changes based on the cancellation date
            // a. Cancellation before the policy start date results in a full refund
            // b. Cancellation on or after the policy start date and within the 14 day cooling off period
            // results in a full refund
            if (DateTime.UtcNow < policy.StartDate || (DateTime.UtcNow >= policy.StartDate && DateTime.UtcNow <= policy.StartDate.AddDays(14)))
            {
                return new Refund
                {
                    Amount = policy.Amount,
                    Type = previousRefund.Type
                };
            }

            // c. Cancellation after the 14 day cooling off period results in a pro rata refund based on
            // the number of days of insurance not used
            // 2. The refund can only be made to the same method as the original payment
            var totalDays = (policy.EndDate - policy.StartDate).TotalDays;
            var usedDays = (DateTime.UtcNow - policy.StartDate).TotalDays;
            var proRataAmount = decimal.Round(policy.Amount * (decimal)((totalDays - usedDays) / totalDays), 2, MidpointRounding.AwayFromZero);

            return new Refund
            {
                Amount = proRataAmount,
                Type = previousRefund.Type
            };

        }

        public async Task<Refund?> CancelPolicyAsync(string policyReference)
        {
            var policy = await policyRepository.GetPolicyAsync(policyReference);
            if (policy == null)
            {
                throw new InvalidOperationException("Policy not found.");
            }

            if (policy.IsCancelled)
            {
                throw new InvalidOperationException("Policy is already cancelled.");
            }

            var refund = await CalculateCancellationCostAsync(policy);

            policy.IsCancelled = true;
            policy.CancellationDate = DateTime.UtcNow;

            policy.Refund = refund;

            await policyRepository.UpdatePolicyAsync(policy);

            return refund;
        }

        public async Task<Policy?> GetPolicyAsync(string policyReference)
        {
            return await policyRepository.GetPolicyAsync(policyReference);
        }

        public async Task<Policy> RenewPolicyAsync(string policyReference)
        {
            var policy = await policyRepository.GetPolicyAsync(policyReference);

            if (policy == null)
            {
                throw new InvalidOperationException("Policy not found.");
            }

            // 1. A policy can only be renewed 30 days before the end date of the current policy
            if (DateTime.Now < policy.EndDate.AddDays(-30))
            {
                throw new InvalidOperationException("A policy can only be renewed 30 days before the end date of the current policy.");
            }

            // 2. A policy cannot be renewed after the end date

            if (DateTime.Now > policy.EndDate)
            {
                throw new InvalidOperationException("A policy cannot be renewed after the end date.");
            }

            // 3. When creating a renewal against a policy
            // a. which is set to auto renew true a payment is created
            // b. which is set to auto renew false a payment is not created

            if (policy.AutoRenew)
            {
                var lastPaymentMethod = policy.Payments.OrderByDescending(o => o.PaymentDate).FirstOrDefault()?.Type ?? PaymentMethod.Card; // Default to Card if no payments exist

                if (lastPaymentMethod == PaymentMethod.Cheque)
                {
                    throw new InvalidOperationException("An auto-renewal policy cannot be paid for by cheque.");
                }

                var payment = new Payment
                {
                    // Assuming GUIDs dont collide
                    PaymentReference = Guid.NewGuid().ToString(),
                    Amount = policy.Amount,
                    Type = lastPaymentMethod, // Assuming card for simplicity, could be extended to use the original payment method
                    PaymentDate = DateTime.UtcNow
                };
                policy.Payments.Add(payment);
            }

            policy.EndDate = policy.EndDate.AddYears(1); // Assume paying for an extra year gives you a full extra year on the end date and not a single year from that date like the passport office
            policy.HasClaims = false; // If you are renewing the policy, set claims to false purely because it's new

            await policyRepository.UpdatePolicyAsync(policy);

            return policy;

        }

        public async Task<Policy> SellPolicyAsync(SellPolicyRequest request)
        {
            // 2. A policy can only be sold up to 60 days in advance

            if (request.StartDate > DateTime.Now.AddDays(60))
            {
                throw new ArgumentException("A policy can only be sold up to 60 days in advance.");
            }

            // 4. A policy must have at least 1 but no more than 3 policyholders

            if (request.Policyholders.Count < 1 || request.Policyholders.Count > 3)
            {
                throw new ArgumentException("A policy must have at least 1 but no more than 3 policyholders.");
            }

            // 5. All policyholders must be over 16 years old on the start date of the policy

            if (request.Policyholders.Any(ph => ph.DateOfBirth.AddYears(16) > request.StartDate))
            {
                throw new ArgumentException("All policyholders must be over 16 years old on the start date of the policy.");
            }

            PolicyType policyType = request.Type switch
            {
                "Household" => PolicyType.Household,
                "BuyToLet" => PolicyType.BuyToLet,
                _ => throw new ArgumentException("Invalid policy type. Must be 'Household' or 'BuyToLet'.")
            };

            PaymentMethod paymentMethod = request.InitialPayment.Type switch
            {
                "Card" => PaymentMethod.Card,
                "DirectDebit" => PaymentMethod.DirectDebit,
                "Cheque" => PaymentMethod.Cheque,
                _ => throw new ArgumentException("Invalid payment method. Must be 'Card', 'DirectDebit', or 'Cheque'.")
            };

            var policyRef = request.PolicyReference;
            // Policy Reference is not shown in the spec to be user provided, so i'm gonna assume we could use a GUID
            if (string.IsNullOrEmpty(policyRef))
            {
                policyRef = Guid.NewGuid().ToString();
            }

            var paymentRef = request.InitialPayment.PaymentReference;
            // Same for Payment References
            if (string.IsNullOrEmpty(paymentRef))
            {
                paymentRef = Guid.NewGuid().ToString();
            }

            var policy = new Policy
            {
                PolicyReference = policyRef,
                Type = policyType,
                StartDate = request.StartDate,
                // 3. A policy must always be 1 year in length
                EndDate = request.StartDate.AddYears(1),
                Amount = request.Amount,
                AutoRenew = request.AutoRenew,
                HasClaims = request.HasClaims,
                Policyholders = request.Policyholders.Select(ph => new Policyholder
                {
                    FirstName = ph.FirstName,
                    LastName = ph.LastName,
                    DateOfBirth = ph.DateOfBirth
                }).ToList(),
                Property = new Property
                {
                    AddressLine1 = request.Property.AddressLine1,
                    AddressLine2 = request.Property.AddressLine2,
                    AddressLine3 = request.Property.AddressLine3,
                    Postcode = request.Property.Postcode
                },
                Payments = new List<Payment>
                {
                    new Payment
                    {
                        PaymentReference = paymentRef,
                        //Assuming every policy is paid off in full at the start and not on a 12 month plan
                        Amount = request.Amount,
                        Type = paymentMethod,
                        PaymentDate = DateTime.UtcNow
                    }
                }
            };

            // 1. Policy Reference must be unique

            if (await policyRepository.GetPolicyAsync(policyRef) != null)
            {
                throw new ArgumentException("Policy Reference must be unique.");
            }

            await policyRepository.AddPolicyAsync(policy);

            return policy;
        }
    }
}