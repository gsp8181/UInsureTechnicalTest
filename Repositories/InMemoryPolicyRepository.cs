using UInsureTechnicalTest.Models;

public class InMemoryPolicyRepository : IPolicyRepository
{
    private readonly Dictionary<string, Policy> _policies = new();

    public Task<Policy?> GetPolicyAsync(string policyReference)
    {
        _policies.TryGetValue(policyReference, out var policy);
        return Task.FromResult(policy);
    }

    public Task AddPolicyAsync(Policy policy)
    {
        _policies[policy.PolicyReference] = policy;
        return Task.CompletedTask;
    }

    public Task UpdatePolicyAsync(Policy policy)
    {
        _policies[policy.PolicyReference] = policy;
        return Task.CompletedTask;
    }
}