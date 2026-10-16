using Microsoft.Data.SqlClient;

namespace Backend.Services
{
    public class MagangOtpService
    {
        private readonly string _connectionString;

        public MagangOtpService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                                ?? "Server=DESKTOP-166VE2J\\SQLEXPRESS;Database=magang;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        public async Task<string?> GetOrGenerateBarcodeAsync(string eventName, string sessionNm)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                int parsedSessionNm = int.TryParse(sessionNm, out var s) ? s : 1;

                string insertQuery = @"
                    INSERT INTO magang_token_log (sessionnm, event, niklog, id_user, expired) 
                    OUTPUT INSERTED.id, INSERTED.sessionnm, INSERTED.event, INSERTED.niklog, INSERTED.id_user, INSERTED.expired 
                    VALUES (@sessionnm, @event, NULL, 1, DATEADD(minute, 5, GETDATE()))";

                long generatedId = 0;
                string dbEvent = eventName;
                string dbNiklog = "-";
                string dbSessionNm = sessionNm;
                DateTime dbExpired = DateTime.Now;

                using (SqlCommand cmdInsert = new SqlCommand(insertQuery, connection))
                {
                    cmdInsert.Parameters.AddWithValue("@sessionnm", parsedSessionNm);
                    cmdInsert.Parameters.AddWithValue("@event", eventName);

                    using (SqlDataReader reader = await cmdInsert.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            generatedId = Convert.ToInt64(reader["id"]);
                            dbSessionNm = reader["sessionnm"].ToString()!;
                            dbEvent = reader["event"].ToString()!;
                            dbNiklog = "-";
                            dbExpired = Convert.ToDateTime(reader["expired"]);
                        }
                    }
                }

                return $"{generatedId};{dbEvent};{dbNiklog};{dbSessionNm};{dbExpired:yyyy-MM-dd HH:mm:ss}";
            }
        }

        public async Task<bool> VerifyAndClaimOtpAsync(long id, string niklog, string sessionNm)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                int parsedSessionNm = int.TryParse(sessionNm, out var s) ? s : 1;

                string updateQuery = @"
                    UPDATE magang_token_log 
                    SET niklog = @niklog 
                    WHERE id = @id 
                      AND sessionnm = @sessionnm 
                      AND expired > GETDATE() 
                      AND (niklog IS NULL OR niklog = '-')";

                using (SqlCommand command = new SqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@niklog", niklog);
                    command.Parameters.AddWithValue("@sessionnm", parsedSessionNm);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
    }
}