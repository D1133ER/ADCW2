using WeblogApplication.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WeblogApplication.Data;
using WeblogApplication.Interfaces;
using WeblogApplication.Implementation;
using WeblogApplication.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace WeblogApplication
{
    public class Startup

    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("FrontendPolicy", policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:5173",
                            "http://localhost:4173",
                            "http://localhost:8080")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            services.AddControllersWithViews();
            services.AddSession();

            // Dual authentication: JWT for API controllers, Session for MVC controllers
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "SessionOrJwt";
                    options.DefaultChallengeScheme = "SessionOrJwt";
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidAudience = Configuration["Jwt:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                    };
                })
                .AddCookie("SessionAuth", options =>
                {
                    options.LoginPath = "/user/login";
                    options.ExpireTimeSpan = TimeSpan.FromDays(1);
                })
                .AddPolicyScheme("SessionOrJwt", "Session or JWT", options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        // Use JWT if Authorization header present, otherwise session cookie
                        string? authorization = context.Request.Headers.Authorization;
                        if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            return JwtBearerDefaults.AuthenticationScheme;
                        return "SessionAuth";
                    };
                });

            // Add DbContext using AddDbContext
            services.AddDbContext<WeblogApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // Register services
            services.AddScoped<IBlogService, BlogService>();
            services.AddScoped<IRankingService, RankingService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IAuthorizationHandler, ResourceOwnerHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ResourceOwner", policy =>
                    policy.Requirements.Add(new ResourceOwnerRequirement()));
            });

            services.AddHttpContextAccessor();
            services.Configure<SmtpOptions>(Configuration.GetSection("SmtpOptions"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors("FrontendPolicy");
            app.UseSession();

            app.UseAuthentication();
          
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Blog}/{action=Index}/{id?}");

                // Serve React SPA for all non-API, non-MVC routes (production)
                endpoints.MapFallbackToFile("index.html");
            });

            // In development, SpaProxy auto-starts Vite via the .csproj SpaProxyLaunchCommand.
            // In production, the built SPA files are served from wwwroot via MapFallbackToFile above.
        }
    }
}
