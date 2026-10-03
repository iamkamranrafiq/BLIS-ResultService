using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Common;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Bioreference.ResultService.Application.Jobs
{
    public class RequisitionStatusJob : IBLISJob
    {
        private ILogger<RequisitionStatusJob> _logger;
        private readonly ISettingService _settingsProvider;
        private DataTable reqData = InitDataTable();
        private RequisitionStatusSetting _requisitionStatusSettings = null;

        public RequisitionStatusJob(ILogger<RequisitionStatusJob> logger, ISettingService settingsProvider)
        {
            _logger = logger;
            _settingsProvider = settingsProvider;
        }

        public async Task InitializeAsync()
        {
            string connection = _settingsProvider.GetConnectionString("Bioreference.LIS");
            _requisitionStatusSettings = await _settingsProvider.FetchSetting<RequisitionStatusSetting>(connection, "RequisitionStatusEngine", null);
        }

        public async Task<bool> Execute()
        {
            await InitializeAsync();

            var sw = Stopwatch.StartNew();          
            try
            {
                List<RequisitionRequest> requestList = GetRequisitionRequest();

                if (requestList != null && requestList.Count > 0)
                {
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Count: {Count}", "RequisitionStatus", "FetchRequests", "Number of requisition requests to process.", requestList.Count);

                    foreach (var request in requestList)
                    {
                        try
                        {
                            ProcessRequest(request);
                            foreach (var accession in request.Accessions)
                            {
                                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "RequisitionStatus", "ProcessAccession", "Processing accession number.", accession);
                            }
                        }
                        catch (Exception reqEx)
                        {
                            _logger.LogError(reqEx, "Entity: {Entity}; Event: {Event}; Message: {Message}", "RequisitionStatus", "ProcessRequest", "Error processing RequisitionRequest.");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}", "RequisitionStatus", "FetchRequests", "No accessions found for fetching requisition.");
                }

                SaveRequisitionDetails();
             
                sw.Stop();
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "RequisitionStatus", "Execute", "Requisition Status Engine completed.", sw.ElapsedMilliseconds);

                return true;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "RequisitionStatus", "Execute", "Error in Requisition Status Engine execution.", sw.ElapsedMilliseconds);
                return false;
            }
        }

        private void SaveRequisitionDetails()
        {
            if (reqData != null && reqData.Rows.Count > 0)
            {
                var batches = CreateBatchesOfData(reqData);
                foreach (var table in batches)
                {
                    SaveBatchData(table);
                }
            }
        }

        private void SaveBatchData(DataTable reqData)
        {
            using (var conn = new SqlConnection(_settingsProvider.GetConnectionString("Bioreference.LIS")))
            {
                try
                {
                    using (var cmd = new SqlCommand
                    {
                        Connection = conn,
                        CommandType = CommandType.StoredProcedure,
                        CommandText = "lis_Order_Requisition_Save",
                        CommandTimeout = 99999
                    })
                    {
                        cmd.Parameters.AddWithValue("@OrderRequisitionTable", reqData).SqlDbType = SqlDbType.Structured;
                        conn.Open();
                        _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; RowsAffected: {RowsAffected}", "RequisitionStatus", "SaveBatch", "Batch saved to database.", cmd.ExecuteNonQuery());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}", "RequisitionStatus", "SaveBatch", "Error saving batch data.");
                }
            }
        }

        private List<DataTable> CreateBatchesOfData(DataTable data)
        {
            var batches = new List<DataTable>();
            var copyTable = data.Copy();
            while (data.Rows.Count != 0)
            {
                var table = new DataTable("BatchData");
                table.Columns.Add("DateOfService");
                table.Columns.Add("AccessionNbr");
                table.Columns.Add("RequisitionDate");

                int limit = 50;
                if (data.Rows.Count <= limit)
                {
                    table = data.Copy();
                    data.Rows.Clear();
                    limit = 0;
                }

                while (limit != 0)
                {
                    var dr = data.Rows[limit - 1];
                    var newRow = table.NewRow();
                    newRow[0] = dr[0];
                    newRow[1] = dr[1];
                    newRow[2] = dr[2];
                    data.Rows.Remove(dr);
                    table.Rows.Add(newRow);
                    limit--;
                }
                batches.Add(table);
            }
            return batches;
        }

        public void ProcessRequest(RequisitionRequest request)
        {
            try
            {
                var requisitionSettings = GetRequisitionSettings();
                _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; RequisitionClientId: {RequisitionClientId}", "RequisitionStatus", "Settings", "Loaded requisition settings.", requisitionSettings.RequisitionClientId);

                var clientManager = new RequisitionClientManager(requisitionSettings);
                var response = clientManager.GetResponse(request);

                if (response != null && response.Accessions != null)
                {
                    if (response.Accessions.Count > 0)
                    {
                        foreach (var responseDetail in response.Accessions)
                        {
                            ActivityHelper.SetAccessionLogKey(responseDetail.AccessionNumber);
                            _logger.LogInformation(
                            "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                            responseDetail.AccessionNumber,
                            DateTime.UtcNow);

                            responseDetail.ItemDate.Sort();
                            var dataRow = reqData.NewRow();
                            dataRow["DateOfService"] = DateTime.Parse(request.DateOfService).ToShortDateString();
                            dataRow["RequisitionDate"] = responseDetail.ItemDate[0];
                            dataRow["AccessionNbr"] = responseDetail.AccessionNumber;
                            reqData.Rows.Add(dataRow);
                            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ItemDate: {ItemDate}", "RequisitionStatus", "ProcessResponse", "Processed accession requisition response.", responseDetail.AccessionNumber, responseDetail.ItemDate[0]);
                        }
                    }

                    if (response.Accessions.Count > 0)
                    {
                        var result = string.Join(",", request.Accessions.Except(response.Accessions.Select(r => r.AccessionNumber)));
                        if (result.Length > 0)
                        {
                            _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; UnprocessedAccessions: {UnprocessedAccessions}", "RequisitionStatus", "Unprocessed", "Unprocessed accessions found.", result);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; UnprocessedAccessions: {UnprocessedAccessions}", "RequisitionStatus", "Unprocessed", "All requested accessions unprocessed.", string.Join(",", request.Accessions));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}", "RequisitionStatus", "ProcessRequest", "Exception while processing requisition request.");
            }
        }

        private RequisitionSettings GetRequisitionSettings()
        {
            return new RequisitionSettings
            {
                RequisitionClientId = _requisitionStatusSettings.RequisitionClientId,
                RequisitionClientSecret = _requisitionStatusSettings.RequisitionClientSecret,
                RequisitionGrantType = _requisitionStatusSettings.RequisitionGrantType,
                RequisitionStatusURL = _requisitionStatusSettings.RequisitionStatusURL,
                RequisitionTokenURL = _requisitionStatusSettings.RequisitionTokenURL
            };
        }

        public List<RequisitionRequest> GetRequisitionRequest()
        {
            var requisitionRequestList = new List<RequisitionRequest>();
            try
            {
                var requisitionData = GetRequisitionData();
                RequisitionRequest reqRequest = null;
                string grpDateServiced = string.Empty;
                var accessionList = new List<string>();

                foreach (DataRow r in requisitionData.Rows)
                {
                    if (string.IsNullOrEmpty(grpDateServiced) || grpDateServiced != r["DateServiced"].ToString())
                    {
                        if (reqRequest != null)
                        {
                            reqRequest.AddAccession(accessionList);
                            requisitionRequestList.Add(reqRequest);
                            accessionList = new List<string>();
                        }
                        grpDateServiced = r["DateServiced"].ToString();
                        reqRequest = new RequisitionRequest { DateOfService = grpDateServiced };
                    }
                    if (r["DateServiced"].ToString() == grpDateServiced)
                    {
                        accessionList.Add(r["Accessionnbr"].ToString());
                    }
                }

                if (reqRequest != null)
                {
                    reqRequest.AddAccession(accessionList);
                    requisitionRequestList.Add(reqRequest);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}", "RequisitionStatus", "GetRequisitionRequest", "Error building requisition request list.");
            }
            return requisitionRequestList;
        }

        private DataTable GetRequisitionData()
        {
            var cmdText = "lis_AllPOCAccessions_Fetch";
            var db_b2 = new DBConnection(_settingsProvider.GetConnectionString("Bioreference.LIS"));
            using (var dataSet = db_b2.ExecuteSP(cmdText, new[] { "@ExcludedHours", "@LookBackHours" }, new object[] { _requisitionStatusSettings.ExcludedHours, _requisitionStatusSettings.LookBackHours }))
            {
                if (dataSet.Tables.Count > 0)
                {
                    return dataSet.Tables[0];
                }
            }
            return null;
        }

        private static DataTable InitDataTable()
        {
            var table = new DataTable("MainTable");
            table.Columns.Add("DateOfService");
            table.Columns.Add("AccessionNbr");
            table.Columns.Add("RequisitionDate");
            return table;
        }
    }
}
