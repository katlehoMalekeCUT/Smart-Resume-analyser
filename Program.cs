 using SmartResumeAnalyzer.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add MVC services
builder.Services.AddControllersWithViews();

// Add in-memory caching for performance
builder.Services.AddMemoryCache();

// Add authentication (cookie) and a simple in-memory user service
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.LogoutPath = "/Home/Logout";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Add HttpClient for Supabase
// Register Supabase service. If configuration is missing, register a local stub for developer ease.
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseAnon = builder.Configuration["Supabase:AnonKey"];
var supabaseBucket = builder.Configuration["Supabase:BucketName"];
if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(supabaseAnon) || string.IsNullOrWhiteSpace(supabaseBucket))
{
    // Register the development stub
    builder.Services.AddSingleton<ISupabaseAuthService, SupabaseAuthServiceStub>();
}
else
{
    builder.Services.AddHttpClient<ISupabaseAuthService, SupabaseAuthService>();
}

// Simple in-memory user store (for backward compatibility - can be removed later)
builder.Services.AddSingleton<UserService>();

// Register custom services
builder.Services.AddScoped<ResumeParserService>();
builder.Services.AddScoped<VerbMatchingService>();
builder.Services.AddScoped<ATSScoringService>();
builder.Services.AddScoped<KeywordAnalyzerService>();

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Configure routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Allow integration tests to reference Program
public partial class Program { }
