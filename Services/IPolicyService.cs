using UInsureTechnicalTest.DTOs;
using UInsureTechnicalTest.Models;

namespace UInsureTechnicalTest.Services
{
    public interface IPolicyService
    {
        Task<Refund> CancelPolicyAsync(string policyReference);
        Task<Policy?> GetPolicyAsync(string policyReference);
        Task<Policy> RenewPolicyAsync(string policyReference);
        Task<Policy> SellPolicyAsync(SellPolicyRequest request);
        Task<Refund?> CalculateCancellationCostAsync(Policy policy);
    }
}