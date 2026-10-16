namespace UInsureTechnicalTest.Models
{
    public class Payment
    {
        public required string PaymentReference { get; set; }
        public PaymentMethod Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; internal set; }
    }
}