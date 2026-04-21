using System.ComponentModel.DataAnnotations;

namespace PatientApi.DTOs
{
    public class PatientNameDto
    {
        public string? Id { get; set; }
        public string Use { get; set; } = string.Empty;

        [Required(ErrorMessage = "Family is required")]
        public string Family { get; set; } = string.Empty;

        public List<string> Given { get; set; } = new();
    }
}
