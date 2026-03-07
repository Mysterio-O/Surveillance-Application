using SurveilWin.Api.Data;

namespace SurveilWin.Api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    public UserService(AppDbContext db) { _db = db; }
}
