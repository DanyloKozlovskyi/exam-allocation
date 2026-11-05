using Domain.Rooms;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SeatsConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new SeatId(x));
        builder.Property(x => x.RoomId).HasConversion(x => x.Value, x => new RoomId(x));
        builder.Property(x => x.Key)
            .HasConversion(x => x.Value, x => SeatKey.Parse(x))
            .HasMaxLength(SeatKey.MaxLength)
            .IsRequired();
        builder.Property(x => x.Row);
        builder.Property(x => x.Column);
        builder.Property(x => x.SeatIndex);
        builder.Property(x => x.IsAccessible);
        builder.HasIndex(x => new { x.RoomId, x.Key }).IsUnique();
    }
}