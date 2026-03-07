namespace SurveilWin.Api.Data.Entities;

public class ManagerAssignment
{
    public Guid ManagerId { get; set; }
    public User Manager { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public User Employee { get; set; } = null!;
    public DateTime AssignedAt { get; set; }
}
