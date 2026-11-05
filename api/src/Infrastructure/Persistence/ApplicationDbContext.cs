using System.Reflection;
using Domain.Allocations;
using Domain.Centers;
using Domain.Participants;
using Domain.Pins;
using Domain.Rooms;
using Domain.Rules;
using Domain.Sessions;
using Domain.SolveRuns;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ExamSession> ExamSessions { get; init; }

    public DbSet<TestCenter> TestCenters { get; init; }

    public DbSet<Room> Rooms { get; init; }

    public DbSet<Seat> Seats { get; init; }

    public DbSet<Participant> Participants { get; init; }

    public DbSet<Rule> Rules { get; init; }

    public DbSet<Pin> Pins { get; init; }

    public DbSet<SolveRun> SolveRuns { get; init; }

    public DbSet<Allocation> Allocations { get; init; }

    public DbSet<AllocationItem> AllocationItems { get; init; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(builder);
    }
}