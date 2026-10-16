using UInsureTechnicalTest.Models;

namespace UInsureTechnicalTest.DTOs
{
    public record SellPolicyRequest(
        string? PolicyReference,
        string Type, // "Household" or "BuyToLet"
        DateTime StartDate,
        decimal Amount,
        bool AutoRenew,
        bool HasClaims, // You typically wouldn't sell a policy already with claims, but the spec states 'A policy can have three actions which a consumer can carry out, sell/cancel/renew'. I would have added a claims model but there would have been no way to populate it without a claims action.
        List<PolicyholderDto> Policyholders,
        PropertyDto Property,
        PaymentDto InitialPayment
    );
}