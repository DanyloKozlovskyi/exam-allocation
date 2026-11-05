using Domain.Participants;
using Domain.Pins;
using Domain.Rooms;
using Domain.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PinsConfiguration : IEntityTypeConfiguration<Pin>
{
    public void Configure(EntityTypeBuilder<Pin> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new PinId(x));
        builder.Property(x => x.SessionId).HasConversion(x => x.Value, x => new ExamSessionId(x));
        builder.Property(x => x.ParticipantId).HasConversion(x => x.Value, x => new ParticipantId(x));
        builder.Property(x => x.RoomId).HasConversion(x => x.Value, x => new RoomId(x));
        builder.Property(x => x.SeatId).HasConversion(x => x!.Value, x => new SeatId(x));

        builder.HasOne<ExamSession>()
            .WithMany()
            .HasForeignKey(x => x.SessionId)
            .HasConstraintName("fk_pins_exam_sessions_session_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Participant>()
            .WithMany()
            .HasForeignKey(x => x.ParticipantId)
            .HasConstraintName("fk_pins_participants_participant_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .HasConstraintName("fk_pins_rooms_room_id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Seat>()
            .WithMany()
            .HasForeignKey(x => x.SeatId)
            .HasConstraintName("fk_pins_seats_seat_id")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.SessionId, x.ParticipantId }).IsUnique();
    }
}