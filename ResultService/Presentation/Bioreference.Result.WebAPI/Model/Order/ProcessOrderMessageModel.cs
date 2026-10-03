namespace Bioreference.ResultService.WebAPI.Model
{
    public class ProcessOrderMessageModel
    {
        public string AccessionNumber { get; set; }
        public string OrderId { get; set; }
        public string QueueRouteId { get; set; }
        public string OrderHistoryId { get; set; }
        public QueueInfoModel Queue { get; set; }
        public string MessageId { get; set; }
        public string Payload { get; set; }
    }

    public class QueueInfoModel
    {
        public string QueueName { get; set; }
        public string QueueId { get; set; }
        public string Destination { get; set; }
        public string InternalFlag { get; set; }
    }
}

