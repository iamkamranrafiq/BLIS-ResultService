using System.Data;
using System.Data.SqlClient;

namespace Bioreference.ResultService.DI.Interface
{
    public static class ChannelCommon
    {
        public static string ArchiveStatus(string msg, string ConnString_ArchiveStatus)
        {
            SqlConnection conn = new SqlConnection(ConnString_ArchiveStatus);
            SqlCommand cmd = null;
            string timestampId = "";

            try
            {
                conn.Open();
                cmd = new SqlCommand
                {
                    Connection = conn,
                    CommandType = CommandType.StoredProcedure,
                    CommandText = "StatusToVertex_Save"
                };
                cmd.Parameters.Add("@Message", SqlDbType.Text).Value = msg;

                SqlDataReader dt = cmd.ExecuteReader();

                dt.Read();
                timestampId = dt["TimeStampID"].ToString();
            }
            catch (Exception ex)
            {
                timestampId = "";
                throw;
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }

            return timestampId;

            // DataSet ds = _channelLogDB.ExecuteSP("StatusToVertex_Save", new string[] { "@Message" }, new object[] { msg });
            // return ds.Tables[0].Rows[0]["TimeStampID"].ToString();
        }
    }
}
