using Domain.Allocations;
using Domain.Participants;
using Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AllocationItemsConfiguration : IEntityTypeConfiguration<AllocationItem>
{
    public void Configure(EntityTypeBuilder<AllocationItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new AllocationItemId(x));
        builder.Property(x => x.AllocationId).HasConversion(x => x.Value, x => new AllocationId(x));
        builder.Property(x => x.ParticipantId).HasConversion(x => x.Value, x => new ParticipantId(x));
        builder.Property(x => x.RoomId).HasConversion(x => x.Value, x => new RoomId(x));
        builder.Property(x => x.SeatId).HasConversion(x => x.Value, x => new SeatId(x));

        builder.HasOne<Participant>()
            .WithMany()
            .HasForeignKey(x => x.ParticipantId)
            .HasConstraintName("fk_allocation_items_participants_participant_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .HasConstraintName("fk_allocation_items_rooms_room_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Seat>()
            .WithMany()
            .HasForeignKey(x => x.SeatId)
            .HasConstraintName("fk_allocation_items_seats_seat_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.AllocationId, x.ParticipantId }).IsUnique();
        builder.HasIndex(x => new { x.AllocationId, x.RoomId });
    }
}