using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AppointmentsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetConnectionString()
        => _configuration.GetConnectionString("DefaultConnection");
    
    [HttpGet]
    public async Task<IActionResult> GetAppointments([FromQuery] string? status, [FromQuery] string? patientLastName)
    {
        var result = new List<AppointmentListDto>();

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();

        var query = @"
            SELECT
                a.IdAppointment,
                a.AppointmentDate,
                a.Status,
                a.Reason,
                p.FirstName + ' ' + p.LastName AS PatientFullName,
                p.Email AS PatientEmail
            FROM dbo.Appointments a
            JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
            WHERE (@Status IS NULL OR a.Status = @Status)
              AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
            ORDER BY a.AppointmentDate;
        ";

        await using var command = new SqlCommand(query, connection);

        command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value =
            (object?)status ?? DBNull.Value;

        command.Parameters.Add("@PatientLastName", SqlDbType.NVarChar, 80).Value =
            (object?)patientLastName ?? DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5)
            });
        }

        return Ok(result);
    }
}