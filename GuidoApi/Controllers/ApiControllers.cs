using System.Security.Claims;
using GoGuido.Data;
using GoGuido.Models;
using GoGuido.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoGuido.Controllers;

public abstract class ApiBase : ControllerBase
{
    protected int Uid => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected string RoleName => User.FindFirstValue(ClaimTypes.Role) ?? "";
    protected IActionResult Fail(string msg, int code = 400) => StatusCode(code, new { message = msg });
}

// ───────────── AUTH ─────────────
[ApiController, Route("api/v1/auth")]
public class AuthController(AppDbContext db, IPasswordHasher<User> hasher, TokenService tokens) : ApiBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto d)
    {
        if (d.Role != Roles.Tourist && d.Role != Roles.Guide) return Fail("Role must be Tourist or Guide.");
        if (string.IsNullOrWhiteSpace(d.FullName) || string.IsNullOrWhiteSpace(d.Email) || !d.Email.Contains('@'))
            return Fail("Valid name and email are required.");
        if ((d.Password?.Length ?? 0) < 6) return Fail("Password must be at least 6 characters.");
        var email = d.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email)) return Fail("Email already registered.", 409);

        var user = new User { FullName = d.FullName.Trim(), Email = email, Phone = d.Phone, Role = d.Role };
        user.PasswordHash = hasher.HashPassword(user, d.Password!);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Ok(await tokens.IssueAsync(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto d)
    {
        var email = (d.Email ?? "").Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, d.Password ?? "") == PasswordVerificationResult.Failed)
            return Fail("Invalid email or password.", 401);
        return Ok(await tokens.IssueAsync(user));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshDto d)
    {
        var rt = await db.RefreshTokens.Include(r => r.User).FirstOrDefaultAsync(r => r.Token == d.RefreshToken);
        if (rt is null || rt.Revoked || rt.ExpiresAt < DateTime.UtcNow) return Fail("Invalid refresh token.", 401);
        rt.Revoked = true;
        await db.SaveChangesAsync();
        return Ok(await tokens.IssueAsync(rt.User));
    }

    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me()
    {
        var u = await db.Users.FindAsync(Uid);
        return u is null ? Fail("Not found", 404) : Ok(new UserDto(u.Id, u.FullName, u.Email, u.Role));
    }
}

// ───────────── CITIES & MONUMENTS ─────────────
[ApiController, Route("api/v1/cities")]
public class CitiesController(AppDbContext db) : ApiBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.Cities.AsNoTracking().OrderBy(c => c.Name)
        .Select(c => new { c.Id, c.Name, c.State, c.Description, c.Icon, c.Hue, Monuments = c.Monuments.Count }).ToListAsync());
}

[ApiController, Route("api/v1/monuments")]
public class MonumentsController(AppDbContext db) : ApiBase
{
    [HttpGet]
    public async Task<IActionResult> List(int? cityId, string? q, bool? unesco)
    {
        var query = db.Monuments.AsNoTracking().AsQueryable();
        if (cityId > 0) query = query.Where(m => m.CityId == cityId);
        if (unesco == true) query = query.Where(m => m.IsUnesco);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(m => m.Name.Contains(q) || m.Description.Contains(q));
        return Ok(await query.OrderBy(m => m.City.Name).ThenBy(m => m.Name).Select(m => new
        {
            m.Id, m.CityId, CityName = m.City.Name, CityHue = m.City.Hue, m.Name, m.Era, m.Description, m.Icon, m.EntryFeeInr, m.IsUnesco
        }).ToListAsync());
    }
}

// ───────────── GUIDES ─────────────
[ApiController, Route("api/v1/guides")]
public class GuidesController(AppDbContext db) : ApiBase
{
    private static IQueryable<GuideDto> Project(IQueryable<GuideProfile> q) => q.Select(g => new GuideDto(
        g.Id, g.User.FullName, g.CityId, g.City.Name, g.Bio, g.Languages, g.ExperienceYears, g.PricePerHour,
        g.Reviews.Average(r => (double?)r.Rating) ?? 0, g.Reviews.Count, g.Status));

    // Only APPROVED guides are ever visible to tourists (hard rule at API layer)
    [HttpGet]
    public async Task<IActionResult> Search(int? cityId, string? language, string sort = "rating")
    {
        var q = db.GuideProfiles.AsNoTracking().Where(g => g.Status == St.Approved);
        if (cityId > 0) q = q.Where(g => g.CityId == cityId);
        var list = await Project(q).ToListAsync();
        if (!string.IsNullOrWhiteSpace(language))
            list = list.Where(g => g.Languages.Contains(language, StringComparison.OrdinalIgnoreCase)).ToList();
        list = sort switch
        {
            "price" => list.OrderBy(g => g.PricePerHour).ToList(),
            "experience" => list.OrderByDescending(g => g.ExperienceYears).ToList(),
            _ => list.OrderByDescending(g => g.Rating).ThenByDescending(g => g.ReviewCount).ToList()
        };
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var g = await Project(db.GuideProfiles.AsNoTracking().Where(x => x.Id == id && x.Status == St.Approved)).FirstOrDefaultAsync();
        if (g is null) return Fail("Guide not found.", 404);
        var reviews = await db.Reviews.AsNoTracking().Where(r => r.GuideProfileId == id)
            .OrderByDescending(r => r.CreatedAt).Take(10)
            .Select(r => new { r.Rating, r.Comment, r.CreatedAt, Tourist = r.Tourist.FullName }).ToListAsync();
        return Ok(new { guide = g, reviews });
    }

    [HttpGet("me"), Authorize(Roles = Roles.Guide)]
    public async Task<IActionResult> Mine()
    {
        var p = await db.GuideProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == Uid);
        return p is null ? Ok(null) : Ok(new { p.CityId, p.Bio, p.Languages, p.ExperienceYears, p.PricePerHour, p.LicenseNumber, p.Status, p.RejectReason });
    }

    [HttpPut("me"), Authorize(Roles = Roles.Guide)]
    public async Task<IActionResult> Upsert(GuideProfileDto d)
    {
        if (!await db.Cities.AnyAsync(c => c.Id == d.CityId)) return Fail("Choose a valid city.");
        if (d.PricePerHour <= 0 || d.ExperienceYears < 0) return Fail("Enter a valid price and experience.");
        if (string.IsNullOrWhiteSpace(d.LicenseNumber)) return Fail("Tourist-guide licence number is required for KYC.");
        var p = await db.GuideProfiles.FirstOrDefaultAsync(x => x.UserId == Uid);
        if (p is null) { p = new GuideProfile { UserId = Uid }; db.GuideProfiles.Add(p); }
        if (p.LicenseNumber != d.LicenseNumber) { p.Status = St.Pending; p.RejectReason = null; } // re-verify on licence change
        p.CityId = d.CityId; p.Bio = d.Bio ?? ""; p.Languages = d.Languages ?? "English";
        p.ExperienceYears = d.ExperienceYears; p.PricePerHour = d.PricePerHour; p.LicenseNumber = d.LicenseNumber.Trim();
        await db.SaveChangesAsync();
        return Ok(new { p.Status });
    }
}

// ───────────── BOOKINGS (state machine) ─────────────
[ApiController, Route("api/v1/bookings"), Authorize]
public class BookingsController(AppDbContext db) : ApiBase
{
    [HttpPost, Authorize(Roles = Roles.Tourist)]
    public async Task<IActionResult> Create(CreateBookingDto d)
    {
        var g = await db.GuideProfiles.FirstOrDefaultAsync(x => x.Id == d.GuideId && x.Status == St.Approved);
        if (g is null) return Fail("Guide is not available.", 404);
        if (d.Hours is < 1 or > 12) return Fail("Duration must be 1–12 hours.");
        if (d.TourDate.ToUniversalTime() < DateTime.UtcNow.AddHours(-1)) return Fail("Tour date must be in the future.");
        var b = new Booking
        {
            TouristId = Uid, GuideProfileId = g.Id, MonumentId = d.MonumentId, TourDate = d.TourDate.ToUniversalTime(),
            Hours = d.Hours, TotalPrice = g.PricePerHour * d.Hours, Notes = d.Notes, Status = St.Pending
        };
        db.Bookings.Add(b);
        await db.SaveChangesAsync();
        return Ok(new { b.Id, b.Status, b.TotalPrice });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var q = db.Bookings.AsNoTracking().AsQueryable();
        q = RoleName == Roles.Guide ? q.Where(b => b.GuideProfile.UserId == Uid) : q.Where(b => b.TouristId == Uid);
        var list = await q.OrderByDescending(b => b.CreatedAt).Select(b => new BookingDto(
            b.Id, b.GuideProfile.User.FullName, b.Tourist.FullName, b.GuideProfile.City.Name,
            b.Monument != null ? b.Monument.Name : null, b.TourDate, b.Hours, b.TotalPrice, b.Status, b.Notes,
            db.Reviews.Any(r => r.BookingId == b.Id))).ToListAsync();
        return Ok(list);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, StatusDto d)
    {
        var b = await db.Bookings.Include(x => x.GuideProfile).FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return Fail("Booking not found.", 404);
        var isGuide = RoleName == Roles.Guide && b.GuideProfile.UserId == Uid;
        var isTourist = RoleName == Roles.Tourist && b.TouristId == Uid;
        if (!isGuide && !isTourist) return Fail("Not allowed.", 403);

        bool ok = (isGuide, b.Status, d.Status) switch
        {
            (true, St.Pending, St.Accepted) => true,
            (true, St.Pending, St.Declined) => true,
            (true, St.Accepted, St.InProgress) => true,
            (true, St.InProgress, St.Completed) => true,
            (_, _, _) when isTourist && d.Status == St.Cancelled && (b.Status == St.Pending || b.Status == St.Accepted) => true,
            _ => false
        };
        if (!ok) return Fail($"Cannot change booking from {b.Status} to {d.Status}.");
        b.Status = d.Status;
        await db.SaveChangesAsync();
        return Ok(new { b.Id, b.Status });
    }
}

// ───────────── REVIEWS ─────────────
[ApiController, Route("api/v1/reviews"), Authorize(Roles = Roles.Tourist)]
public class ReviewsController(AppDbContext db) : ApiBase
{
    [HttpPost]
    public async Task<IActionResult> Create(ReviewDto d)
    {
        if (d.Rating is < 1 or > 5) return Fail("Rating must be 1–5.");
        var b = await db.Bookings.FirstOrDefaultAsync(x => x.Id == d.BookingId && x.TouristId == Uid);
        if (b is null) return Fail("Booking not found.", 404);
        if (b.Status != St.Completed) return Fail("You can rate only completed trips.");
        if (await db.Reviews.AnyAsync(r => r.BookingId == b.Id)) return Fail("Already reviewed.", 409);
        db.Reviews.Add(new Review { BookingId = b.Id, GuideProfileId = b.GuideProfileId, TouristId = Uid, Rating = d.Rating, Comment = d.Comment });
        await db.SaveChangesAsync();
        return Ok(new { message = "Thanks for your review!" });
    }
}

// ───────────── STATS (ADO.NET) ─────────────
[ApiController, Route("api/v1/stats")]
public class StatsController(AdoRepository ado) : ApiBase
{
    [HttpGet("public")] public async Task<IActionResult> Public() => Ok(await ado.PublicStatsAsync());
    [HttpGet("admin"), Authorize(Roles = Roles.Admin)] public async Task<IActionResult> Admin() => Ok(await ado.AdminStatsAsync());
}

// ───────────── ADMIN (KYC verification + catalog) ─────────────
[ApiController, Route("api/v1/admin"), Authorize(Roles = Roles.Admin)]
public class AdminController(AppDbContext db) : ApiBase
{
    [HttpGet("guides")]
    public async Task<IActionResult> Guides(string? status) => Ok(await db.GuideProfiles.AsNoTracking()
        .Where(g => string.IsNullOrEmpty(status) || g.Status == status)
        .OrderBy(g => g.Status).ThenBy(g => g.Id)
        .Select(g => new { g.Id, Name = g.User.FullName, g.User.Email, City = g.City.Name, g.LicenseNumber, g.ExperienceYears, g.PricePerHour, g.Languages, g.Bio, g.Status, g.RejectReason })
        .ToListAsync());

    [HttpPut("guides/{id:int}/verify")]
    public async Task<IActionResult> Verify(int id, VerifyDto d)
    {
        var g = await db.GuideProfiles.FindAsync(id);
        if (g is null) return Fail("Guide not found.", 404);
        if (!d.Approve && string.IsNullOrWhiteSpace(d.Reason)) return Fail("A rejection reason is required.");
        g.Status = d.Approve ? St.Approved : St.Rejected;
        g.RejectReason = d.Approve ? null : d.Reason;
        await db.SaveChangesAsync();
        return Ok(new { g.Id, g.Status });
    }

    [HttpPost("cities")]
    public async Task<IActionResult> AddCity(CityDto d)
    {
        var c = new City { Name = d.Name, State = d.State, Description = d.Description, Icon = d.Icon, Hue = d.Hue };
        db.Cities.Add(c); await db.SaveChangesAsync(); return Ok(new { c.Id });
    }

    [HttpPost("monuments")]
    public async Task<IActionResult> AddMonument(MonumentDto d)
    {
        if (!await db.Cities.AnyAsync(c => c.Id == d.CityId)) return Fail("Invalid city.");
        var m = new Monument { CityId = d.CityId, Name = d.Name, Era = d.Era, Description = d.Description, Icon = d.Icon, EntryFeeInr = d.EntryFeeInr, IsUnesco = d.IsUnesco };
        db.Monuments.Add(m); await db.SaveChangesAsync(); return Ok(new { m.Id });
    }
}
