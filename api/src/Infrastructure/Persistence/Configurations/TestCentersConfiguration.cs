using Domain.Centers;
using Domain.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class TestCentersConfiguration : IEntityTypeConfiguration<TestCenter>
{
    public void Configure(EntityTypeBuilder<TestCenter> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new TestCenterId(x));
        builder.Property(x => x.SessionId).HasConversion(x => x.Value, x => new ExamSessionId(x));
        builder.Property(x => x.Name).HasMaxLength(TestCenterConstraints.NameMaxLength).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(TestCenterConstraints.AddressMaxLength).IsRequired();
        builder.Property(x => x.GooglePlaceId).HasMaxLength(TestCenterConstraints.GooglePlaceIdMaxLength);
        builder.Property(x => x.CostPerRoom).HasPrecision(ColumnTypes.MoneyPrecision, ColumnTypes.MoneyScale);
        builder.Property(x => x.IsActive);
        builder.Property(x => x.Version).IsRowVersion();
        builder.OwnsOne(x => x.Location, location =>
        {
            location.Property(x => x.Latitude);
            location.Property(x => x.Longitude);
        });
        builder.Navigation(x => x.Location).IsRequired();

        builder.HasOne<ExamSession>()
            .WithMany()
            .HasForeignKey(x => x.SessionId)
            .HasConstraintName("fk_test_centers_exam_sessions_session_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Rooms)
            .WithOne(x => x.Center)
            .HasForeignKey(x => x.CenterId)
            .HasConstraintName("fk_rooms_test_centers_center_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SessionId, x.Name }).IsUnique();
    }
}