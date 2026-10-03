namespace Bioreference.ScanningService.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Domain.Entities;

public class UnitOfWork : IUnitOfWork
{
    private readonly ScanningServiceDbContext _context;
    private readonly ILoggerFactory _loggerFactory;
    private IBatchRepository? _batches;
    private IDocumentRepository? _documents;
    private IRepository<DocumentPage>? _documentPages;
    private IRepository<DocumentKeywordValue>? _documentKeywordValues;
    private IRepository<DocumentType>? _documentTypes;
    private IRepository<DocumentTypeGroup>? _documentTypeGroups;
    private IRepository<DocumentTypeKeyword>? _documentTypeKeywords;
    private IRepository<Keyword>? _keywords;
    private IRepository<ActivityLog>? _activityLogs;
    private IRepository<ScanQueue>? _scanQueues;
    private IRepository<ScanQueueDocumentType>? _scanQueueDocumentTypes;
    private IRepository<ScanningFormat>? _scanningFormats;
    private IRepository<BatchStatus>? _batchStatuses;
    private IRepository<ScanQueueDocumentType>? _scanQueueDocumentTypesRepository;
    private IRepository<ScanningFormat>? _scanningFormatsRepository;
    private IRepository<BatchStatus>? _batchStatusesRepository;
    private IRepository<BarCodeFormatDocumentTypeKeyword>? _barCodeFormatKeywords;
    private IRepository<BarCodeFormat>? _barCodeFormats;
    private IRepository<ScanningUser>? _scanningUsers;
    private IRepository<ScanningUserGroup>? _scanningUserGroups;


    public UnitOfWork(ScanningServiceDbContext context, ILoggerFactory loggerFactory)
    {
        _context = context;
        _loggerFactory = loggerFactory;
    }

    public IBatchRepository Batches => _batches ??= new BatchRepository(_context, _loggerFactory.CreateLogger<BatchRepository>());
    public IDocumentRepository Documents => _documents ??= new DocumentRepository(_context, _loggerFactory.CreateLogger<DocumentRepository>());
    public IRepository<DocumentPage> DocumentPages => _documentPages ??= new Repository<DocumentPage>(_context);
    public IRepository<DocumentKeywordValue> DocumentKeywordValues => 
        _documentKeywordValues ??= new Repository<DocumentKeywordValue>(_context);
    public IRepository<DocumentType> DocumentTypes => _documentTypes ??= new Repository<DocumentType>(_context);
    public IRepository<DocumentTypeGroup> DocumentTypeGroups => 
        _documentTypeGroups ??= new Repository<DocumentTypeGroup>(_context);
    public IRepository<DocumentTypeKeyword> DocumentTypeKeywords => 
        _documentTypeKeywords ??= new Repository<DocumentTypeKeyword>(_context);
    public IRepository<Keyword> Keywords => _keywords ??= new Repository<Keyword>(_context);
    public IRepository<ActivityLog> ActivityLogs => 
        _activityLogs ??= new Repository<ActivityLog>(_context);
    public IRepository<ScanQueue> ScanQueues => _scanQueues ??= new Repository<ScanQueue>(_context);
    public IRepository<ScanQueueDocumentType> ScanQueueDocumentTypes => 
        _scanQueueDocumentTypes ??= new Repository<ScanQueueDocumentType>(_context);
    public IRepository<ScanningFormat> ScanningFormats => 
        _scanningFormats ??= new Repository<ScanningFormat>(_context);
    public IRepository<BatchStatus> BatchStatuses => 
        _batchStatuses ??= new Repository<BatchStatus>(_context);
    public IRepository<BarCodeFormatDocumentTypeKeyword> BarCodeFormatDocumentTypeKeywords => 
        _barCodeFormatKeywords ??= new Repository<BarCodeFormatDocumentTypeKeyword>(_context);
    public IRepository<BarCodeFormat> BarCodeFormats => 
        _barCodeFormats ??= new Repository<BarCodeFormat>(_context);
    public IRepository<ScanningUser> ScanningUsers =>
        _scanningUsers ??= new Repository<ScanningUser>(_context);
    public IRepository<ScanningUserGroup> ScanningUserGroups =>
        _scanningUserGroups ??= new Repository<ScanningUserGroup>(_context);


    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }

    public async Task RollbackAsync()
    {
        await _context.Database.RollbackTransactionAsync();

        // Rolling back the database transaction does NOT clear EF Core's change tracker.
        // Any entities added/modified before the failure remain tracked in the 'Added'/'Modified'
        // state, so a subsequent SaveChangesAsync (e.g. writing an error ActivityLog) would try to
        // re-persist those rolled-back entities and fail on FK/constraint violations, silently
        // preventing the activity log from being inserted. Clear the tracker so post-rollback
        // saves operate on a clean context.
        _context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
