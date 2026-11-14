using System.Text.Json;
using Domain.Rules;
using Domain.Rules.Parameters;
using Domain.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RulesConfiguration : IEntityTypeConfiguration<Rule>
{
    /// <summary>
    /// jsonb normalizes key order, so the polymorphic "type" discriminator is not necessarily the first
    /// property when read back; out-of-order metadata must be allowed.
    /// </summary>
    private static readonly JsonSerializerOptions ParametersJson = new()
    {
        AllowOutOfOrderMetadataProperties = true,
    };

    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new RuleId(x));
        builder.Property(x => x.SessionId).HasConversion(x => x.Value, x => new ExamSessionId(x));
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(ColumnTypes.EnumMaxLength);
        builder.Property(x => x.Parameters)
            .HasColumnType(ColumnTypes.Jsonb)
            .HasConversion(
                x => Serialize(x),
                x => Deserialize(x),
                new ValueComparer<RuleParameters>(
                    (a, b) => Serialize(a) == Serialize(b),
                    x => Serialize(x).GetHashCode(StringComparison.Ordinal),
                    x => Deserialize(Serialize(x))))
            .IsRequired();
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

    private static string Serialize(RuleParameters? parameters)
        => parameters is null ? string.Empty : JsonSerializer.Serialize(parameters, ParametersJson);

    private static RuleParameters Deserialize(string json)
        => JsonSerializer.Deserialize<RuleParameters>(json, ParametersJson)!;
}