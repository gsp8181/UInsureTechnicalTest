using UInsureTechnicalTest.Models;

namespace UInsureTechnicalTest.DTOs
{
    public record PaymentDto(
        string? PaymentReference, 
        string Type
        //Assuming everyone pays off their policy in one initial payment (even though monthly policies exist)
        );
}