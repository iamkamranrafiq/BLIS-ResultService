using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model.Lookup.InternalNote
{
    public class InternalNoteModel
    {
        private string _note;

        /// <summary>
        /// Note with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string Note 
        { 
            get => _note;
            set => _note = value.NormalizeToWindows();
        }
        public int Id { get; set; }
    }
}
