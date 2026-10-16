namespace UInsureTechnicalTest.DTOs
{
    public record PolicyholderDto(
        string FirstName, 
        string LastName, 
        DateTime DateOfBirth
        );
}