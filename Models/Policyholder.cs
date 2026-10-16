namespace UInsureTechnicalTest.Models
{
    public class Policyholder
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public DateTime DateOfBirth { get; set; } // Must be > 16 years old on Policy StartDate
    }
}