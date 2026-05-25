using FileService.Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Persistence.Configurations;

public sealed class FileRecordConfiguration : IEntityTypeConfiguration<FileRecord>
{
    public void Configure(EntityTypeBuilder<FileRecord> builder)
    {
        builder.ToTable("files");

        builder.HasKey(file => file.Id);

        builder.Property(file => file.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(file => file.TenantId)
            .HasColumnName("tenant_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(file => file.OwnerId)
            .HasColumnName("owner_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(file => file.Purpose)
            .HasColumnName("purpose")
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(file => file.EntityType)
            .HasColumnName("entity_type")
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(file => file.EntityId)
            .HasColumnName("entity_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(file => file.IsPrivate)
            .HasColumnName("is_private")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(file => file.Provider)
            .HasColumnName("provider")
            .HasColumnType("varchar")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(file => file.ProviderKey)
            .HasColumnName("provider_key")
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(file => file.StorageRegion)
            .HasColumnName("storage_region")
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(file => file.OriginalFilename)
            .HasColumnName("original_filename")
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(file => file.MimeType)
            .HasColumnName("mime_type")
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(file => file.Size)
            .HasColumnName("size")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(file => file.Checksum)
            .HasColumnName("checksum")
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(file => file.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.Property(file => file.Status)
            .HasColumnName("status")
            .HasColumnType("varchar")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(file => file.StagingExpiresAt)
            .HasColumnName("staging_expires_at")
            .HasColumnType("timestamptz");

        builder.Property(file => file.CommittedAt)
            .HasColumnName("committed_at")
            .HasColumnType("timestamptz");

        builder.Property(file => file.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamptz");

        builder.Property(file => file.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(file => file.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasMany(file => file.Events)
            .WithOne(fileEvent => fileEvent.File)
            .HasForeignKey(fileEvent => fileEvent.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(file => file.CleanupLogs)
            .WithOne(cleanupLog => cleanupLog.File)
            .HasForeignKey(cleanupLog => cleanupLog.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(file => file.TenantId)
            .HasDatabaseName("ix_files_tenant_id");

        builder.HasIndex(file => file.OwnerId)
            .HasDatabaseName("ix_files_owner_id");

        builder.HasIndex(file => file.EntityId)
            .HasDatabaseName("ix_files_entity_id");

        builder.HasIndex(file => file.Status)
            .HasDatabaseName("ix_files_status");

        builder.HasIndex(file => file.StagingExpiresAt)
            .HasDatabaseName("idx_files_staging_expiry")
            .HasFilter("status = 'Staged'");
    }
}