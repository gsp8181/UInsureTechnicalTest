
using UInsureTechnicalTest.Models;

public interface IPolicyRepository
{
    Task<Policy?> GetPolicyAsync(string policyReference);
    Task AddPolicyAsync(Policy policy);
    Task UpdatePolicyAsync(Policy policy);
}