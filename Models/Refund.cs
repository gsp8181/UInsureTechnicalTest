namespace UInsureTechnicalTest.Models
{
    public class Refund
    {
        public string RefundReference { get; set; } = Guid.NewGuid().ToString();
        public PaymentMethod Type { get; set; } // Must match original payment method
        public decimal Amount { get; set; }
    }
}