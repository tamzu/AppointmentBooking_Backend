using AppointmentBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace AppointmentBooking.API.Identity
{
    public static class IdentityStaffSeeder
    {
        public static async Task SeedAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var email = configuration["BootstrapStaff:Email"];
            var password = configuration["BootstrapStaff:Password"];

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            email = email.Trim().ToLowerInvariant();

            var staff = await userManager.FindByEmailAsync(email);

            if (staff == null)
            {
                staff = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = configuration["BootstrapStaff:FirstName"]
                        ?? "Appointment",
                    LastName = configuration["BootstrapStaff:LastName"]
                        ?? "Staff",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(
                    staff, password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        "; ",
                        result.Errors.Select(x => x.Description));

                    throw new InvalidOperationException(
                        $"Failed to create staff account: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(staff, "Staff"))
            {
                var result = await userManager.AddToRoleAsync(
                    staff, "Staff");

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        "; ",
                        result.Errors.Select(x => x.Description));

                    throw new InvalidOperationException(
                        $"Failed to assign Staff role: {errors}");
                }
            }
        }
    }
}