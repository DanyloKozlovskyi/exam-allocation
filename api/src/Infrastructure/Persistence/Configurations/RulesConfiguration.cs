using Domain.Rules;
using Domain.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RulesConfiguration : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new RuleId(x));
        builder.Property(x => x.SessionId).HasConversion(x => x.Value, x => new ExamSessionId(x));
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(ColumnTypes.EnumMaxLength);
        builder.Property(x => x.Parameters).HasColumnType(ColumnTypes.Jsonb).IsRequired();
        builder.Property(x => x.IsHard);
        builder.Property(x => x.Weight);
        builder.Property(x => x.IsEnabled);
        builder.Property(x => x.DisplayName).HasMaxLength(RuleConstraints.DisplayNameMaxLength);
        builder.Property(x => x.Version).IsRowVersion();

        builder.HasOne<ExamSession>()
            .WithMany()
            .HasForeignKey(x => x.SessionId)
            .HasConstraintName("fk_rules_exam_sessions_session_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}