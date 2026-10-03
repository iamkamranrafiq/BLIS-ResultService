using Bioreference.Common;
using Bioreference.LIS;
using Microsoft.VisualBasic.CompilerServices;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace Bioreference.ResultService.Common
{
    public static class Utilities
    {
        public const int NOTEFIXEDWIDTH = 78;

        public static string ConvertResultTypeToString(Bioreference.Common.Lab.resultType oResultType)
        {
            return ConvertResultTypeToString(oResultType, false, false);
        }

        public static string ConvertResultTypeToString(Bioreference.Common.Lab.resultType oResultType, bool isAoe)
        {
            return ConvertResultTypeToString(oResultType, isAoe, false);
        }

        public static string ConvertResultTypeToString(Bioreference.Common.Lab.resultType oResultType, bool isAoe, bool sendAoeAsStType)
        {
            if (isAoe)
            {
                if (sendAoeAsStType)
                {
                    return "ST";
                }
                else
                {
                    return "AE";
                }
            }
            else if (oResultType == Bioreference.Common.Lab.resultType.DecimalFormat)
            {
                return "NM";
            }
            else if (oResultType == Bioreference.Common.Lab.resultType.NumericFormat)
            {
                return "NM";
            }
            else if (oResultType == Bioreference.Common.Lab.resultType.PickListItem)
            {
                return "ST";
            }
            else
            {
                return "ST";
            }
        }

        public static string ConvertStatusToString(Bioreference.LIS.resultStatusType oStatus)
        {
            // If oStatus = Bioreference.LIS.resultStatusType.Pending Then
            // Return "P"
            if (oStatus == Bioreference.LIS.resultStatusType.Final)
            {
                return "F";
            }
            else if (oStatus == Bioreference.LIS.resultStatusType.Corrected)
            {
                return "C";
            }
            else
            {
                return "P";
            } // 'If Pending or Preliminary set to P
        }

        public static string PriorityToString(Bioreference.LIS.OrderPriority oPriority)
        {
            if (oPriority == Bioreference.LIS.OrderPriority.Routine)
            {
                return "R";
            }
            else if (oPriority == Bioreference.LIS.OrderPriority.STAT)
            {
                return "S";
            }
            else
            {
                return "";
            }
        }

        public static string GenderToSex(Gender oGender)
        {
            if (oGender == Gender.Male)
            {
                return "M";
            }
            else if (oGender == Gender.Female)
            {
                return "F";
            }
            else
            {
                return "U";
            }
        }

        public static string FormatFixedWidthText(string text, int maxCharWidth, char breakChar = ' ')
        {
            string formattedComment = "";
            var s = new List<string>();            
            text = text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", "\r\n");

            foreach (string line in text.Split('\r'))
            {
                s.Clear();
                string currentLine = line.Replace("\n", "");  // Create a modifiable copy of 'line'

                if (currentLine.Length > maxCharWidth)
                {
                    int i;
                    while (currentLine.Length > maxCharWidth)
                    {
                        i = maxCharWidth - 1;
                        while (i != 0)
                        {
                            if (currentLine.Substring(i, 1) == breakChar.ToString())
                            {
                                s.Add(string.Concat(currentLine.Substring(0, i), "\r\n"));  // Use "\r\n" instead of Constants.vbCrLf
                                currentLine = currentLine.Substring(i + 1);  // Update 'currentLine' instead of 'line'
                                break;
                            }
                            i -= 1;
                        }
                    }

                    s.Add(string.Concat(currentLine, "\r\n"));

                    foreach (string c in s)
                    {
                        formattedComment = string.Concat(formattedComment, c);
                    }
                    formattedComment = string.Concat(formattedComment, "\r\n");
                }
                else
                {
                    formattedComment = string.Concat(formattedComment, currentLine, "\r\n");
                }
            }

            return formattedComment;
        }

        public static List<string> CreateNoteFromAnalyte(Bioreference.LIS.ReportAnalyte a, int pos = 1)
        {

            string testDescriptionText = "";
            string resultText = "";
            string flagText = "";
            string referenceRangeText = "";
            string unitsText = "";
            bool addlLine = false;
            int startPos;

            var list = new List<string>();

            startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:TestDescriptionWidth")));
            if (a.Analyte.Name.Length > startPos)
                testDescriptionText = a.Analyte.Name.Substring(startPos);
            if (testDescriptionText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:TestDescriptionWidth")))
            {
                testDescriptionText = testDescriptionText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:TestDescriptionWidth")));
                addlLine = true;
            }
            testDescriptionText = testDescriptionText.PadRight((Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:TestDescriptionTitle")).Length, ' ');

            startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ResultWidth")));
            if (a.ResultValue.Length > startPos)
                resultText = a.ResultValue.Substring(startPos);
            if (resultText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ResultWidth")))
            {
                resultText = resultText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ResultWidth")));
                addlLine = true;
            }
            resultText = resultText.PadRight(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ResultTitle").Length, ' ');

            startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:FlagWidth")));
            if (a.FlagValue.Length > startPos)
                flagText = a.FlagValue.Substring(startPos);
            if (flagText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:FlagWidth")))
            {
                flagText = flagText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:FlagWidth")));
                addlLine = true;
            }
            flagText = flagText.PadRight(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:FlagTitle").Length, ' ');

            startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ReferenceRangeWidth")));
            if (a.GetReferenceRange().Length > startPos)
                referenceRangeText = a.GetReferenceRange().Substring(startPos);
            if (referenceRangeText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ReferenceRangeWidth")))
            {
                referenceRangeText = referenceRangeText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ReferenceRangeWidth")));
                addlLine = true;
            }
            referenceRangeText = referenceRangeText.PadRight(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:ReferenceRangeTitle").Length, ' ');

            startPos = (int)Math.Round((pos - 1) * Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:UnitsWidth")));
            if (a.Analyte.Units.Length > startPos)
                unitsText = a.Analyte.Units.Substring(startPos);
            if (unitsText.Length > Convert.ToDouble(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:UnitsWidth")))
            {
                unitsText = unitsText.Substring(0, Convert.ToInt32(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:UnitsWidth")));
                addlLine = true;
            }

            unitsText = unitsText.PadRight(Bioreference.LIS.Configuration.AppSettings.GetString("Bioreference.Iguana.HL7.B2:UnitsTitle").Length, ' ');

            list.Add(string.Concat(testDescriptionText, resultText, flagText, referenceRangeText, unitsText));

            if (addlLine)
            {
                list.AddRange(CreateNoteFromAnalyte(a, pos + 1));
            }

            return list;

        }

        public static string UpdateResultToFollow(Bioreference.LIS.ReportAnalyte analyte, ref DateTime reportDate)
        {
            string resultValue;

            reportDate = analyte.ReleaseDate; // ITBTC-81: Sending current release date, if already has been released
            if (!analyte.HasBeenReleased())
            {
                if ((analyte.ToFollowSent == false && analyte.Analyte.IsReportable) & analyte.Analyte.IsRequired)
                {
                    if (analyte.CodeTypeId == 5) // AOE
                    {
                        resultValue = analyte.ResultValue;
                    }
                    else
                    {
                        resultValue = "TO FOLLOW";
                        reportDate = DateTime.Now;
                    } // ITBTC-81: sending current date to each TO FOLLOW on the order message.
                }
                else
                {
                    resultValue = analyte.ResultValue;
                }     // resultValue = "Pending"
            }
            else
            {
                resultValue = analyte.ResultValue;
            }
            return resultValue;
        }
        public static string UpdateResultToFollow(Bioreference.LIS.ReportAnalyte analyte)
        {
            DateTime reportDate = DateTime.Now;
            return UpdateResultToFollow(analyte, ref reportDate);
        }


        public static string ArchiveStatus(string msg)
        {

            var conn = new SqlConnection(LIS.Configuration.AppSettings.GetString("ConnectionStrings:Bioreference.Iguana.HL7.B2.ConnString_ArchiveStatus"));
            SqlCommand cmd = null;
            string timestampId = "";

            try
            {

                conn.Open();
                cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "StatusToVertex_Save";
                cmd.Parameters.Add("@Message", SqlDbType.Text).Value = msg;

                var dt = cmd.ExecuteReader();

                dt.Read();
                timestampId = Conversions.ToString(dt["TimeStampID"]);
            }

            catch (Exception ex)
            {
                timestampId = "";
                throw ex;
            }
            finally
            {
                if (!(conn == null))
                {
                    conn.Close();
                    conn.Dispose();
                }
                if (!(cmd == null))
                {
                    cmd.Dispose();
                }
            }

            return timestampId;

            // Dim ds As DataSet = _channelLogDB.ExecuteSP("StatusToVertex_Save", New String() {"@Message"}, New Object() {msg})
            // Return ds.Tables(0).Rows(0)("TimeStampID")
        }
        public static void SaveMessageToArchives(string msg, string processName, bool isProcessed, bool isDeleted)
        {
            try
            {
                if (string.IsNullOrEmpty(msg)) return;
                ArchiveMessage archive = new ArchiveMessage(processName, msg, isProcessed, isDeleted);
                archive.Save();
            }
            catch (Exception ex)
            {
                throw ex; 
            }                  
        }
        public static bool CheckMappedHL7JsonError(string json, out string? errorDetail)
        {
            errorDetail = null;     

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("Error", out var errorElement))
                {
                    errorDetail = errorElement.ValueKind == JsonValueKind.String
                        ? errorElement.GetString()
                        : errorElement.ToString();

                    return true;
                }
            }
            catch (JsonException)
            {
                // ignore invalid JSON
            }

            return false;
        }

    }
}
