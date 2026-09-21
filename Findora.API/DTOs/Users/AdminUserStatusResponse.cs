using Findora.API.Models;

namespace Findora.API.DTOs.Users;

/// <summary>Confirmation response for a successful <c>PATCH /api/v1/users/{id}/status</c>.</summary>
public class AdminUserStatusResponse
{
    public Guid Id { get; set; }
    public UserStatus Status { get; set; }
}
