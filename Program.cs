using Backend.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Tambahkan layanan Controller dan Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Backend v1",
        Version = "v1"
    });
});

// Daftarkan MagangOtpService ke Dependency Injection
builder.Services.AddScoped<MagangOtpService>();

var app = builder.Build();

// Konfigurasi Swagger untuk Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.SerializeAsV2 = true;
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Backend v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Memetakan Controller API secara otomatis
app.MapControllers();

app.Run();