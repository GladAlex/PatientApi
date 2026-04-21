using Microsoft.AspNetCore.Mvc;
using PatientApi.DTOs;
using PatientApi.Services;

namespace PatientApi.Controllers;

/// <summary>
/// CRUD operations for Patient entities (newborns)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PatientController : ControllerBase
{
    private readonly IPatientService _service;
    private readonly ILogger<PatientController> _logger;

    public PatientController(IPatientService service, ILogger<PatientController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get a patient by ID
    /// </summary>
    /// <param name="id">Patient GUID</param>
    /// <returns>Patient data</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var patient = await _service.GetByIdAsync(id);
        
        return patient == null ? NotFound(new { message = $"Patient {id} not found" }) : Ok(patient);
    }

    /// <summary>
    /// Search patients by birth date using FHIR date search parameters.
    /// Supported prefixes: eq, ne, lt, gt, le, ge, sa, eb, ap.
    /// Supported date formats: YYYY, YYYY-MM, YYYY-MM-DD, YYYY-MM-DDThh:mm:ss.
    /// Examples: birthDate=2026-01-13, birthDate=gt2026-01-01, birthDate=le2025-12-31
    /// </summary>
    /// <param name="birthDate">FHIR date search string (e.g. "eq2026-01-13", "gt2026-01-01", "2026")</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<PatientResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string birthDate)
    {
        var results = await _service.SearchByBirthDateAsync(birthDate);
        return Ok(results);
    }

    /// <summary>
    /// Create a new patient
    /// </summary>
    /// <param name="dto">Patient data. Required: name.family, birthDate</param>
    [HttpPost]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] PatientCreateRequestDto dto)
    {
        if (!ModelState.IsValid)
        { 
            return BadRequest(ModelState);
        }

        var created = await _service.CreateAsync(dto);
        var id = Guid.Parse(created.Name.Id!);
        return CreatedAtAction(nameof(GetById), new { id }, created);
    }

    /// <summary>
    /// Update an existing patient
    /// </summary>
    /// <param name="id">Patient GUID</param>
    /// <param name="dto">Updated patient data</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] PatientUpdateRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _service.UpdateAsync(id, dto);
        
        return (updated == null) ? NotFound(new { message = $"Patient {id} not found" }) : Ok(updated);
    }

    /// <summary>
    /// Delete a patient by ID
    /// </summary>
    /// <param name="id">Patient GUID</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        
        return deleted ? NoContent() : NotFound(new { message = $"Patient {id} not found" });
    }
}
