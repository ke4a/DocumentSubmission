using DocumentSubmission.Domain.Submissions;
using DocumentSubmission.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DocumentSubmission.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Recipient> Recipients => Set<Recipient>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocumentContent> DocumentContents => Set<DocumentContent>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // DateTime properties map to "timestamp with time zone" (Npgsql's default); values must be UTC.

        modelBuilder.Entity<User>(user =>
        {
            user.HasKey(u => u.Id);
            user.Property(u => u.Id).ValueGeneratedNever();
            user.Property(u => u.Name).HasMaxLength(100);
            user.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            user.Property(u => u.CountryCode).HasMaxLength(2);
            user.HasData(SeedData.Users);
        });

        modelBuilder.Entity<Recipient>(recipient =>
        {
            recipient.HasKey(r => r.Id);
            recipient.Property(r => r.Id).ValueGeneratedNever();
            recipient.Property(r => r.Name).HasMaxLength(100);
            recipient.Property(r => r.CountryCode).HasMaxLength(2);
            recipient.HasData(SeedData.Recipients);
        });

        modelBuilder.Entity<Submission>(submission =>
        {
            submission.HasKey(s => s.Id);
            submission.Property(s => s.Id).ValueGeneratedNever();
            submission.Property(s => s.Title).HasMaxLength(Submission.MaxTitleLength);
            submission.Property(s => s.CountryCode).HasMaxLength(2);
            submission.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            submission.Property(s => s.ConcurrencyToken).IsConcurrencyToken();

            submission.HasOne(s => s.Editor).WithMany().HasForeignKey(s => s.EditorId).OnDelete(DeleteBehavior.Restrict);
            submission.HasOne(s => s.Reviewer).WithMany().HasForeignKey(s => s.ReviewerId).OnDelete(DeleteBehavior.Restrict);
            submission.HasOne(s => s.Recipient).WithMany().HasForeignKey(s => s.RecipientId).OnDelete(DeleteBehavior.Restrict);

            // Versions and audit entries are reached through read-only collections backed by private lists.
            submission.HasMany(s => s.Versions).WithOne().HasForeignKey(v => v.SubmissionId);
            submission.HasMany(s => s.AuditEntries).WithOne().HasForeignKey(a => a.SubmissionId);

            submission.HasIndex(s => s.EditorId);
            submission.HasIndex(s => s.ReviewerId);
        });

        modelBuilder.Entity<DocumentVersion>(version =>
        {
            // Composite key: version numbers run 1..N per submission. Two concurrent resubmits that both
            // try to insert version N+1 hit this primary key, which the API maps to 409.
            version.HasKey(v => new { v.SubmissionId, v.Number });
            version.Property(v => v.Number).ValueGeneratedNever();
            version.Property(v => v.FileName).HasMaxLength(255);
            version.Property(v => v.ContentType).HasMaxLength(100);

            version.HasOne(v => v.DocumentContent).WithOne()
                .HasForeignKey<DocumentContent>(c => new { c.SubmissionId, c.VersionNumber });
        });

        modelBuilder.Entity<DocumentContent>(content =>
        {
            content.HasKey(c => new { c.SubmissionId, c.VersionNumber });
        });

        modelBuilder.Entity<AuditEntry>(audit =>
        {
            audit.HasKey(a => a.Id);
            // The domain creates the Id; without this, EF would treat a new entry with an Id as an existing row.
            audit.Property(a => a.Id).ValueGeneratedNever();
            audit.Property(a => a.Action).HasConversion<string>().HasMaxLength(20);
            audit.Property(a => a.Comment).HasMaxLength(Submission.MaxCommentLength);

            audit.HasOne(a => a.Actor).WithMany().HasForeignKey(a => a.ActorId).OnDelete(DeleteBehavior.Restrict);
            audit.HasOne(a => a.Recipient).WithMany().HasForeignKey(a => a.RecipientId).OnDelete(DeleteBehavior.Restrict);

            audit.HasIndex(a => a.SubmissionId);
        });
    }
}
