using System.ComponentModel.DataAnnotations;

namespace UInsureTechnicalTest.DTOs
{
    public record PropertyDto(
        [Required(ErrorMessage = "Address Line 1 is required.")]
        string AddressLine1,

        string? AddressLine2,

        string? AddressLine3,

        [Required(ErrorMessage = "Postcode is required.")]
        [StringLength(8, ErrorMessage = "Postcode cannot be longer than 8 characters.")]
        string Postcode
    );
}