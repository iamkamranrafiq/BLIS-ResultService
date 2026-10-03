using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.Order;


namespace Bioreference.ResultService.Abstractions.Application.Order
{
    public interface IOrderService
    {
        public Task<OrderModel> FetchOrder(int orderId);
        public Task<OrderModel> FetchOrder(string accessionNbr);
        public string GetPatientAge(LIS.Order order);
        public Task<List<OrderCommentModel>> GetOrderComment(int orderId);
        public Task<List<OrderCommentModel>> SaveOrderComment(OrderCommentInputModel input);
        public Task<List<OrderInfoModel>> FetchOrders(string accessionNbr);
        public Task<OrderModel> FetchOrder(string accessionNbr, DateTime dateServiced);

    }
}
