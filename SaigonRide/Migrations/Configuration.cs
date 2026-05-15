namespace SaigonRide.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using SaigonRide.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<SaigonRide.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(SaigonRide.Models.ApplicationDbContext context)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            foreach (var roleName in new[] { "Admin", "User" })
            {
                if (!roleManager.RoleExists(roleName))
                {
                    roleManager.Create(new IdentityRole(roleName));
                }
            }

            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
            userManager.UserValidator = new UserValidator<ApplicationUser>(userManager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };
            userManager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true
            };

            const string adminEmail = "admin@saigonride.local";
            var admin = userManager.FindByName(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, UserType = "Local" };
                var result = userManager.Create(admin, "Admin@123");
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException("Unable to seed the default admin account: " + string.Join("; ", result.Errors));
                }
            }
            else if (string.IsNullOrWhiteSpace(admin.UserType))
            {
                admin.UserType = "Local";
                userManager.Update(admin);
            }

            if (!userManager.IsInRole(admin.Id, "Admin"))
            {
                userManager.AddToRole(admin.Id, "Admin");
            }

            context.VehicleCategories.AddOrUpdate(
                c => c.Name,
                new VehicleCategory { Name = "Standard Bike", PricePerMinute = 500m },
                new VehicleCategory { Name = "E-Scooter", PricePerMinute = 1500m });
        }
    }
}
