using FileService.Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Persistence.Configurations;

public sealed class FileCleanupLogConfiguration : IEntityTypeConfiguration<FileCleanupLog>
{
    public void Configure(EntityTypeBuilder<FileCleanupLog> builder)
    {
        builder.ToTable("file_cleanup_logs");

        builder.HasKey(cleanupLog => cleanupLog.Id);

        builder.Property(cleanupLog => cleanupLog.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(cleanupLog => cleanupLog.FileId)
            .HasColumnName("file_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(cleanupLog => cleanupLog.Action)
            .HasColumnName("action")
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(cleanupLog => cleanupLog.Status)
            .HasColumnName("status")
            .HasColumnType("varchar")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(cleanupLog => cleanupLog.Message)
            .HasColumnName("message")
            .HasColumnType("text");

        builder.Property(cleanupLog => cleanupLog.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasOne(cleanupLog => cleanupLog.File)
            .WithMany(file => file.CleanupLogs)
            .HasForeignKey(cleanupLog => cleanupLog.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cleanupLog => cleanupLog.FileId)
            .HasDatabaseName("ix_file_cleanup_logs_file_id");

        builder.HasIndex(cleanupLog => cleanupLog.CreatedAt)
            .HasDatabaseName("ix_file_cleanup_logs_created_at");
    }
}