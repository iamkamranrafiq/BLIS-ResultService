using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model
{
    public class AOEAnswerModel
    {
        private string _question;
        private string _answer;

        /// <summary>
        /// Question text with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string Question 
        { 
            get => _question;
            set => _question = value.NormalizeToWindows();
        }
        
        /// <summary>
        /// Answer text with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string Answer 
        { 
            get => _answer;
            set => _answer = LineEndingHelper.NormalizeToWindows(value);
        }

    }
}
