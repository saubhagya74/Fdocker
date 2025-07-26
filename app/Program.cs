
using app.Data;
using app.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Text.Json;

namespace app
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Configure database
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DatabaseConnection")));
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            // 2. Controllers & SignalR
            builder.Services.AddControllers();
            builder.Services.AddSingleton<SnowFlakeGen>();
            builder.Services.AddSignalR();
            builder.Services
            .AddControllers()
            .AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });


            // 3. Swagger
            builder.Services.AddOpenApi();

            // 4. Authentication + JWT configuration with SignalR support
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["AppSettings:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["AppSettings:Audience"],
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Token"]!)),
                        ClockSkew = TimeSpan.Zero // optional: makes token expiry strict
                    };

                    // Required for SignalR to read token from query string
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
     (path.StartsWithSegments("/ChatHub") || path.StartsWithSegments("/VideoChatHub")))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // 5. CORS setup
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularDevClient", policy =>
                {
                    policy.WithOrigins(  "http://52.86.249.52")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // needed for cookies or SignalR
                });
            });

            var app = builder.Build();

            // Apply EF Core migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate(); // This will apply any pending migrations to the database
}

            app.Urls.Add("http://+:80");

            // 6. Middleware pipeline
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


            app.UseRouting(); //  Required before CORS/Auth

            app.UseCors("AllowAngularDevClient"); //  Must come before Auth

            app.UseAuthentication();
            app.UseAuthorization();
         
            app.MapControllers();
            app.MapHub<ChatHub>("/ChatHub"); 
            app.MapHub<VideoChatHub>("/VideoChatHub"); //  Match Angular path exactly

            app.Run();
        }
    }
}
