using Bioreference.ResultService.Common;
using Bioreference.ResultService.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model
{
    public class SpecimenRequestModel
    {
        private string _comments;

        /// <summary>
        /// Comments with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string Comments 
        { 
            get => _comments;
            set => _comments = value.NormalizeToWindows();
        }

        public bool IsMasterRequired { get; set; }

        public int FridgeId { get; set; }

        public int PriorityTypeID { get; set; }

        public int RequestDeptID { get; set; }

        public string RequestedBy { get; set; }

        public string SpecimenNumbers { get; set; }

        public int SpecimenTypeID { get; set; }

        public int Quantity { get; set; }

    }
}
