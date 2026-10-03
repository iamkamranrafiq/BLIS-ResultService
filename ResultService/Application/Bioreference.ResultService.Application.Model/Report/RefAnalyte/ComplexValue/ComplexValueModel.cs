using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model
{
    public class ComplexValueModel
    {
        public string Value { get; set; }
        public string Flag { get; set; }
        public bool IsDropdown { get; set; }
        public int OrderIndex { get; set; }
        //  public DefaultCommentList DefaultComments { get; set; }
    }
}
