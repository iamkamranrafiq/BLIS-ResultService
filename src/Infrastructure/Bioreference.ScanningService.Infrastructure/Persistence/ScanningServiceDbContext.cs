using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bioreference.ScanningService.Infrastructure.Persistence;

public class ScanningServiceDbContext : DbContext
{
    public ScanningServiceDbContext(DbContextOptions<ScanningServiceDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLog { get; set; }

    public virtual DbSet<BarCodeFormat> BarCodeFormat { get; set; }

    public virtual DbSet<BarCodeFormatDocumentTypeKeyword> BarCodeFormatDocumentTypeKeyword { get; set; }

    public virtual DbSet<Batch> Batch { get; set; }

    public virtual DbSet<BatchStatus> BatchStatus { get; set; }

    public virtual DbSet<Document> Document { get; set; }

    public virtual DbSet<DocumentKeywordValue> DocumentKeywordValue { get; set; }

    public virtual DbSet<DocumentType> DocumentType { get; set; }

    public virtual DbSet<DocumentTypeGroup> DocumentTypeGroup { get; set; }

    public virtual DbSet<DocumentTypeKeyword> DocumentTypeKeyword { get; set; }

    public virtual DbSet<Keyword> Keyword { get; set; }

    public virtual DbSet<ScanQueue> ScanQueue { get; set; }

    public virtual DbSet<ScanQueueDocumentType> ScanQueueDocumentType { get; set; }

    public virtual DbSet<ScanningFormat> ScanningFormat { get; set; }
    
    public virtual DbSet<ScanningUser> ScanningUser { get; set; }
    
    public virtual DbSet<ScanningUserGroup> ScanningUserGroup { get; set; }
    
    public virtual DbSet<ScanningUserGroupScanningUser> ScanningUserGroupScanningUser { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScanningServiceDbContext).Assembly);
    }
}
