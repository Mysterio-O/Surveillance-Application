namespace SurveilWin.Api.Data.Entities;

public enum UserRole { SuperAdmin, OrgAdmin, Manager, Employee }

public class User
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? InvitedBy { get; set; }
    public string? InviteToken { get; set; }
    public DateTime? InviteExpires { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}
