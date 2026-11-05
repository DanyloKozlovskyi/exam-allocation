using Domain.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ExamSessionsConfiguration : IEntityTypeConfiguration<ExamSession>
{
    public void Configure(EntityTypeBuilder<ExamSession> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new ExamSessionId(x));
        builder.Property(x => x.Name).HasMaxLength(ExamSessionConstraints.NameMaxLength).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(ExamSessionConstraints.DescriptionMaxLength);
        builder.Property(x => x.ExamDate);
        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}