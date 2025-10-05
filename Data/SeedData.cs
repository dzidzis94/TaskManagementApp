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

            // Izveido lomas
            string[] roleNames = { "Admin", "User" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Izveido administratoru
            var adminUser = await userManager.FindByEmailAsync("admin@taskapp.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@taskapp.com",
                    Email = "admin@taskapp.com",
                    FirstName = "Sistēmas",
                    LastName = "Administrators",
                    EmailConfirmed = true
                };

                var createPowerUser = await userManager.CreateAsync(adminUser, "Admin123!");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Izveido parasto lietotāju
            var normalUser = await userManager.FindByEmailAsync("user@taskapp.com");
            if (normalUser == null)
            {
                normalUser = new ApplicationUser
                {
                    UserName = "user@taskapp.com",
                    Email = "user@taskapp.com",
                    FirstName = "Parasts",
                    LastName = "Lietotājs",
                    EmailConfirmed = true
                };

                var createUser = await userManager.CreateAsync(normalUser, "User123!");
                if (createUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(normalUser, "User");
                }
            }
        }
    }
}