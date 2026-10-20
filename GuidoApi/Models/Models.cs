namespace GoGuido.Models;

public static class Roles { public const string Tourist = "Tourist", Guide = "Guide", Admin = "Admin"; }
public static class St
{
    public const string Pending = "Pending", Approved = "Approved", Rejected = "Rejected",
        Accepted = "Accepted", Declined = "Declined", InProgress = "InProgress",
        Completed = "Completed", Cancelled = "Cancelled";
}

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = Roles.Tourist;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public GuideProfile? GuideProfile { get; set; }
}

public class City
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string State { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "🏛️";
    public int Hue { get; set; } = 25;
    public List<Monument> Monuments { get; set; } = new();
}

public class Monument
{
    public int Id { get; set; }
    public int CityId { get; set; }
    public City City { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Era { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "🕌";
    public decimal EntryFeeInr { get; set; }
    public bool IsUnesco { get; set; }
}

public class GuideProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CityId { get; set; }
    public City City { get; set; } = null!;
    public string Bio { get; set; } = "";
    public string Languages { get; set; } = "English,Hindi";
    public int ExperienceYears { get; set; }
    public decimal PricePerHour { get; set; }
    public string LicenseNumber { get; set; } = "";
    public string Status { get; set; } = St.Pending;
    public string? RejectReason { get; set; }
    public List<Review> Reviews { get; set; } = new();
}

public class Booking
{
    public int Id { get; set; }
    public int TouristId { get; set; }
    public User Tourist { get; set; } = null!;
    public int GuideProfileId { get; set; }
    public GuideProfile GuideProfile { get; set; } = null!;
    public int? MonumentId { get; set; }
    public Monument? Monument { get; set; }
    public DateTime TourDate { get; set; }
    public int Hours { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = St.Pending;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Review
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public int GuideProfileId { get; set; }
    public GuideProfile GuideProfile { get; set; } = null!;
    public int TouristId { get; set; }
    public User Tourist { get; set; } = null!;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; }
}

// ---------- DTOs ----------
public record RegisterDto(string FullName, string Email, string Password, string? Phone, string Role);
public record LoginDto(string Email, string Password);
public record RefreshDto(string RefreshToken);
public record UserDto(int Id, string FullName, string Email, string Role);
public record AuthResponse(string AccessToken, string RefreshToken, UserDto User);
public record GuideDto(int Id, string Name, int CityId, string CityName, string Bio, string Languages,
    int ExperienceYears, decimal PricePerHour, double Rating, int ReviewCount, string Status);
public record GuideProfileDto(int CityId, string Bio, string Languages, int ExperienceYears, decimal PricePerHour, string LicenseNumber);
public record CreateBookingDto(int GuideId, int? MonumentId, DateTime TourDate, int Hours, string? Notes);
public record StatusDto(string Status);
public record BookingDto(int Id, string GuideName, string TouristName, string CityName, string? MonumentName,
    DateTime TourDate, int Hours, decimal TotalPrice, string Status, string? Notes, bool Reviewed);
public record ReviewDto(int BookingId, int Rating, string? Comment);
public record VerifyDto(bool Approve, string? Reason);
public record CityDto(string Name, string State, string Description, string Icon, int Hue);
public record MonumentDto(int CityId, string Name, string Era, string Description, string Icon, decimal EntryFeeInr, bool IsUnesco);
