using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketAPI.Data;
using SupportTicketAPI.Models;

namespace SupportTicketAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly TicketRepository _tickets;
    private readonly HistoryRepository _history;
    private readonly CommentRepository _comments;
    private readonly UserRepository _users;

    public TicketsController(
        TicketRepository tickets,
        HistoryRepository history,
        CommentRepository comments,
        UserRepository users)
    {
        _tickets = tickets;
        _history = history;
        _comments = comments;
        _users = users;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string CurrentRole =>
        User.FindFirstValue(ClaimTypes.Role)!;

    private bool IsAdmin => CurrentRole == "Admin";

    // POST /api/tickets
    [HttpPost]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Subject))
            return BadRequest(ApiResponse<object>.Fail("Subject is required."));
        if (string.IsNullOrWhiteSpace(req.Description))
            return BadRequest(ApiResponse<object>.Fail("Description is required."));
        if (!new[] { "Low", "Medium", "High" }.Contains(req.Priority))
            return BadRequest(ApiResponse<object>.Fail("Priority must be Low, Medium, or High."));

        var ticketNumber = await _tickets.GenerateTicketNumberAsync();
        var ticket = new Ticket
        {
            TicketNumber = ticketNumber,
            Subject = req.Subject.Trim(),
            Description = req.Description.Trim(),
            Priority = req.Priority,
            Status = "Open",
            CreatedByUserId = CurrentUserId
        };

        var newId = await _tickets.CreateTicketAsync(ticket);

        await _history.AddHistoryAsync(new TicketStatusHistory
        {
            TicketId = newId,
            OldStatus = null,
            NewStatus = "Open",
            ChangedByUserId = CurrentUserId,
            Remarks = "Ticket created."
        });

        var created = await _tickets.GetTicketByIdAsync(newId);
        return CreatedAtAction(nameof(GetTicketById), new { id = newId },
            ApiResponse<Ticket>.Ok(created!, "Ticket created successfully."));
    }

    // GET /api/tickets
    [HttpGet]
    public async Task<IActionResult> GetTickets()
    {
        var tickets = IsAdmin
            ? await _tickets.GetAllTicketsAsync()
            : await _tickets.GetTicketsForUserAsync(CurrentUserId);

        return Ok(ApiResponse<IEnumerable<Ticket>>.Ok(tickets));
    }

    // GET /api/tickets/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTicketById(int id)
    {
        var ticket = await _tickets.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound(ApiResponse<object>.Fail("Ticket not found."));

        if (!IsAdmin && ticket.CreatedByUserId != CurrentUserId)
            return Forbid();

        var history = await _history.GetHistoryByTicketAsync(id);
        var comments = await _comments.GetCommentsByTicketAsync(id, IsAdmin);

        var detail = new TicketDetailResponse
        {
            Ticket = ticket,
            History = history.ToList(),
            Comments = comments.ToList()
        };

        return Ok(ApiResponse<TicketDetailResponse>.Ok(detail));
    }

    // PUT /api/tickets/{id}/assign
    [HttpPut("{id:int}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignTicket(int id, [FromBody] AssignTicketRequest req)
    {
        var ticket = await _tickets.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound(ApiResponse<object>.Fail("Ticket not found."));
        if (ticket.Status == "Closed")
            return BadRequest(ApiResponse<object>.Fail("Cannot modify a closed ticket."));

        var admin = await _users.GetByIdAsync(req.AssignedToUserId);
        if (admin == null || admin.Role != "Admin")
            return BadRequest(ApiResponse<object>.Fail("Assigned user must be an Admin."));

        await _tickets.AssignTicketAsync(id, req.AssignedToUserId);

        await _history.AddHistoryAsync(new TicketStatusHistory
        {
            TicketId = id,
            OldStatus = ticket.Status,
            NewStatus = ticket.Status,
            ChangedByUserId = CurrentUserId,
            Remarks = $"Assigned to {admin.FullName}."
        });

        return Ok(ApiResponse<object>.Ok(null!, "Ticket assigned successfully."));
    }

    // PUT /api/tickets/{id}/status
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest req)
    {
        var ticket = await _tickets.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound(ApiResponse<object>.Fail("Ticket not found."));
        if (ticket.Status == "Closed")
            return BadRequest(ApiResponse<object>.Fail("Cannot modify a closed ticket."));

        var validTransitions = new Dictionary<string, string[]>
        {
            { "Open",       new[] { "InProgress" } },
            { "InProgress", new[] { "Closed" } }
        };

        if (!validTransitions.TryGetValue(ticket.Status, out var allowed) ||
            !allowed.Contains(req.NewStatus))
        {
            return BadRequest(ApiResponse<object>.Fail(
                $"Invalid transition: {ticket.Status} → {req.NewStatus}. " +
                $"Allowed: {string.Join(", ", allowed ?? Array.Empty<string>())}"));
        }

        await _tickets.UpdateStatusAsync(id, req.NewStatus);

        await _history.AddHistoryAsync(new TicketStatusHistory
        {
            TicketId = id,
            OldStatus = ticket.Status,
            NewStatus = req.NewStatus,
            ChangedByUserId = CurrentUserId,
            Remarks = req.Remarks ?? $"Status changed to {req.NewStatus}."
        });

        return Ok(ApiResponse<object>.Ok(null!, "Status updated successfully."));
    }

    // POST /api/tickets/{id}/comments
    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.CommentText))
            return BadRequest(ApiResponse<object>.Fail("Comment text is required."));

        var ticket = await _tickets.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound(ApiResponse<object>.Fail("Ticket not found."));
        if (ticket.Status == "Closed")
            return BadRequest(ApiResponse<object>.Fail("Cannot comment on a closed ticket."));

        if (!IsAdmin && ticket.CreatedByUserId != CurrentUserId)
            return Forbid();

        var isInternal = req.IsInternal && IsAdmin;

        await _comments.AddCommentAsync(new TicketComment
        {
            TicketId = id,
            CommentText = req.CommentText.Trim(),
            IsInternal = isInternal,
            CreatedByUserId = CurrentUserId
        });

        return Ok(ApiResponse<object>.Ok(null!, "Comment added successfully."));
    }

    // GET /api/tickets/admins
    [HttpGet("admins")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdmins()
    {
        var admins = await _users.GetAdminUsersAsync();
        return Ok(ApiResponse<IEnumerable<User>>.Ok(admins));
    }
}