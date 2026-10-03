using Bioreference.Contracts.Result;
using Bioreference.LIS;
using Bioreference.Messaging.Abstractions;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model.AppSettings.Reflex;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Bioreference.ResultService.Application.Jobs
{
    public class ReflexesJob : IBLISJob
    {
        private ILogger<ReflexesJob> logger;
        private readonly IMessageProducer<Reflex> producer;
        private readonly ISettingService settingsProvider;
        private readonly AppSettingsReflex appSettings;
        private ReflexSettings settings = null;
        private string connectionString = string.Empty;

        public ReflexesJob(ILogger<ReflexesJob> logger, IProducerProvider provider, ISettingService settingsProvider, IOptions<AppSettingsReflex> options)
        {
            this.logger = logger;
            this.producer = provider.GetMessageProducer<Reflex>();
            this.settingsProvider = settingsProvider;
            this.appSettings = options.Value;
        }

        public async Task<bool> Execute()
        {
            bool isSuccess = false;
            var sw = Stopwatch.StartNew();

            try
            {                
                connectionString = settingsProvider.GetConnectionString("Bioreference.LIS");

                settings = await settingsProvider.FetchSetting<ReflexSettings>(connectionString, "B2ReflexesToSPM", "");
                var igeCodes = settings.SuppressIGEReflexCodes;
                var tnpCodes = settings.SuppressTNPReflexCodes;
                string igeCode = settings.IGECode;

                OutboundMessages reflexes = OutboundMessages.Fetch(outBoundMessageSendTo.Vertex, false);
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; FetchCount: {FetchCount}", "Reflexes", "Fetch", "Fetched outbound reflex messages.", reflexes.List.Length);

                foreach (OutboundMessage msg in reflexes.List)
                {
                    try
                    {
                        logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; RawMessage: {RawMessage}", "Reflexes", "ProcessMessage", "Processing outbound reflex message.", msg.AccessionNumber, msg.Message);

                        string[] tokens = msg.Message.Split(new char[] { '|' }, StringSplitOptions.None);
                        
                        // As the Case is handled for all data
                        //if (tokens.Length < 5)
                        //{
                        //    logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; RawMessage: {RawMessage}", "Reflexes", "Validation", "Skipping invalid reflex message (insufficient tokens).", msg.AccessionNumber, msg.Message);
                        //    msg.MarkAsProcessed();
                        //    msg.Save();
                        //    continue;
                        //}
                       
                        Reflex reflex = new Reflex()
                        {
                            OriginalAccessionNumber = GetToken(tokens, 0),
                            AccessionNumber = GetToken(tokens, 0),
                            ReflexCode = GetToken(tokens, 1),
                            ActionType = GetToken(tokens, 3),
                            OrderedCode = GetToken(tokens, 4) 
                        };


                        ActivityHelper.SetAccessionLogKey(reflex.AccessionNumber);
                        logger.LogInformation(
                        "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                        reflex.AccessionNumber,
                        DateTime.UtcNow);

                        if (reflex.AccessionNumber.Length == 7) reflex.AccessionNumber = $"10{reflex.AccessionNumber}";

                        logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; OrderedCode: {OrderedCode}; ReflexCode: {ReflexCode}", "Reflexes", "Parse", "Parsed reflex details.", reflex.AccessionNumber, reflex.OrderedCode, reflex.ReflexCode);

                        if (reflex.ReflexCode == igeCode && igeCodes.Contains(reflex.OrderedCode))
                        {
                            if (Bioreference.LIS.Report.IsAnalyteTNP(reflex.OriginalAccessionNumber, reflex.OrderedCode))
                            {
                                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; OrderedCode: {OrderedCode}; ReflexCode: {ReflexCode}", "Reflexes", "SuppressIGE", "Ordered allergen is TNP, suppressing IGE reflex.", reflex.AccessionNumber, reflex.OrderedCode, reflex.ReflexCode);
                                continue;
                            }
                        }

                        if (tnpCodes.Contains(reflex.OrderedCode))
                        {
                            if (Bioreference.LIS.Report.IsAnalyteTNP(reflex.OriginalAccessionNumber, reflex.OrderedCode))
                            {
                                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; OrderedCode: {OrderedCode}; ReflexCode: {ReflexCode}", "Reflexes", "SuppressTNP", "Ordered code is TNP, suppressing reflex.", reflex.AccessionNumber, reflex.OrderedCode, reflex.ReflexCode);
                                continue;
                            }
                        }

                        logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "Reflexes", "TNPCheck", "TNP check complete.", reflex.AccessionNumber);

                        await ProduceMessage(reflex);
                        SaveReflex(reflex);

                        msg.MarkAsProcessed();
                        msg.Save();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; RawMessage: {RawMessage}", "Reflexes", "ProcessMessage", "Error processing reflex message.", msg.AccessionNumber, msg.Message);
                    }
                }

                isSuccess = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}", "Reflexes", "Execute", "Error processing Reflexes job.");
            }
            finally
            {
                sw.Stop();
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime}", "Reflexes", "Execute", "Reflexes job completed.", sw.ElapsedMilliseconds);
            }

            return isSuccess;
        }
        string GetToken(string[] arr, int index)
        {
            return arr.Length > index ? arr[index] : string.Empty;
        }

        private async Task ProduceMessage(Reflex reflex)
        {
            if (reflex == null) return;
            
            var messageWrapper = new Message<Reflex>
            {
                Payload = reflex,
                Key = reflex.AccessionNumber,
                MessageId = Guid.NewGuid().ToString(),
            };

            messageWrapper.AddHeader("log_key", reflex.AccessionNumber);
            
            MessagePartitionInfo result = await producer.ProduceAsync(messageWrapper);

            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; Partition: {Partition}; Offset: {Offset}", "Reflexes", "ProduceMessage", "Message produced.", reflex.AccessionNumber, result.Partition, result.Offset);
        }

        private void SaveReflex(Reflex reflex)
        {
            try
            {
                
                List<SqlParameter> p =
                [
                    new SqlParameter("@AccessionNbr", reflex.OriginalAccessionNumber),
                new SqlParameter("@ReflexAnalyte", reflex.ReflexCode)
                ];

                using (SqlConnection cn = new SqlConnection() { ConnectionString = connectionString })
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand("lis_ReportAnalyte_SaveReflex", cn)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure,
                        CommandTimeout = 300
                    })
                    {
                        cmd.Parameters.AddRange(p.ToArray());
                        cmd.ExecuteNonQuery();
                    }
                    cn.Close();
                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}", "Reflexes", "SaveReflex", "Reflex saved.", reflex.AccessionNumber);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Accession: {Accession}; ReflexCode: {ReflexCode}", "Reflexes", "SaveReflex", "Exception saving reflex.", reflex.AccessionNumber, reflex.ReflexCode);
            }
        }
    }

}
