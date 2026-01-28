public class AuthService
{
    private readonly ApplicationDbContext _context;

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
    }

    public void Register(User user)
    {
        _context.Users.Add(user);
        _context.SaveChanges();
    }
}
