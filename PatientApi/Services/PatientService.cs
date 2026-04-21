using Microsoft.EntityFrameworkCore;
using PatientApi.Data;
using PatientApi.DTOs;
using PatientApi.Models;

namespace PatientApi.Services;

public class PatientService : IPatientService
{
    private readonly PatientApiDbContext _db;

    public PatientService(PatientApiDbContext db)
    {
        _db = db;
    }

    public async Task<PatientResponseDto?> GetByIdAsync(Guid id)
    {
        var patient = await _db.Patients
            .Include(p => p.Name)
            .FirstOrDefaultAsync(p => p.Id == id);

        return patient == null ? null : MapToResponse(patient);
    }

    public async Task<List<PatientResponseDto>> SearchByBirthDateAsync(string birthDateParam)
    {
        var query = _db.Patients.Include(p => p.Name).AsQueryable();
        if (!string.IsNullOrWhiteSpace(birthDateParam))
        {
            var param = FhirDateSearchParser.Parse(birthDateParam);
            if (param != null)
            {
                query = FhirDateSearchParser.ApplyFilter(query, param);
            }
        }
        
        var patients = await query.ToListAsync();
        return patients.Select(MapToResponse).ToList();
    }

    public async Task<PatientResponseDto> CreateAsync(PatientCreateRequestDto dto)
    {
        var id = dto.Name.Id != null && Guid.TryParse(dto.Name.Id, out var parsedId)
            ? parsedId
            : Guid.NewGuid();

        var patient = new Patient
        {
            Id = id,
            Gender = dto.Gender,
            BirthDate = dto.BirthDate!.Value,
            Active = dto.Active,
            Name = new PatientName
            {
                Id = id,   // same GUID as PK+FK
                Use = dto.Name.Use,
                Family = dto.Name.Family,
                Given = dto.Name.Given
            }
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        return MapToResponse(patient);
    }

    public async Task<PatientResponseDto?> UpdateAsync(Guid id, PatientUpdateRequestDto dto)
    {
        var patient = await _db.Patients
            .Include(p => p.Name)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null)
        {
            return null;
        }

        patient.Gender = dto.Gender;
        patient.BirthDate = dto.BirthDate!.Value;
        patient.Active = dto.Active;
        patient.Name.Use = dto.Name.Use;
        patient.Name.Family = dto.Name.Family;
        patient.Name.Given = dto.Name.Given;

        await _db.SaveChangesAsync();
        return MapToResponse(patient);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient == null)
        {
            return false;
        }

        _db.Patients.Remove(patient);
        await _db.SaveChangesAsync();
        return true;
    }

    private static PatientResponseDto MapToResponse(Patient patient) => new()
    {
        Name = new PatientNameDto
        {
            Id = patient.Id.ToString(),
            Use = patient.Name.Use,
            Family = patient.Name.Family,
            Given = patient.Name.Given
        },
        Gender = patient.Gender.ToString(),
        BirthDate = patient.BirthDate,
        Active = patient.Active
    };
}
