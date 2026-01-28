var builder = WebApplication.CreateBuilder(args);

// 🔹 1. Controllers add
builder.Services.AddControllers();

// 🔹 2. Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔹 3. (Later) DbContext yahin add hoga
// builder.Services.AddDbContext<ApplicationDbContext>();

var app = builder.Build();

// 🔹 4. Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔹 5. HTTPS
app.UseHttpsRedirection();

// 🔹 6. Authorization (later use hoga)
app.UseAuthorization();

// 🔹 7. Map controllers
app.MapControllers();

// 🔹 8. Run app
app.Run();
