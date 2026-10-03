using Bioreference.ResultService.Application.Model.Enum;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model
{
    public class RapidResultAnalyteModel
    {
        private string _commentsToAdd;

        public string PerformingFacility { get; set; }

        public string AccessioningFacility { get; set; }

        public int RowNumber { get; set; }

        public bool IsControl { get; set; }
        public bool Has4kTest { get; set; }

        /// <summary>
        /// Comments to add with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string CommentsToAdd 
        { 
            get => _commentsToAdd;
            set => _commentsToAdd = value.NormalizeToWindows();
        }

        public int SortOrder { get; set; }

        public int Id { get; set; }

        public long ReportAnalyteId { get; set; }

        public int ReportId { get; set; }

        public string AnalyteCode { get; set; }

        public string AnalyteName { get; set; }

        public string AccessionNbr { get; set; }

        public string AccessionIdentifier { get; set; }

        public int AccountStatusLevel { get; set; }

        public string ResultValue { get; set; }

        public bool IsDeactivated { get; set; }

        public resultStatusTypeModel ResultStatus { get; set; }

        public transmitStatusTypeModel TransmitStatus { get; set; }

        public RefAnalyteModel Analyte { get; set; }

        public string DOB { get; set; }

        public int AgeNbr { get; set; }

        public string AgeType { get; set; }

        public GenderModel Gender { get; set; }

        public string PatientName { get; set; }

        public DateTime DateServiced { get; set; }

        public bool IsReference { get; set; }

        public ReportAnalyteHistoryModel[] ResultHistory { get; set; }
    }

}
