namespace UInsureTechnicalTest.Models
{
    public class Property
    {
        public required string AddressLine1 { get; set; } // Required
        public string? AddressLine2 { get; set; }
        public string? AddressLine3 { get; set; }
        public required string Postcode { get; set; } // Required, max length 8
    }
}