using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model.Messaging
{
    public class ReflexMessage
    {
        private string _text;

        public int Id { get; set; }
        
        /// <summary>
        /// Message text with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string Text 
        { 
            get => _text;
            set => _text = value.NormalizeToWindows();
        }
    }
}
