using System.ComponentModel.DataAnnotations;

namespace PatientApi.Models;

public class Patient
{
    [Key]
    public Guid Id { get; set; }
    public Gender Gender { get; set; }
    [Required]
    public DateTime BirthDate { get; set; }
    public bool Active { get; set; }

    public PatientName Name { get; set; } = null!;
}
