# GoGuido — ASP.NET Core 8 · EF Core · ADO.NET · SQL Server

Location-aware platform to hire **verified local guides** for India's monuments
(Taj Mahal, Agra Fort, Fatehpur Sikri, Red Fort, Qutub Minar, Amber Fort, Hawa Mahal, Golden Temple, …).
Built from your SRS + Implementation Guide (Phases 0–5 and 7, 11-lite).

## Run (3 steps)
```bash
docker compose up -d          # SQL Server 2022 on localhost:1433   (or use your own SQL Server)
dotnet run                    # creates DB "GoGuidoDb" + seeds 8 cities, 34 monuments, 11 guides
# open http://localhost:5080   (Swagger: http://localhost:5080/swagger)
```
Using an existing SQL Server instead? Edit `ConnectionStrings:Default` in `appsettings.json`, e.g.
`Server=(localdb)\\MSSQLLocalDB;Database=GoGuidoDb;Trusted_Connection=True;`

## Demo logins
| Role | Email | Password |
|---|---|---|
| Tourist | tourist@goguido.in | Tourist@123 |
| Guide | rajesh@goguido.in | Guide@123 |
| Admin | admin@goguido.in | Admin@123 |

## Architecture (modular monolith, per the SRS)
```
Controllers/ApiControllers.cs   REST API  /api/v1/{auth,cities,monuments,guides,bookings,reviews,stats,admin}
Models/Models.cs                EF Core entities + DTO records
Data/AppDbContext.cs            EF Core context (SQL Server, restrict-delete, precision)
Data/SeedData.cs                Cities, monuments, guides, demo users
Services/TokenService.cs        JWT access token (60 min) + rotating refresh tokens
Services/AdoRepository.cs       Pure ADO.NET (SqlConnection/SqlCommand/SqlDataReader) – stats/reporting
wwwroot/                        Website (HTML/CSS/JS) served by the same app
```
**Auth:** PBKDF2 password hashing (ASP.NET Identity `PasswordHasher`), JWT Bearer, roles `Tourist | Guide | Admin`.
**Rules enforced at API layer:** unverified guides never appear in search; booking state machine
`Pending → Accepted/Declined → InProgress → Completed` (tourist may cancel Pending/Accepted);
reviews only for the tourist's own completed trips, one per trip; licence change triggers re-verification.

## Key endpoints
| Method | Route | Who |
|---|---|---|
| POST | /api/v1/auth/register · login · refresh | public |
| GET | /api/v1/cities · /monuments?cityId=&q=&unesco= | public |
| GET | /api/v1/guides?cityId=&language=&sort=rating\|price\|experience | public (approved only) |
| PUT/GET | /api/v1/guides/me | Guide (KYC submit) |
| POST/GET | /api/v1/bookings · /bookings/mine · PUT /bookings/{id}/status | Tourist/Guide |
| POST | /api/v1/reviews | Tourist |
| GET | /api/v1/stats/public (ADO.NET) · /stats/admin (ADO.NET) | public / Admin |
| GET/PUT/POST | /api/v1/admin/guides · /guides/{id}/verify · /cities · /monuments | Admin |

## Production notes
* Change `Jwt:Key` and the SA password; store secrets in Key Vault / user-secrets.
* The app uses `EnsureCreated()` for instant setup. For versioned schema changes switch to migrations:
  `dotnet ef migrations add Init && dotnet ef database update` (replace `EnsureCreated` with `Migrate()`).
* Next phases from your guide (not included yet): SignalR live tracking, payments, notifications, KYC document upload.
* Entry fees are approximate (Indian citizens) and should be verified before launch.
