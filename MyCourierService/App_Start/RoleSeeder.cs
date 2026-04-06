using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using MyCourierSA.Models;
using MyCourierSA.Constants;
using System;
using System.Linq;
using System.Collections.Generic;

public class RoleSeeder
{
    public static void Seed()
    {
        using (var context = new ApplicationDbContext())
        {
            var roleStore = new RoleStore<IdentityRole>(context);
            var roleManager = new RoleManager<IdentityRole>(roleStore);

            var userStore = new UserStore<ApplicationUser>(context);
            var userManager = new UserManager<ApplicationUser>(userStore);

            // 1. SEED ROLES (The "Containers" for permissions)
            // These must exist so the Admin can assign people to them later.
            string[] roles = {
                AppConstants.Roles.Admin,
                AppConstants.Roles.Dispatcher,
                AppConstants.Roles.Driver,
                AppConstants.Roles.Customer
            };

            foreach (var roleName in roles)
            {
                if (!roleManager.RoleExists(roleName))
                {
                    roleManager.Create(new IdentityRole(roleName));
                }
            }

            // 2. SEED MASTER ADMIN (The "Key" to the building)
            // We only seed this one user so you can log in the first time.
            string adminEmail = "admin@mycourier.co.za";
            if (userManager.FindByEmail(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Name = "System",
                    Surname = "Administrator",
                    CreatedAt = DateTime.UtcNow,
                    WalletBalance = 0 // Admins don't need a balance
                };

                var result = userManager.Create(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    userManager.AddToRole(adminUser.Id, AppConstants.Roles.Admin);
                }
            }

            // 3. SEED INITIAL SYSTEM SETTINGS (Bonus)
            // This ensures your PricingService doesn't crash on the first run.
            if (!context.SystemSettings.Any())
            {
                context.SystemSettings.AddRange(new List<SystemSetting>
                {
                    new SystemSetting { Key = "BasePriceFee", Value = "40.00" },
                    new SystemSetting { Key = "PerKgRateFee", Value = "12.00" },
                    new SystemSetting { Key = "DriverCommissionRate", Value = "0.70" }
                });
            }

            context.SaveChanges();
        }
    }
}