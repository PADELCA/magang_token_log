using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MagangOtpController : ControllerBase
    {
        private readonly string _connectionString;

        public MagangOtpController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                                ?? "Server=DESKTOP-166VE2J\\SQLEXPRESS;Database=magang;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        public class VerifyRequestDto
        {
            public int SessionNm { get; set; }
            public string Niklog { get; set; }
            public int IdUser { get; set; }
        }

        [HttpGet("barcode")]
        public async Task<IActionResult> GetBarcode([FromQuery] string eventName, [FromQuery] int idUser)
        {
            if (string.IsNullOrEmpty(eventName) || idUser <= 0)
            {
                return BadRequest(new { message = "Parameter eventName dan idUser wajib diisi!" });
            }

            try
            {
                int generatedSessionNm = 0;
                DateTime dbExpired = DateTime.Now;

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string insertQuery = @"
                        INSERT INTO magang_token_log (event, niklog, id_user, expired) 
                        OUTPUT INSERTED.sessionnm, INSERTED.expired 
                        VALUES (@event, NULL, @idUser, DATEADD(minute, 5, GETDATE()))";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@event", eventName);
                        cmd.Parameters.AddWithValue("@idUser", idUser);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                generatedSessionNm = Convert.ToInt32(reader["sessionnm"]);
                                dbExpired = Convert.ToDateTime(reader["expired"]);
                            }
                        }
                    }
                }

                string formatBarcode = $"{generatedSessionNm};{eventName};-;{idUser};{dbExpired:yyyy-MM-dd HH:mm:ss}";

                return Ok(new { 
                    status = true, 
                    sessionnm = generatedSessionNm, 
                    id_user = idUser,
                    format_barcode = formatBarcode 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] VerifyRequestDto request)
        {
            if (request.SessionNm <= 0 || string.IsNullOrEmpty(request.Niklog) || request.IdUser <= 0)
            {
                return BadRequest(new { message = "SessionNm, Niklog, dan IdUser wajib diisi!" });
            }

            try
            {
                int rowsAffected = 0;

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string updateQuery = @"
                        UPDATE magang_token_log 
                        SET niklog = @niklog 
                        WHERE sessionnm = @sessionnm 
                          AND id_user = @id_user 
                          AND expired > GETDATE()";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@sessionnm", request.SessionNm);
                        command.Parameters.AddWithValue("@niklog", request.Niklog);
                        command.Parameters.AddWithValue("@id_user", request.IdUser);

                        rowsAffected = await command.ExecuteNonQueryAsync();
                    }
                }

                if (rowsAffected > 0)
                {
                    return Ok(new { 
                        status = true, 
                        sessionnm = request.SessionNm,
                        niklog = request.Niklog,
                        id_user = request.IdUser,
                        message = $"Verifikasi berhasil: Sesi {request.SessionNm} untuk User {request.IdUser} berhasil di-update!" 
                    });
                }
                else
                {
                    return BadRequest(new { 
                        status = false, 
                        message = "Gagal update: Kemungkinan SessionNm tidak ditemukan, IdUser tidak cocok, atau token sudah kedaluwarsa (> 5 menit)." 
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}