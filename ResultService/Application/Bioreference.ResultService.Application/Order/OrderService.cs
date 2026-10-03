using AutoMapper;
using Bioreference.Common.Client;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Order;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.Order;

namespace Bioreference.ResultService.Application.Order
{
    public class OrderService : IOrderService
    {
        private readonly IMapper _mapper;

        public OrderService(IMapper mapper)
        {
            _mapper = mapper;
        }

        public async Task<OrderModel> FetchOrder(int orderId)
        {
            var returnValue = await Task.Run(()=> OrderManager.FetchOrder(orderId));
            return await AssembleAccountDetails(returnValue);
        }

        public async Task<OrderModel> FetchOrder(string accessionNbr)
        {
            var returnValue = await Task.Run(() => OrderManager.FetchOrder(accessionNbr));
            return await AssembleAccountDetails(returnValue);
        }
        public async Task<OrderModel> FetchOrder(string accessionNbr,DateTime dateServiced)
        {
            var returnValue = await Task.Run(() => OrderManager.FetchOrder(accessionNbr, dateServiced));
            return await AssembleAccountDetails(returnValue);
        }

        public async Task<List<OrderInfoModel>> FetchOrders(string accessionNbr)
        {
            var ordersInfo = await Task.Run(() => Orders.Fetch(accessionNbr));
            List<OrderInfoModel> ordersList = new List<OrderInfoModel>();

            foreach(var ord in ordersInfo.List)
            {
                OrderInfoModel orderInfo = new OrderInfoModel();
                orderInfo.OrderId = ord.Id;
                orderInfo.AccessionNumber = accessionNbr;
                orderInfo.IsReportHold = ord.IsReportHold;
                orderInfo.DateOfService = ord.DateOfService;

                var accountInfo = await Task.Run(() => Account.Fetch(ord.AccountNumber));
                if (accountInfo != null)
                {
                    orderInfo.ClientID = ord.AccountNumber+" - "+accountInfo.AccountName;
                }
                ordersList.Add(orderInfo);
                
            }
            return ordersList;
        }

        private async Task<OrderModel> AssembleAccountDetails(LIS.Order order)
        {
            var response = _mapper.Map<OrderModel>(order);

            if (response != null)
            {
               
                response.Age = GetPatientAge(order);    
                response.Specimens = _mapper.Map<List<SpecimenModel>>(order.Specimens.List);
                response.Tests = _mapper.Map<List<TestModel>>(order.Tests.List);
                response.AOEAnswers = _mapper.Map<List<AOEAnswerModel>>(order.AOEs.List);

                var accountInfo = await Task.Run(() => Account.Fetch(response.AccountNumber));
                if (accountInfo != null)
                {
                    response.AccountName = accountInfo.AccountName;
                }

            }

            return response;
        }

        public string GetPatientAge(LIS.Order order)
        {

            DateTime dob;
            if (DateTime.TryParse(order.Patient.DateOfBirth, out dob) && dob != new DateTime(1900, 1, 1))
            {
                return Bioreference.Utilities.General.GetAge(dob, order.DateOfCollection);
            }
            else if (order.Patient.AgeNumber != 0)
            {
               return $"{order.Patient.AgeNumber} {order.Patient.AgeType}";
            }
            else
            {
                return "";
            }
        }

        public async Task<List<OrderCommentModel>> GetOrderComment(int orderId)
        {
            var order = await Task.Run(() => OrderManager.FetchOrder(orderId)); 
            return MapOrderComments(order);
        }

        public async Task<List<OrderCommentModel>> SaveOrderComment(OrderCommentInputModel input)
        {
            var order = await Task.Run(() => OrderManager.FetchOrder(input.OrderId));

            foreach (var comment in input.Criteria)
            {
                switch (comment.Status)
                {
                    case OrderCommentStatus.Add:
                        order.OrderComments.AddComment(comment.Text);
                        break;
                    case OrderCommentStatus.Delete:
                        order.OrderComments.DeleteComment(comment.Id);
                        break;
                }
            }

            order.Save();
            return MapOrderComments(order);
        }

        private List<OrderCommentModel> MapOrderComments(LIS.Order order)
        {
            var commentModels = new List<OrderCommentModel>();

            if (order == null)
            {
                Console.WriteLine("Order not found.");
                return commentModels;
            }

            var comments = order.OrderComments?.List;
            if (comments == null || comments.Count == 0)
            {
                Console.WriteLine("No comments found for this order.");
                return commentModels;
            }

            foreach (var comment in comments)
            {
                commentModels.Add(_mapper.Map<OrderCommentModel>(comment));
            }
            return commentModels;
        }

    }
}



