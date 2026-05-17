using FileService.Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Persistence.Configurations;

public sealed class FileEventConfiguration : IEntityTypeConfiguration<FileEvent>
{
    public void Configure(EntityTypeBuilder<FileEvent> builder)
    {
        builder.ToTable("file_events");

        builder.HasKey(fileEvent => fileEvent.Id);

        builder.Property(fileEvent => fileEvent.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(fileEvent => fileEvent.FileId)
            .HasColumnName("file_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(fileEvent => fileEvent.EventType)
            .HasColumnName("event_type")
            .HasColumnType("varchar")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(fileEvent => fileEvent.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.Property(fileEvent => fileEvent.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasOne(fileEvent => fileEvent.File)
            .WithMany(file => file.Events)
            .HasForeignKey(fileEvent => fileEvent.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(fileEvent => fileEvent.FileId)
            .HasDatabaseName("ix_file_events_file_id");

        builder.HasIndex(fileEvent => fileEvent.CreatedAt)
            .HasDatabaseName("ix_file_events_created_at");
    }
}