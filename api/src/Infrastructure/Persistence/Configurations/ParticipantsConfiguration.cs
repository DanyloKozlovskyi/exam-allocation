using Domain.Participants;
using Domain.Sessions;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ParticipantsConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new ParticipantId(x));
        builder.Property(x => x.SessionId).HasConversion(x => x.Value, x => new ExamSessionId(x));
        builder.Property(x => x.ExternalId).HasMaxLength(ParticipantConstraints.ExternalIdMaxLength);
        builder.OwnsOne(x => x.Name, name =>
        {
            name.Property(x => x.LastName).HasMaxLength(PersonName.PartMaxLength).IsRequired();
            name.Property(x => x.FirstName).HasMaxLength(PersonName.PartMaxLength).IsRequired();
            name.Property(x => x.MiddleName).HasMaxLength(PersonName.PartMaxLength);
            name.Ignore(x => x.SortKey);
        });
        builder.Navigation(x => x.Name).IsRequired();
        builder.Property(x => x.Group).HasMaxLength(ParticipantConstraints.GroupMaxLength);
        builder.Property(x => x.School).HasMaxLength(ParticipantConstraints.SchoolMaxLength);
        builder.Property(x => x.NeedsAccessibleSeat);
        builder.Property(x => x.Attributes).HasColumnType(ColumnTypes.Jsonb).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(ParticipantConstraints.NotesMaxLength);
        builder.Property(x => x.Version).IsRowVersion();

        builder.HasOne<ExamSession>()
            .WithMany()
            .HasForeignKey(x => x.SessionId)
            .HasConstraintName("fk_participants_exam_sessions_session_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SessionId, x.ExternalId })
            .IsUnique()
            .HasFilter("external_id IS NOT NULL");
        builder.HasIndex(x => new { x.SessionId, x.Group });
        builder.HasIndex(x => new { x.SessionId, x.School });
    }
}