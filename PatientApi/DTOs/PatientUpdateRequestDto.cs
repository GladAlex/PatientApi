using PatientApi.Models;
using System.ComponentModel.DataAnnotations;

namespace PatientApi.DTOs
{
    public class PatientUpdateRequestDto
    {
        [Required]
        public PatientNameDto Name { get; set; } = null!;

        public Gender Gender { get; set; } = Gender.unknown;

        [Required(ErrorMessage = "Birth date is required")]
        public DateTime? BirthDate { get; set; }

        public bool Active { get; set; }
    }
}
