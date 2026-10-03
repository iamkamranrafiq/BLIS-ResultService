using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model.Tools
{
    public class BulkTNPReleaseModel
    {
        private string _comment;

        public string AccessionNumber { get; set; }
        public string TestCode { get; set; }
        public string PanelCode { get; set; }
        public string CommentType { get; set; }
        
        /// <summary>
        /// Comment with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string Comment 
        { 
            get => _comment;
            set => _comment = value.NormalizeToWindows();
        }

    }
}
