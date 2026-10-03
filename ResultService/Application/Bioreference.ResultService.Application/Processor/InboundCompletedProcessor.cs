using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Bioreference.ResultService.Application.Processor
{
    public class InboundCompletedProcessor: IInboundCompletedProcessor
    {
        private ILogger<InboundCompletedProcessor> _logger;
        private readonly AppSettingsInboundCompleted _appSettings;

        public InboundCompletedProcessor(ILogger<InboundCompletedProcessor> logger, IOptions<AppSettingsInboundCompleted> options)
        {
            _logger = logger;
            _appSettings = options.Value;
        }

        public async Task ProcessMessage(string flatWire)
        {
            try
            {
                // Trick the JSON
                flatWire = flatWire.Replace(",}", "}");

                var rptArr = JsonConvert.DeserializeObject<List<ReportCompletedEntity>>(flatWire);

                foreach (var rpt in rptArr)
                {
                    ActivityHelper.SetAccessionLogKey(rpt.AccessionNumber.ToString());
                    _logger.LogInformation(
                        "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                        rpt.AccessionNumber.ToString(),
                        DateTime.UtcNow
                    );

                    var rptCompleted = new Bioreference.LIS.ReportCompleted
                    {
                        AccessionNumber = rpt.AccessionNumber,
                        Status = rpt.Status,
                        FinalReportDate = rpt.FinalReportDate,
                        ServiceDate = rpt.ServiceDate
                    };

                    await Task.Run(() => rptCompleted.Update());
                }
            }
            catch (Exception ex)
            {
                // Save message to FailedMessages
                //SaveFailedMessage(flatWire);
                ResultService.Common.Utilities.SaveMessageToArchives(flatWire, "ResultCompleted", false, false);
                _logger.LogError(ex.ToString());
            }
        }
        private void SaveFailedMessage(string msg)
        {
            string timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string fileName = string.Concat(_appSettings.AuditChannelPath, "FailedMessages\\", _appSettings.AuditFilePrefix, timeStamp, ".txt");

            try
            {
                _logger.LogDebug("Writing Failed File:" + fileName);

                using (StreamWriter sw = new StreamWriter(fileName, true))
                {
                    sw.WriteLine(msg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to write Failed File : " + fileName + ex.ToString());
            }
        }
        public class ReportCompletedEntity
        {
            public string AccessionNumber { get; set; }
            public long ReportId { get; set; }
            public string Status { get; set; }
            public DateTime FinalReportDate { get; set; }
            public DateTime ServiceDate { get; set; }
        }
    }
}
