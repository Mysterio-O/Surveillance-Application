namespace SurveilWin.Api.DTOs.Users;

public class InviteUserRequest
{
    public string Email { get; set; } = "";
    public string Role { get; set; } = "Employee";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public Guid? ManagerId { get; set; }
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public Guid OrganizationId { get; set; }
    public string? OrgName { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AssignManagerRequest
{
    public Guid ManagerId { get; set; }
    public List<Guid> EmployeeIds { get; set; } = new();
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = "";
}
