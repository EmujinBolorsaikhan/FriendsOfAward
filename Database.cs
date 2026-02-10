using MySql.Data.MySqlClient;
using System.Data;

public interface IQrCodeService
{
    Task<bool> InsertQrCodeToken(string token);
    Task<List<string>> InsertMultipleQrCodeTokens(List<string> tokens);
    Task<bool> TokenExistsAsync(string token);
    Task<bool> SubmitVotesAsync(VoteSubmission vote);
    Task<bool> CreateAdminAsync(string email, string password);

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
                "SELECT COUNT(*) FROM foa_users WHERE userToken = @token AND qrCodeUsed = 0",
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
    public async Task<bool> SubmitVotesAsync(VoteSubmission vote)
    {
        if (vote.Favorites.Count != 5 || vote.SuperFavorite is null)
            return false;

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1️⃣ get user
            var getUser = new MySqlCommand(
                "SELECT id, qrCodeUsed FROM foa_users WHERE userToken=@token FOR UPDATE",
                connection, (MySqlTransaction)transaction);

            getUser.Parameters.AddWithValue("@token", vote.Token);

            using var reader = await getUser.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return false;

            int userId = reader.GetInt32("id");
            bool used = reader.GetBoolean("qrCodeUsed");

            if (used)
                return false; // already voted

            await reader.CloseAsync();

            // 2️⃣ insert favorites
            foreach (var fav in vote.Favorites)
            {
                var cmd = new MySqlCommand(
                "INSERT INTO foa_votes(userId, projectId, voteType) VALUES(@u,@p,'FAVORIT')",
                connection, (MySqlTransaction)transaction);

                cmd.Parameters.AddWithValue("@u", userId);
                cmd.Parameters.AddWithValue("@p", fav);
                await cmd.ExecuteNonQueryAsync();
            }

            // 3️⃣ insert super favorite
            if (vote.SuperFavorite.HasValue)
            {
                var cmd = new MySqlCommand(
                "INSERT INTO foa_votes(userId, projectId, voteType) VALUES(@u,@p,'SUPER')",
                connection, (MySqlTransaction)transaction);

                cmd.Parameters.AddWithValue("@u", userId);
                cmd.Parameters.AddWithValue("@p", vote.SuperFavorite.Value);
                await cmd.ExecuteNonQueryAsync();
            }

            // 4️⃣ mark token as used
            var update = new MySqlCommand(
                "UPDATE foa_users SET qrCodeUsed=1, usedAt=NOW() WHERE id=@id",
                connection, (MySqlTransaction)transaction);

            update.Parameters.AddWithValue("@id", userId);
            await update.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }
    public async Task<bool> CreateAdminAsync(string email, string password)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Check if admin already exists
            var checkCmd = new MySqlCommand(
                "SELECT COUNT(*) FROM foa_admins WHERE email = @email",
                connection);

            checkCmd.Parameters.AddWithValue("@email", email);

            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

            if (exists > 0)
                return false;

            // Insert admin
            var cmd = new MySqlCommand(
                "INSERT INTO foa_admins (email, aPassword) VALUES (@email, @password)",
                connection);

            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@password", password);

            var result = await cmd.ExecuteNonQueryAsync();

            return result > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Admin creation error: " + ex.Message);
            return false;
        }
    }

}