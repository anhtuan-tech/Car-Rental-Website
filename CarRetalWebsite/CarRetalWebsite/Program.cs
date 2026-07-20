using CarRetalWebsite.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace CarRetalWebsite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<CarRetalWebsite.Services.EmailService>();
            builder.Services.AddScoped<CarRetalWebsite.Services.IImageService, CarRetalWebsite.Services.ImageService>();
            // Register DbContext with connection string from appsettings.json
            builder.Services.AddDbContext<CarRentalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                });

            var app = builder.Build();

            // Seed mock data
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<CarRentalDbContext>();
                CarRetalWebsite.Services.DbInitializer.SeedAsync(context).GetAwaiter().GetResult();
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Serve static files from the root repo "image" folder
            var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(app.Environment.ContentRootPath, "..", ".."));
            var imagePath = System.IO.Path.Combine(repoRoot, "image");

            if (!System.IO.Directory.Exists(imagePath))
            {
                System.IO.Directory.CreateDirectory(imagePath);
            }
            if (!System.IO.Directory.Exists(System.IO.Path.Combine(imagePath, "Car")))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(imagePath, "Car"));
            }
            if (!System.IO.Directory.Exists(System.IO.Path.Combine(imagePath, "User")))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(imagePath, "User"));
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(imagePath),
                RequestPath = "/image"
            });

            app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
