using Microsoft.EntityFrameworkCore;
using TradingPartnerPortal.Domain.Entities;

namespace TradingPartnerPortal.Infrastructure.Data;

public class TradingPartnerPortalDbContext : DbContext
{
    public TradingPartnerPortalDbContext(DbContextOptions<TradingPartnerPortalDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Partner> Partners { get; set; }
    public DbSet<PgpKey> PgpKeys { get; set; }
    public DbSet<SftpCredential> SftpCredentials { get; set; }
    public DbSet<FileTransferEvent> FileTransferEvents { get; set; }
    public DbSet<AuditEvent> AuditEvents { get; set; }
    public DbSet<SseEventCursor> SseEventCursors { get; set; }
    public DbSet<SftpConnectionEvent> SftpConnectionEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Partner entity configuration
        modelBuilder.Entity<Partner>(entity =>
        {
            entity.HasKey(e => e.PartnerId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).HasConversion<string>();
        });

        // PgpKey entity configuration
        modelBuilder.Entity<PgpKey>(entity =>
        {
            entity.HasKey(e => e.KeyId);
            entity.Property(e => e.PublicKeyArmored).IsRequired();
            entity.Property(e => e.Fingerprint).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Algorithm).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>();
            
            entity.HasOne(e => e.Partner)
                  .WithMany(p => p.PgpKeys)
                  .HasForeignKey(e => e.PartnerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.PartnerId, e.Fingerprint }).IsUnique();
        });

        // SftpCredential entity configuration
        modelBuilder.Entity<SftpCredential>(entity =>
        {
            entity.HasKey(e => e.PartnerId);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.PasswordSalt).IsRequired();
            entity.Property(e => e.RotationMethod).HasConversion<string>();
            
            entity.HasOne(e => e.Partner)
                  .WithOne(p => p.SftpCredential)
                  .HasForeignKey<SftpCredential>(e => e.PartnerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FileTransferEvent entity configuration
        modelBuilder.Entity<FileTransferEvent>(entity =>
        {
            entity.HasKey(e => e.FileId);
            entity.Property(e => e.Direction).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.DocType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ErrorCode).HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            
            entity.HasOne(e => e.Partner)
                  .WithMany(p => p.FileTransferEvents)
                  .HasForeignKey(e => e.PartnerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ReceivedAt);
            entity.HasIndex(e => new { e.PartnerId, e.Status });
        });

        // AuditEvent entity configuration
        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(e => e.AuditId);
            entity.Property(e => e.ActorUserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ActorRole).IsRequired().HasMaxLength(50);
            entity.Property(e => e.OperationType).HasConversion<string>();
            entity.Property(e => e.MetadataJson).IsRequired();
            
            entity.HasOne(e => e.Partner)
                  .WithMany(p => p.AuditEvents)
                  .HasForeignKey(e => e.PartnerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.PartnerId, e.OperationType });
        });

        // SseEventCursor entity configuration
        modelBuilder.Entity<SseEventCursor>(entity =>
        {
            entity.HasKey(e => e.PartnerId);
            
            entity.HasOne(e => e.Partner)
                  .WithOne()
                  .HasForeignKey<SseEventCursor>(e => e.PartnerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SftpConnectionEvent entity configuration
        modelBuilder.Entity<SftpConnectionEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.Outcome).HasConversion<string>();
            
            entity.HasOne(e => e.Partner)
                  .WithMany()
                  .HasForeignKey(e => e.PartnerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => new { e.PartnerId, e.Outcome });
        });
    }
}