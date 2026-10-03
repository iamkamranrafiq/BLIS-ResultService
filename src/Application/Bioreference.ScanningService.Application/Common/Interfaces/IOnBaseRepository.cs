using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.Common.Interfaces;

public interface IOnBaseRepository
{
    Task<OnBaseDocumentSearchResponse> SearchDocumentAsync(DocumentSearchRequest request);
    Task<DocumentDetailDto> GetDocumentByIdAsync(long documentId);
    Task<Document> GetDocumentDetailByIdAsync(long documentId);
    Task<OnBaseBatchListResponse> SearchBatchsAsync(BatchListRequest request);
    Task<OnBaseBatchDocumentListResponse> GetDocumentByBatchIdAsync(int batchId);
}
