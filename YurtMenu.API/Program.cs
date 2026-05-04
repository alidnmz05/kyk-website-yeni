using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using YurtMenu.API.Data;
using YurtMenu.API.Helpers;

var builder = WebApplication.CreateBuilder(args);

// ==================== Firestore DI ====================
var projectId = builder.Configuration["GoogleCloud:ProjectId"];
var credPath = builder.Configuration["GoogleCloud:CredentialsPath"];

if (string.IsNullOrWhiteSpace(projectId))
    throw new InvalidOperationException("GoogleCloud:ProjectId is missing.");

if (string.IsNullOrWhiteSpace(credPath) || !File.Exists(credPath))
    throw new FileNotFoundException($"Firebase credential not found at: {credPath}");

GoogleCredential credential = GoogleCredential.FromFile(credPath);
var firestoreClient = new FirestoreClientBuilder { Credential = credential }.Build();
var firestoreDb = FirestoreDb.Create(projectId, firestoreClient);
builder.Services.AddSingleton(firestoreDb);

// Firebase Admin SDK (Push Notifications için) - EKLE
if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions()
    {
        Credential = credential,
        ProjectId = projectId
    });
}

// ==================== Services ====================
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
    ));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==================== CORS (Default Policy) ====================
// Frontend origin'lerini buraya ekleyin. HTTPS'e geçince https://... olanları da eklemeyi unutmayın.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                "http://31.57.156.217:3000",
                "http://localhost:3000",
                "http://localhost:3001",
                "http://31.57.156.217" // (opsiyonel) 80 portundan yayın varsa
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});

// ==================== JWT Auth ====================
var jwtSection = builder.Configuration.GetSection("Jwt");

var jwtIssuer = jwtSection["Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is missing");
var jwtAudience = jwtSection["Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is missing");
var jwtKeyString = jwtSection["Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKeyString));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = key,
        ClockSkew = TimeSpan.Zero
    };
});

var app = builder.Build();

// ==================== Warmups ====================
using (var scope = app.Services.CreateScope())
{
    var dbCtx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    StaticFoodCache.LoadFromDatabase(dbCtx);
}

// ==================== Pipeline ====================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Sıra: Routing → CORS → Auth
app.UseRouting();

// Default CORS policy'yi uygula (MapControllers'tan önce olmalı)
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// API Key middleware
app.UseMiddleware<ApiKeyMiddleware>();

// Bazı ortamlarda preflight (OPTIONS) erken 204 dönebilir.
// Bu mapping, CORS middleware üzerinden geçmesini garanti eder.
app.MapMethods("{*path}", new[] { "OPTIONS" }, () => Results.NoContent())
   .RequireCors(); // default policy

app.MapControllers();

app.Run();
