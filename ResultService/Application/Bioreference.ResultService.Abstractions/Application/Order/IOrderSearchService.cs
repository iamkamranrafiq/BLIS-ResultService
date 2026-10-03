using Bioreference.ResultService.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Abstractions.Application.Order
{
    public interface IOrderSearchService
    {
        public Task<List<OrderSearchModel>> Search(OrderSearchCriteria input);
    }
}
