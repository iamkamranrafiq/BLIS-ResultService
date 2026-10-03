using AutoMapper;
using Bioreference.Common.Client;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Order;
using Bioreference.ResultService.Application.Model;

namespace Bioreference.ResultService.Application.Order
{
    public class OrderSearchService: IOrderSearchService
    {
        private readonly IMapper _mapper;

        public OrderSearchService(IMapper mapper)
        {
            _mapper = mapper;
        }

        public async Task<List<OrderSearchModel>> Search(OrderSearchCriteria input)
        {
            ReportSearch search = new ReportSearch();
            search.AccessionNumber = input.AccessionNumber;
            search.AccountNumber = input.AccountNumber;
            search.PatientName = input.PatientName;
            search.StartDate = input.StartDate;
            search.EndDate = input.EndDate;
            search.TransmitStatus = input.TransmitStatus ==0 ? transmitStatusType.NotSet: (transmitStatusType)input.TransmitStatus;
            search.UserDivisionCodes = input.UserDivisionCodes;
            search.EUID = input.EUID == 0? -1: input.EUID;
            Reports returnValue = await Task.Run(()=> search.Execute());
            List<OrderSearchModel> orderSearchResponse = _mapper.Map<List<OrderSearchModel>>(returnValue.List);

            //BLISS-TODO- IMPORTANT - Refactor the code to remove the loop and include the fields in the stored procedure.
            foreach (OrderSearchModel orderSearchOutput in orderSearchResponse)
            {
                var orderInfo = await Task.Run(()=> OrderManager.FetchOrder(orderSearchOutput.OrderId));
                orderSearchOutput.CollectionDate = orderInfo.DateOfCollection;
                orderSearchOutput.ServiceDate = orderInfo.DateOfService;
                orderSearchOutput.IsReportHold = orderInfo.IsReportHold;

                var accountInfo = await Task.Run(() => Account.Fetch(orderInfo.AccountNumber));
                if (accountInfo != null)
                {
                 orderSearchOutput.ClientID = orderInfo.AccountNumber +"-"+ accountInfo.AccountName;
                }
            }
            return orderSearchResponse;
        }
    }
}
