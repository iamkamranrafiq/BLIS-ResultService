using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model
{
    public class AttachmentModel
    {
        private string _attachmentDescription;

        public long Id { get; set; }
        public int AttachmentTypeId { get; set; }
        public string AttachmentTypeDescription { get; set; }
        
        /// <summary>
        /// Attachment description with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string AttachmentDescription 
        { 
            get => _attachmentDescription;
            set => _attachmentDescription = value.NormalizeToWindows();
        }
        public object IdentifierId { get; set; }
        public byte[] FileContents { get; set; }
        public string FileContentsBase64 { get; set; }
        public List<string> GetFormattedAuditItems { get; }

    }
}
