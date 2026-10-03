using Bioreference.Data.Audit;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Globalization;
using System.Transactions;

namespace Bioreference.ResultService.Application.Processor
{
    public class InboundAuditProcessor :IInboundAuditProcessor
    {
        private ILogger<InboundAuditProcessor> _logger;

        public InboundAuditProcessor(ILogger<InboundAuditProcessor> logger)
        {
            _logger = logger;

        }

        public void ProcessMessage(string flatWire)
        {
            string dateTimeSt;
            string identifier;
            string objectName;
            auditActionType type;
            string parentIdentifierId;
            parentIdentifierType parentIdentifierType;
            long objLookupId;
            int objLookupType;
            string userName;
            string propertyGroups;
            string auditMessage;

            string propertyName;
            string propertyPreviousValue;
            string propertyCurrentValue;
            string propertyType;
            bool propertyIsPriority;

            int nTotalRecords = 0;

            try
            {
                AuditItems objAudit;

                _logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; RequestUrl: {RequestUrl}",
                    "InboundAudit",
                    "Request",
                    flatWire
                );

                string[] sLines = flatWire.Split(new[] { "\r\n" }, StringSplitOptions.None);

                var options = new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = new TimeSpan(0, 2, 0)
                };

                using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
                {
                    objAudit = AuditItems.CreateAuditItems();

                    foreach (string line in sLines)
                    {
                        if (string.IsNullOrEmpty(line.Trim()) || !line.Contains("|"))
                            continue;

                        nTotalRecords++;

                        string[] sFields = line.Split('|');

                        if (sFields.Length < 11)
                        {
                            throw new Exception("Audit record: " + nTotalRecords + " does not have the proper number of fields.  Bypassing message.");
                        }

                        try
                        {
                            dateTimeSt = sFields[0];
                            type = (auditActionType)Convert.ToInt32(sFields[1]);
                            identifier = sFields[2];
                            objectName = sFields[3];
                            userName = sFields[4];
                            auditMessage = sFields[5];
                            parentIdentifierId = sFields[6];
                            parentIdentifierType = (parentIdentifierType)int.Parse(sFields[7]);
                            objLookupId = long.Parse(sFields[8]);
                            objLookupType = int.Parse(sFields[9]);
                            propertyGroups = sFields[10];
                            ActivityHelper.SetAccessionLogKey(identifier);
                            _logger.LogInformation(
                                "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                                identifier,
                                DateTime.UtcNow
                            );
                        }
                        catch
                        {
                            throw new Exception("Audit record: " + nTotalRecords + " unable to parse audit record.  Bypassing message.");
                        }

                        var objAuditItem = objAudit.AddAuditItem(DateTime.Parse(dateTimeSt, new CultureInfo("en-US")), identifier, objectName, type, parentIdentifierId, parentIdentifierType, objLookupId, objLookupType, userName, auditMessage);

                        if (propertyGroups.Trim().Length > 0)
                        {
                            string[] propertyGroupList = propertyGroups.Split('~');

                            foreach (string pGroup in propertyGroupList)
                            {
                                string[] aiProperties = pGroup.Split('^');

                                if (aiProperties.Length == 5)
                                {
                                    propertyName = aiProperties[0];
                                    propertyPreviousValue = aiProperties[1];
                                    propertyCurrentValue = aiProperties[2];
                                    propertyType = aiProperties[3];
                                    propertyIsPriority = aiProperties[4] == "1";

                                              _logger.LogDebug(
                                                "Entity: {Entity}; Event: {Event};  Property: {Property}; PropertyType: {PropertyType} PreviousValue: {PreviousValue}; CurrentValue: {CurrentValue}",
                                              "InboundAudit",
                                              "AuditProperties",
                                              propertyName,propertyType,propertyPreviousValue,propertyCurrentValue);

                                    if (propertyName != null && propertyType != null)
                                    {
                                        objAuditItem.CreateProperty(propertyName, propertyType, propertyPreviousValue, propertyCurrentValue, propertyIsPriority);
                                    }
                                }
                            }
                        }
                    }

                    // Start stopwatch for database save
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    objAudit.Save();
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "Entity: {Entity}; Event: {Event}; ElapsedTime: {ElapsedTime} ms; Message: {Message}",
                        "InboundAudit",
                        "Save",
                        stopwatch.ElapsedMilliseconds,
                        "Added Successfully"
                    );

                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; Message: {Message}",
                    "InboundAudit",
                    "Error",
                    ex.Message
                );
            }
        }
    }
}
