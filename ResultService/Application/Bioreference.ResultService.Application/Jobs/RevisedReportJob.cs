using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Common;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Net.Mail;

namespace Bioreference.ResultService.Application
{
    public class RevisedReportJob : IBLISJob
    {
        private ILogger<RevisedReportJob> _logger;
        private readonly ISettingService _settingsProvider;
        private RevisedReportEngineSetting _revisedReportSettings = null;
        private DBConnection db_b2 = null;

        public RevisedReportJob(ILogger<RevisedReportJob> logger, ISettingService settingsProvider, IOptions<AppSettingsInboundEngine> options)
        {
            _logger = logger;
            _settingsProvider = settingsProvider;
        }

        public async Task InitializeAsync()
        {
            string connection = _settingsProvider.GetConnectionString("Bioreference.LIS");
            db_b2 = new DBConnection(connection);
            _revisedReportSettings = await _settingsProvider.FetchSetting<RevisedReportEngineSetting>(connection, "RevisedReportEngine");
        }

        public async Task<bool> Execute()
        {
            await InitializeAsync();
            var sw = Stopwatch.StartNew();
            using (var revisedReportData = await GetRevisedReportData())
            {
                GenerateEmailMessageAndSend(revisedReportData);
            }
            sw.Stop();
            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "RevisedReport", "Execute", "Revised Report job completed.", sw.ElapsedMilliseconds);
            return true;
        }

        private async Task<DataTable> GetRevisedReportData()
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}", "RevisedReport", "GetRevisedReportData", "Starting GetRevisedReportData.");
            try
            {
                string cmdText = "lis_CorrectedReasonDetail_Fetch";
                var lastExec = await GetLastExecDateTime();
                using var dataSet = await Task.Run(() => db_b2.ExecuteSP(cmdText, new string[] { "@StartDateTime", "@UpdateProcess" }, new object[] { lastExec, "1" }));
                if (dataSet.Tables.Count > 0)
                {
                    var table = dataSet.Tables[0];
                    sw.Stop();
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; RowCount: {RowCount}; ElapsedTime: {ElapsedTime} ms", "RevisedReport", "GetRevisedReportData", "Stored procedure returned rows.", table.Rows.Count, sw.ElapsedMilliseconds);
                    return table;
                }
             }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "RevisedReport", "GetRevisedReportData", "Error occurred while fetching revised report data.", sw.ElapsedMilliseconds);
                throw;
            }
            sw.Stop();
            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "RevisedReport", "GetRevisedReportData", "GetRevisedReportData completed with no data.", sw.ElapsedMilliseconds);
            return null;
        }

        private async Task<DateTime> GetLastExecDateTime()
        {
            DateTime lastExecutionDateTime = new DateTime(SqlDateTime.MinValue.Value.Year, SqlDateTime.MinValue.Value.Month, SqlDateTime.MinValue.Value.Day);
            using (var dsB2 = await Task.Run(() => db_b2.ExecuteSP("lis_B2Process_fetch")))
            {
                if (dsB2.Tables.Count > 0 && dsB2.Tables[0].Rows.Count > 0) lastExecutionDateTime = (DateTime)dsB2.Tables[0].Rows[0]["ExecutionDateTime"];
            }
            return lastExecutionDateTime;
        }

        private void GenerateEmailMessageAndSend(DataTable revisedReportData)
        {
            var sw = Stopwatch.StartNew();
           try
            {

                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                var subjectPrefix = string.IsNullOrWhiteSpace(environment)
                    ? string.Empty
                    : $"[{environment}] ";

                var subject = $"{subjectPrefix}{_revisedReportSettings.RevisedReportMailSubject}";


                bool hasData = revisedReportData != null && revisedReportData.Rows.Count > 0;             
                using var emailMessage = new MailMessage(_revisedReportSettings.RevisedReportFromEmail, _revisedReportSettings.RevisedReportToEmail) { Subject = subject, IsBodyHtml = true, Body = string.Format(_revisedReportSettings.RevisedReportMailBody, hasData ? "Please find attached file herewith." : "No attachment because there is no data.") };

                if (hasData)
                {
                    using var xlWorkbook = new XLWorkbook();
                    xlWorkbook.Worksheets.Add(revisedReportData, "RevisedReport");
                    xlWorkbook.Worksheet(1).Row(1).Style.Font.Bold = true;
                    var attachmentStream = new MemoryStream();
                    xlWorkbook.SaveAs(attachmentStream);
                    attachmentStream.Position = 0;
                    var attachmentName = $"RevisedReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    emailMessage.Attachments.Add(new System.Net.Mail.Attachment(attachmentStream, attachmentName, "application/vnd.ms-excel"));
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; AttachmentName: {AttachmentName}", "RevisedReport", "GenerateEmail", "Attachment created.", attachmentName);
                }

                SharedFunctions.SendMail(emailMessage);
                sw.Stop();
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "RevisedReport", "GenerateEmail", "Email sent successfully.", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "RevisedReport", "GenerateEmail", "Error occurred while generating or sending revised report email.", sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
