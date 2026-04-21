namespace PatientApi.DTOs
{
    public class PatientResponseDto
    {
        public PatientNameDto Name { get; set; } = null!;
        public string Gender { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public bool Active { get; set; }
    }
}
