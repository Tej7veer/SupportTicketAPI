using Dapper;
using MySql.Data.MySqlClient;
using SupportTicketAPI.Models;

namespace SupportTicketAPI.Data;

public class DatabaseContext
{
    private readonly string _connectionString;
    public DatabaseContext(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }
    public MySqlConnection GetConnection() => new(_connectionString);
}

public class UserRepository
{
    private readonly DatabaseContext _db;
    public UserRepository(DatabaseContext db) => _db = db;

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Username = @Username AND IsActive = 1",
            new { Username = username });
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<User>> GetAdminUsersAsync()
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<User>(
            "SELECT Id, Username, FullName, Email, Role FROM Users WHERE Role = 'Admin' AND IsActive = 1");
    }
}

public class TicketRepository
{
    private readonly DatabaseContext _db;
    public TicketRepository(DatabaseContext db) => _db = db;

    public async Task<int> CreateTicketAsync(Ticket ticket)
    {
        using var conn = _db.GetConnection();
        var sql = @"INSERT INTO Tickets 
            (TicketNumber, Subject, Description, Priority, Status, CreatedByUserId, CreatedAt, UpdatedAt)
            VALUES (@TicketNumber, @Subject, @Description, @Priority, @Status, @CreatedByUserId, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await conn.ExecuteScalarAsync<int>(sql, ticket);
    }

    public async Task<string> GenerateTicketNumberAsync()
    {
        using var conn = _db.GetConnection();
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Tickets");
        return $"TKT-{DateTime.UtcNow:yyyyMM}-{(count + 1):D4}";
    }

    public async Task<IEnumerable<Ticket>> GetTicketsForUserAsync(int userId)
    {
        using var conn = _db.GetConnection();
        var sql = @"SELECT t.*, u1.FullName AS CreatedByName, u2.FullName AS AssignedToName
            FROM Tickets t
            INNER JOIN Users u1 ON t.CreatedByUserId = u1.Id
            LEFT  JOIN Users u2 ON t.AssignedToUserId = u2.Id
            WHERE t.CreatedByUserId = @UserId
            ORDER BY t.CreatedAt DESC";
        return await conn.QueryAsync<Ticket>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<Ticket>> GetAllTicketsAsync()
    {
        using var conn = _db.GetConnection();
        var sql = @"SELECT t.*, u1.FullName AS CreatedByName, u2.FullName AS AssignedToName
            FROM Tickets t
            INNER JOIN Users u1 ON t.CreatedByUserId = u1.Id
            LEFT  JOIN Users u2 ON t.AssignedToUserId = u2.Id
            ORDER BY t.CreatedAt DESC";
        return await conn.QueryAsync<Ticket>(sql);
    }

    public async Task<Ticket?> GetTicketByIdAsync(int id)
    {
        using var conn = _db.GetConnection();
        var sql = @"SELECT t.*, u1.FullName AS CreatedByName, u2.FullName AS AssignedToName
            FROM Tickets t
            INNER JOIN Users u1 ON t.CreatedByUserId = u1.Id
            LEFT  JOIN Users u2 ON t.AssignedToUserId = u2.Id
            WHERE t.Id = @Id";
        return await conn.QueryFirstOrDefaultAsync<Ticket>(sql, new { Id = id });
    }

    public async Task AssignTicketAsync(int ticketId, int adminUserId)
    {
        using var conn = _db.GetConnection();
        await conn.ExecuteAsync(
            "UPDATE Tickets SET AssignedToUserId = @AdminUserId, UpdatedAt = UTC_TIMESTAMP() WHERE Id = @TicketId",
            new { AdminUserId = adminUserId, TicketId = ticketId });
    }

    public async Task UpdateStatusAsync(int ticketId, string newStatus)
    {
        using var conn = _db.GetConnection();
        await conn.ExecuteAsync(
            "UPDATE Tickets SET Status = @Status, UpdatedAt = UTC_TIMESTAMP() WHERE Id = @TicketId",
            new { Status = newStatus, TicketId = ticketId });
    }
}

public class HistoryRepository
{
    private readonly DatabaseContext _db;
    public HistoryRepository(DatabaseContext db) => _db = db;

    public async Task AddHistoryAsync(TicketStatusHistory entry)
    {
        using var conn = _db.GetConnection();
        var sql = @"INSERT INTO TicketStatusHistory 
            (TicketId, OldStatus, NewStatus, ChangedByUserId, ChangedAt, Remarks)
            VALUES (@TicketId, @OldStatus, @NewStatus, @ChangedByUserId, UTC_TIMESTAMP(), @Remarks)";
        await conn.ExecuteAsync(sql, entry);
    }

    public async Task<IEnumerable<TicketStatusHistory>> GetHistoryByTicketAsync(int ticketId)
    {
        using var conn = _db.GetConnection();
        var sql = @"SELECT h.*, u.FullName AS ChangedByName
            FROM TicketStatusHistory h
            INNER JOIN Users u ON h.ChangedByUserId = u.Id
            WHERE h.TicketId = @TicketId
            ORDER BY h.ChangedAt DESC";
        return await conn.QueryAsync<TicketStatusHistory>(sql, new { TicketId = ticketId });
    }
}

public class CommentRepository
{
    private readonly DatabaseContext _db;
    public CommentRepository(DatabaseContext db) => _db = db;

    public async Task AddCommentAsync(TicketComment comment)
    {
        using var conn = _db.GetConnection();
        var sql = @"INSERT INTO TicketComments 
            (TicketId, CommentText, IsInternal, CreatedByUserId, CreatedAt)
            VALUES (@TicketId, @CommentText, @IsInternal, @CreatedByUserId, UTC_TIMESTAMP())";
        await conn.ExecuteAsync(sql, comment);
    }

    public async Task<IEnumerable<TicketComment>> GetCommentsByTicketAsync(int ticketId, bool isAdmin)
    {
        using var conn = _db.GetConnection();
        var sql = @"SELECT c.*, u.FullName AS CreatedByName, u.Role AS CreatedByRole
            FROM TicketComments c
            INNER JOIN Users u ON c.CreatedByUserId = u.Id
            WHERE c.TicketId = @TicketId"
            + (isAdmin ? "" : " AND c.IsInternal = 0")
            + " ORDER BY c.CreatedAt ASC";
        return await conn.QueryAsync<TicketComment>(sql, new { TicketId = ticketId });
    }
}