namespace SupportTicketAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class Ticket
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Description { get; set; } = "";
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public int CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TicketStatusHistory
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = "";
    public int ChangedByUserId { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Remarks { get; set; }
}

public class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string CommentText { get; set; } = "";
    public bool IsInternal { get; set; }
    public int CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public string? CreatedByRole { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class CreateTicketRequest
{
    public string Subject { get; set; } = "";
    public string Description { get; set; } = "";
    public string Priority { get; set; } = "Medium";
}

public class AssignTicketRequest
{
    public int AssignedToUserId { get; set; }
}

public class UpdateStatusRequest
{
    public string NewStatus { get; set; } = "";
    public string? Remarks { get; set; }
}

public class AddCommentRequest
{
    public string CommentText { get; set; } = "";
    public bool IsInternal { get; set; } = false;
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}

public class TicketDetailResponse
{
    public Ticket Ticket { get; set; } = new();
    public List<TicketStatusHistory> History { get; set; } = new();
    public List<TicketComment> Comments { get; set; } = new();
}