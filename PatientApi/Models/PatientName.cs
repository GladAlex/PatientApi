using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PatientApi.Models
{
    [Owned]
    public class PatientName
    {
        public string Use { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public List<string> Given { get; set; } = new();
    }
}
