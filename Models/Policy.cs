namespace UInsureTechnicalTest.Models
{
    public class Policy
    {
        public required string PolicyReference { get; set; }
        public PolicyType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; } // Must be exactly 1 year after StartDate
        public decimal Amount { get; set; }
        public bool HasClaims { get; set; }
        public bool AutoRenew { get; set; }
        public bool IsCancelled { get; set; }
        public DateTime? CancellationDate { get; set; }

        public List<Policyholder> Policyholders { get; set; } = new();
        public Property Property { get; set; } = null!;
        public List<Payment> Payments { get; set; } = new();
        public Refund? Refund { get; set; }
    }
}