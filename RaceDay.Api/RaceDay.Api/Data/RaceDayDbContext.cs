using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using Route = RaceDay.Api.Models.Route;

namespace RaceDay.Api.Data;

// The DbContext is EF Core's "door" to the database. Each DbSet = one table.
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Table names match the Part 1 SQL script (singular)
        b.Entity<Organiser>().ToTable("Organiser");
        b.Entity<Participant>().ToTable("Participant", t =>
            t.HasCheckConstraint("CK_Participant_Gender", "[Gender] IN ('Male','Female','Other')"));
        b.Entity<Event>().ToTable("Event", t =>
        {
            t.HasCheckConstraint("CK_Event_Type", "[EventType] IN ('Run','Walk','Cycle')");
            t.HasCheckConstraint("CK_Event_Distance", "[DistanceKm] > 0");
        });
        b.Entity<Category>().ToTable("Category", t =>
            t.HasCheckConstraint("CK_Category_Gender", "[Gender] IN ('Male','Female','Any')"));
        b.Entity<Route>().ToTable("Route");
        b.Entity<Registration>().ToTable("Registration");
        b.Entity<Result>().ToTable("Result", t =>
            t.HasCheckConstraint("CK_Result_Status", "[Status] IN ('Registered','Finished','DNF','DSQ')"));

        // Unique constraints
        b.Entity<Organiser>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Participant>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Category>().HasIndex(x => new { x.EventID, x.Name }).IsUnique();
        b.Entity<Registration>().HasIndex(x => new { x.ParticipantID, x.CategoryID }).IsUnique();
        b.Entity<Registration>().HasIndex(x => x.BibNumber).IsUnique();
        b.Entity<Result>().HasIndex(x => x.RegistrationID).IsUnique();

        // Column types / defaults
        b.Entity<Event>().Property(x => x.DistanceKm).HasPrecision(6, 2);
        b.Entity<Category>().Property(x => x.DistanceKm).HasPrecision(6, 2);
        b.Entity<Route>().Property(x => x.DistanceKm).HasPrecision(6, 2);
        b.Entity<Result>().Property(x => x.FinishTime).HasColumnType("time(0)");
        b.Entity<Category>().Property(x => x.Gender).HasDefaultValue("Any");
        b.Entity<Registration>().Property(x => x.RegistrationDate).HasDefaultValueSql("GETDATE()");
        b.Entity<Result>().Property(x => x.Status).HasDefaultValue("Registered");

        // Relationships (cascade rules match the SQL script)
        b.Entity<Event>().HasOne(e => e.Organiser).WithMany(o => o.Events)
            .HasForeignKey(e => e.OrganiserID).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Category>().HasOne(c => c.Event).WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventID).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Route>().HasOne(r => r.Event).WithMany(e => e.Routes)
            .HasForeignKey(r => r.EventID).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Registration>().HasOne(r => r.Participant).WithMany(p => p.Registrations)
            .HasForeignKey(r => r.ParticipantID).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Registration>().HasOne(r => r.Category).WithMany(c => c.Registrations)
            .HasForeignKey(r => r.CategoryID).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Result>().HasOne(r => r.Registration).WithOne(r => r.Result)
            .HasForeignKey<Result>(r => r.RegistrationID).OnDelete(DeleteBehavior.Cascade);
    }
}
