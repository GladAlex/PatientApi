using System.ComponentModel.DataAnnotations;

namespace PatientApi.Models
{
    public class PatientName
    {
        [Key]
        public Guid Id { get; set; }
        public string Use { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public List<string> Given { get; set; } = new();

        public Patient Patient { get; set; } = null!;
    }
}
