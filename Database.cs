using MySql.Data.MySqlClient;

public interface IQrCodeService
{
    Task<bool> InsertQrCodeToken(string token);
    Task<List<string>> InsertMultipleQrCodeTokens(List<string> tokens);
    Task<bool> TokenExistsAsync(string token);
}


public class QrCodeService : IQrCodeService
{
    private readonly string _connectionString;

    public QrCodeService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<bool> InsertQrCodeToken(string token)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new MySqlCommand(
                "INSERT INTO foa_users (userToken, qrCodeUsed) VALUES (@token, 0)",
                connection);

            command.Parameters.AddWithValue("@token", token);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database error: {ex.Message}");
            return false;
        }
    }

    public async Task<List<string>> InsertMultipleQrCodeTokens(List<string> tokens)
    {
        var insertedTokens = new List<string>();

        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                foreach (var token in tokens)
                {
                    var command = new MySqlCommand(
                        "INSERT INTO foa_users (userToken, qrCodeUsed) VALUES (@token, 0)",
                        connection,
                        transaction);

                    command.Parameters.AddWithValue("@token", token);

                    var result = await command.ExecuteNonQueryAsync();
                    if (result > 0)
                    {
                        insertedTokens.Add(token);
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database error: {ex.Message}");
        }

        return insertedTokens;
    }
    public async Task<bool> TokenExistsAsync(string token)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new MySqlCommand(
                "SELECT COUNT(*) FROM foa_users WHERE userToken = @token",
                connection);

            command.Parameters.AddWithValue("@token", token);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database error: {ex.Message}");
            return false;
        }
    }

}