using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Bioreference.ScanningService.Application.DTOs.Requests
{
    internal class DocumentRequests
    {
    }
    public class DocumentTypeKeywordsRequest
    {
        public List<int> DocumentTypeIds { get; set; } = new();
    }

    /// <summary>
    /// Document search request matching the UI search form structure
    /// </summary>
    public class DocumentSearchRequest
    {
        /// <summary>
        /// Optional: Document Type Group ID filter (single selection)
        /// Example: "Billing Documents", "Medical Records", etc.
        /// </summary>
        public int? DocumentTypeGroupId { get; set; }

        /// <summary>
        /// List of Document Type IDs to filter by (multiple selection checkboxes)
        /// OR logic - matches ANY selected type
        /// Example: ["DOC - EOB'S", "DOC - DCC'S"]
        /// </summary>
        public List<int>? DocumentTypeIds { get; set; }

        /// <summary>
        /// From Date - start of date range filter
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// To Date - end of date range filter
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Dynamic keyword search criteria (AND logic - ALL criteria must match)
        /// These fields appear dynamically based on selected document types
        /// Examples: EOB Batch Number, Deposit Date, Initials, Date Posted, etc.
        /// </summary>
        public List<KeywordSearchCriteria>? KeywordCriteria { get; set; }

        public PagedRequest PagedRequest { get; set; }
        public string? OrderBy { get; set; } = "DatePosted";
        public string? SortBy { get; set; } = "desc";

        public QueryState? NextToken { get; set; }
    }

    /// <summary>
    /// Keyword search criteria for filtering documents by keyword values
    /// Represents dynamic keyword fields like "EOB Batch Number", "Initials", "Date Posted", etc.
    /// </summary>
    public class KeywordSearchCriteria
    {
        /// <summary>
        /// The Keyword ID to search
        /// </summary>
        public int KeywordId { get; set; }
        public int? TableType { get; set; }

        /// <summary>
        /// The value to search for (partial match using CONTAINS)
        /// Can be text, date, or number based on keyword data type
        /// </summary>
        public string KeywordValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Legacy support - will be deprecated
    /// </summary>
    [Obsolete("Use DocumentSearchRequest with DocumentTypeIds and KeywordCriteria instead")]
    public class DocumentTypeRequest
    {
        public int DocumentTypeId { get; set; }
        public int KeywordId { get; set; }
        public string Keywordvalue { get; set; } = string.Empty;
    }


}
