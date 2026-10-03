using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class ComplexValuesModel
    {
        public ComplexValueListModel List { get; set; }
        public List<ComplexValueModel> DropdownList { get; set; }

    }
}
