using Bioreference.ScanningService.Domain.Entities;
using SkiaSharp;
using ZXing;

namespace Bioreference.ScanningService.Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IBatchRepository Batches { get; }
    IDocumentRepository Documents { get; }
    IRepository<DocumentPage> DocumentPages { get; }
    IRepository<DocumentKeywordValue> DocumentKeywordValues { get; }
    IRepository<DocumentType> DocumentTypes { get; }
    IRepository<DocumentTypeGroup> DocumentTypeGroups { get; }
    IRepository<DocumentTypeKeyword> DocumentTypeKeywords { get; }
    IRepository<Keyword> Keywords { get; }
    IRepository<ActivityLog> ActivityLogs { get; }
    IRepository<ScanQueue> ScanQueues { get; }
    IRepository<ScanQueueDocumentType> ScanQueueDocumentTypes { get; }
    IRepository<ScanningFormat> ScanningFormats { get; }
    IRepository<BatchStatus> BatchStatuses { get; }
    IRepository<BarCodeFormatDocumentTypeKeyword> BarCodeFormatDocumentTypeKeywords { get; }
    IRepository<BarCodeFormat> BarCodeFormats { get; }
    IRepository<ScanningUser> ScanningUsers { get; }
    IRepository<ScanningUserGroup> ScanningUserGroups { get; }


    Task SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
