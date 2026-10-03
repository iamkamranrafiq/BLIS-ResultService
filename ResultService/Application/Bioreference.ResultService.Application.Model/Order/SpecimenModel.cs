using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model
{
    public class SpecimenModel
    {
        private string _description;

        public string Code { get; set; }
        
        /// <summary>
        /// Description with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string Description 
        { 
            get => _description;
            set => _description = value.NormalizeToWindows();
        }
        public int Qty { get; set; }
        public string Temp { get; set; }
        public string Flag { get; set; }
    }
}
