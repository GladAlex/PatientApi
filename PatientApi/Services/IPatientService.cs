using PatientApi.DTOs;

namespace PatientApi.Services
{
    public interface IPatientService
    {
        Task<PatientResponseDto?> GetByIdAsync(Guid id);
        Task<List<PatientResponseDto>> SearchByBirthDateAsync(string birthDate);
        Task<PatientResponseDto> CreateAsync(PatientCreateRequestDto dto);
        Task<PatientResponseDto?> UpdateAsync(Guid id, PatientUpdateRequestDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
