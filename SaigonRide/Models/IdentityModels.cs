using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace SaigonRide.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            UserType = "Local";
        }

        [Required]
        [StringLength(20)]
        public string UserType { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public System.Data.Entity.DbSet<SaigonRide.Models.Vehicle> Vehicles { get; set; }

        public System.Data.Entity.DbSet<SaigonRide.Models.Station> Stations { get; set; }

        public System.Data.Entity.DbSet<SaigonRide.Models.VehicleCategory> VehicleCategories { get; set; }

        public System.Data.Entity.DbSet<SaigonRide.Models.Rental> Rentals { get; set; }

        public System.Data.Entity.DbSet<SaigonRide.Models.Payment> Payments { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Rental>()
                .HasRequired(r => r.Vehicle)
                .WithMany()
                .HasForeignKey(r => r.VehicleId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Rental>()
                .HasRequired(r => r.StartStation)
                .WithMany()
                .HasForeignKey(r => r.StartStationId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Rental>()
                .HasOptional(r => r.ReturnStation)
                .WithMany()
                .HasForeignKey(r => r.ReturnStationId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Payment>()
                .HasRequired(p => p.Rental)
                .WithMany()
                .HasForeignKey(p => p.RentalId)
                .WillCascadeOnDelete(false);
        }
    }
}
