using AppointmentBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace AppointmentBooking.API.Identity
{
    public static class IdentityDataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var roleManager =
                scope.ServiceProvider.GetRequiredService<
                    RoleManager<IdentityRole>>();

            var userManager =
                scope.ServiceProvider.GetRequiredService<
                    UserManager<ApplicationUser>>();

            var configuration =
                scope.ServiceProvider.GetRequiredService<
                    IConfiguration>();

            var logger =
                scope.ServiceProvider.GetRequiredService<
                    ILoggerFactory>()
                .CreateLogger("IdentityDataSeeder");

            // Create application roles.
            string[] roles = ["Admin", "Staff"];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(
                        new IdentityRole(role));

                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Unable to create role {role}: " +
                            string.Join("; ",
                                result.Errors.Select(x => x.Description)));
                    }
                }
            }

            // Read initial administrator configuration.
            var email = configuration["InitialAdmin:Email"];
            var password = configuration["InitialAdmin:Password"];

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning(
                    "Initial Admin account was not created. " +
                    "InitialAdmin credentials are not configured.");

                return;
            }

            var firstName =
                configuration["InitialAdmin:FirstName"] ?? "System";

            var lastName =
                configuration["InitialAdmin:LastName"] ?? "Administrator";

            var admin = await userManager.FindByEmailAsync(email);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult =
                    await userManager.CreateAsync(admin, password);

                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Unable to create initial Admin account: " +
                        string.Join("; ",
                            createResult.Errors.Select(x => x.Description)));
                }

                logger.LogInformation(
                    "Initial Admin account created.");
            }

            // Ensure the administrator has the Admin role.
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                var roleResult =
                    await userManager.AddToRoleAsync(admin, "Admin");

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Unable to assign Admin role: " +
                        string.Join("; ",
                            roleResult.Errors.Select(x => x.Description)));
                }
            }

            logger.LogInformation(
                "Initial Admin account is configured.");
        }
    }
}