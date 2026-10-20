using Microsoft.Data.SqlClient;

namespace GoGuido.Services;

/// <summary>Pure ADO.NET data access (SqlConnection/SqlCommand/SqlDataReader) for fast read-only reporting.</summary>
public class AdoRepository(IConfiguration cfg)
{
    private readonly string _cs = cfg.GetConnectionString("Default")!;

    public async Task<Dictionary<string, int>> PublicStatsAsync()
    {
        const string sql = @"SELECT
            (SELECT COUNT(*) FROM Cities)                                   AS Cities,
            (SELECT COUNT(*) FROM Monuments)                                AS Monuments,
            (SELECT COUNT(*) FROM GuideProfiles WHERE Status = @approved)   AS Guides,
            (SELECT COUNT(*) FROM Bookings WHERE Status = @completed)       AS Trips";
        await using var con = new SqlConnection(_cs);
        await con.OpenAsync();
        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@approved", "Approved");
        cmd.Parameters.AddWithValue("@completed", "Completed");
        await using var r = await cmd.ExecuteReaderAsync();
        var d = new Dictionary<string, int>();
        if (await r.ReadAsync())
            for (int i = 0; i < r.FieldCount; i++) d[r.GetName(i).ToLowerInvariant()] = r.GetInt32(i);
        return d;
    }

    public async Task<object> AdminStatsAsync()
    {
        const string sql = @"
            SELECT Role, COUNT(*) FROM Users GROUP BY Role;
            SELECT Status, COUNT(*) FROM Bookings GROUP BY Status;
            SELECT ISNULL(SUM(TotalPrice),0) FROM Bookings WHERE Status = 'Completed';
            SELECT COUNT(*) FROM GuideProfiles WHERE Status = 'Pending';";
        await using var con = new SqlConnection(_cs);
        await con.OpenAsync();
        await using var cmd = new SqlCommand(sql, con);
        await using var r = await cmd.ExecuteReaderAsync();
        var users = new Dictionary<string, int>();
        var bookings = new Dictionary<string, int>();
        while (await r.ReadAsync()) users[r.GetString(0)] = r.GetInt32(1);
        await r.NextResultAsync();
        while (await r.ReadAsync()) bookings[r.GetString(0)] = r.GetInt32(1);
        await r.NextResultAsync();
        decimal revenue = 0; if (await r.ReadAsync()) revenue = r.GetDecimal(0);
        await r.NextResultAsync();
        int pending = 0; if (await r.ReadAsync()) pending = r.GetInt32(0);
        return new { users, bookings, revenue, pendingGuides = pending, commission = Math.Round(revenue * 0.10m, 2) };
    }
}
