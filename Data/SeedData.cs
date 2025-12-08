using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TaskManagementApp.Models;

namespace TaskManagementApp.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Create roles
            string[] roleNames = { "Admin", "Client", "User" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create test_admin user
            var adminUser = await userManager.FindByNameAsync("test_admin");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "test_admin",
                    Email = "test_admin@example.com",
                    FirstName = "Test",
                    LastName = "Admin",
                    EmailConfirmed = true
                };

                var createAdminUser = await userManager.CreateAsync(adminUser, "password123");
                if (createAdminUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Create test_client user
            var clientUser = await userManager.FindByNameAsync("test_client");
            if (clientUser == null)
            {
                clientUser = new ApplicationUser
                {
                    UserName = "test_client",
                    Email = "test_client@example.com",
                    FirstName = "Test",
                    LastName = "Client",
                    EmailConfirmed = true
                };

                var createClientUser = await userManager.CreateAsync(clientUser, "password123");
                if (createClientUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(clientUser, "Client");
                }
            }
        }
    }
}