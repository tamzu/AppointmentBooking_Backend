using AppointmentBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace AppointmentBooking.API.Identity
{
    public static class IdentityAdminSeeder
    {
        public static async Task SeedAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var email = configuration["BootstrapAdmin:Email"];
            var password = configuration["BootstrapAdmin:Password"];
            var firstName = configuration["BootstrapAdmin:FirstName"];
            var lastName = configuration["BootstrapAdmin:LastName"];

            // Bootstrap credentials are optional.
            // No administrator is created when they are absent.
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            email = email.Trim().ToLowerInvariant();

            var admin = await userManager.FindByEmailAsync(email);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName?.Trim() ?? "System",
                    LastName = lastName?.Trim() ?? "Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(
                    admin, password);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(
                        "; ",
                        createResult.Errors.Select(x => x.Description));

                    throw new InvalidOperationException(
                        $"Failed to create bootstrap administrator: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                var roleResult = await userManager.AddToRoleAsync(
                    admin, "Admin");

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(
                        "; ",
                        roleResult.Errors.Select(x => x.Description));

                    throw new InvalidOperationException(
                        $"Failed to assign Admin role: {errors}");
                }
            }
        }
    }
}