using Bioreference.ResultService.Common;
using Bioreference.ResultService.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model
{
    public class AuditModel
    {
        private string _fromValue;
        private string _toValue;

        public string PropertyName { get; set; }
        public string AuditType { get; set; }
        
        /// <summary>
        /// From value with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string FromValue 
        { 
            get => _fromValue;
            set => _fromValue = value.NormalizeToWindows();
        }
        
        /// <summary>
        /// To value with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string ToValue 
        { 
            get => _toValue;
            set => _toValue = value.NormalizeToWindows();
        }
        public string UserName { get; set; }
        public string TestCode { get; set; }
        public string TestName { get; set; }
        public DateTime EventDate { get; set; }
    }
}
