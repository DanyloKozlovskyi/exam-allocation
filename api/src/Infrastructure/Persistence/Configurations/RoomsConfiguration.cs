using Domain.Centers;
using Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RoomsConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new RoomId(x));
        builder.Property(x => x.CenterId).HasConversion(x => x.Value, x => new TestCenterId(x));
        builder.Property(x => x.Name).HasMaxLength(RoomConstraints.NameMaxLength).IsRequired();
        builder.Property(x => x.Layout).HasColumnType(ColumnTypes.Jsonb).IsRequired();
        builder.Property(x => x.Capacity);
        builder.Property(x => x.AccessibleCapacity);
        builder.Property(x => x.CostOverride).HasPrecision(ColumnTypes.MoneyPrecision, ColumnTypes.MoneyScale);
        builder.Property(x => x.IsActive);
        builder.Property(x => x.Version).IsRowVersion();

        builder.HasMany(x => x.Seats)
            .WithOne()
            .HasForeignKey(x => x.RoomId)
            .HasConstraintName("fk_seats_rooms_room_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CenterId, x.Name }).IsUnique();
    }
}