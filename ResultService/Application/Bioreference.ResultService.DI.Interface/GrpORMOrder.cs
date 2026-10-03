using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.DI.Interface
{
    public class GrpORMOrder
    {
        public List<Tx> Tx { get; set; }
        public List<OBX> Result { get; set; }
    }
}
