using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bioreference.ScanningService.Application.DTOs.Mappers;

public static class DocumentMapper
{
    public static DocumentDetailDto ToDetailDto(this Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new DocumentDetailDto
        {
            Id = document.DocumentId,
            Name = document.Name,
            ShortName = document.ShortName,
            BarcodeValue = document.BarcodeValue,
            SpecimenNumber = document.SpecimenNumber,
            IndexingValue = document.IndexingValue,
            // DocUrl is not always populated on the document row; fall back to the first page's
            // file path (PageURL) so callers get the actual document file location.
            DocUrl = !string.IsNullOrWhiteSpace(document.DocUrl)
                ? document.DocUrl
                : document.DocumentPages?
                    .Where(p => !p.IsDeleted && !string.IsNullOrWhiteSpace(p.PageURL))
                    .OrderBy(p => p.DocumentPageId)
                    .Select(p => p.PageURL)
                    .FirstOrDefault(),
            ThumbnailUrl = document.ThumbnailUrl,
            DocumentDate = document.DocumentDate,
            DatePosted = document.DatePosted,
            Status = document.Status,
            IsIndexed = document.IsIndexed,
            TotalPages = document.TotalPages,
            IsActive = document.IsActive,
            CreatedDate = document.CreatedDate,
            CreatedBy = document.CreatedBy,
            UpdatedDate = document.UpdatedDate,
            UpdatedBy = document.UpdatedBy,

            DocumentType = MapDocumentType(document.DocumentType),

            DocumentGroup = MapDocumentGroup(document.DocumentType?.Group),

            Batch = MapBatch(document.Batch),

            Keywords = MapKeywords(document.DocumentKeywordValues)
        };
    }

    private static DocumentTypeInfoDto MapDocumentType(DocumentType? documentType)
    {
        if (documentType == null)
        {
            return new DocumentTypeInfoDto();
        }

        return new DocumentTypeInfoDto
        {
            Id = documentType.DocumentTypeId,
            Code = documentType.Code,
            DisplayName = documentType.DisplayName
        };
    }

    private static DocumentGroupInfoDto MapDocumentGroup(DocumentTypeGroup? group)
    {
        if (group == null)
        {
            return new DocumentGroupInfoDto();
        }

        return new DocumentGroupInfoDto
        {
            Id = group.DocumentTypeGroupId,
            Name = group.Name
        };
    }

    private static BatchInfoDto? MapBatch(Batch? batch)
    {
        if (batch == null)
        {
            return null;
        }

        return new BatchInfoDto
        {
            Id = batch.BatchId,
            BatchNumber = batch.BatchNumber,
            Name = batch.Name,
            Status = batch.BatchStatus?.Name ?? string.Empty
        };
    }

    private static List<KeywordValueDto> MapKeywords(IEnumerable<DocumentKeywordValue> keywordValues)
    {
        return keywordValues
            .Where(x => x.Keyword != null)
            .Select(x => new KeywordValueDto
            {
                DocumentKeywordValueId = x.DocumentKeywordValueId,
                KeywordId = x.KeywordId,
                KeywordName = x.Keyword!.Name,
                AlphanumericValue = x.AlphanumericValue,
                DateTimeValue = x.DateTimeValue,
                DecimalValue = x.DecimalValue,
                LongValue = x.LongValue,
                TextValue = x.TextValue,
                DataType = x.Keyword.DataType,
                ControlType = x.Keyword.ControlType,
                IsRequired = x.Keyword.IsRequired
            })
            .OrderBy(x => x.KeywordName)
            .ToList();
    }
}
