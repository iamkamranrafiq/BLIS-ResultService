using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Collections.Specialized;

namespace Bioreference.ResultService.Common
{
    public class DBConnection : IDisposable
    {
        private SqlConnection con;
        private string _cnStr;

        public DBConnection()
        {
        }

        public DBConnection(string cnStr)
        {
            _cnStr = cnStr;
        }

        #region Connect/Disconnect Methods

        internal void Connect()
        {
            con = new SqlConnection();
            con.ConnectionString = _cnStr;
            con.Open();
        }

        public void Connect(string sConstrName)
        {
            con = new SqlConnection();
            con.ConnectionString = ConfigurationManager.ConnectionStrings[sConstrName].ConnectionString;
            con.Open();
        }

        public void Disconnect()
        {
            con.Close();
            con.Dispose();
            con = null;
        }

        #endregion

        #region Execute Methods

        public DataSet ExecuteSP(string cmdText, string[] paramNames, object[] paramValues)
        {
            Connect();
            DataSet dSet = new DataSet();
            using (SqlDataAdapter adapter = new SqlDataAdapter())
            {
                using (SqlCommand cmd = new SqlCommand(cmdText, con))
                {
                    if (paramNames != null && paramNames.Length > 0)
                    {
                        for (int i = 0; i < paramNames.Length; i++)
                        {
                            SqlParameter param = new SqlParameter
                            {
                                ParameterName = paramNames[i],
                                Direction = ParameterDirection.Input,
                                Value = paramValues[i]
                            };
                            cmd.Parameters.Add(param);
                        }
                    }
                    cmd.CommandTimeout = 99999;
                    cmd.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand = cmd;
                    adapter.Fill(dSet);
                }
            }
            Disconnect();
            return dSet;
        }

        public void ExecuteSP_WithOutputParams(string cmdText, string[] paramNames, object[] paramValues, NameValueCollection outputParams)
        {
            Connect();
            using (SqlCommand cmd = new SqlCommand(cmdText, con))
            {
                if (paramNames != null && paramNames.Length > 0)
                {
                    for (int i = 0; i < paramNames.Length; i++)
                    {
                        SqlParameter param = new SqlParameter
                        {
                            ParameterName = paramNames[i],
                            Direction = ParameterDirection.Input,
                            Value = paramValues[i]
                        };
                        cmd.Parameters.Add(param);
                    }
                }

                if (outputParams != null && outputParams.Count > 0)
                {
                    foreach (string paramName in outputParams)
                    {
                        SqlParameter param = new SqlParameter
                        {
                            ParameterName = paramName,
                            Direction = ParameterDirection.Output,
                            Value = DBNull.Value,
                            Size = 25
                        };
                        cmd.Parameters.Add(param);
                    }
                }

                cmd.CommandTimeout = 99999;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();

                // Set output param values
                for (int i = 0; i < outputParams.Count; i++)
                {
                    outputParams[outputParams.GetKey(i)] = cmd.Parameters[outputParams.GetKey(i)].Value?.ToString();
                }
            }
            Disconnect();
        }

        public DataSet ExecuteSP(string cmdText)
        {
            Connect();
            DataSet dSet = new DataSet();
            using (SqlDataAdapter adapter = new SqlDataAdapter())
            {
                using (SqlCommand cmd = new SqlCommand(cmdText, con))
                {
                    cmd.CommandTimeout = 99999;
                    cmd.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand = cmd;
                    adapter.Fill(dSet);
                }
            }
            Disconnect();
            return dSet;
        }

        public DataSet ExecuteSelect(string cmdText)
        {
            Connect();
            DataSet dSet = new DataSet();
            using (SqlDataAdapter adapter = new SqlDataAdapter())
            {
                using (SqlCommand cmd = new SqlCommand(cmdText, con))
                {
                    cmd.CommandTimeout = 99999;
                    adapter.SelectCommand = cmd;
                    adapter.Fill(dSet);
                }
            }
            Disconnect();
            return dSet;
        }

        public int ExecuteDML(string cmdText)
        {
            Connect();
            int rowsAffected;
            using (SqlCommand cmd = new SqlCommand(cmdText, con))
            {
                cmd.CommandTimeout = 99999;
                rowsAffected = cmd.ExecuteNonQuery();
            }
            Disconnect();
            return rowsAffected;
        }

        #endregion

        #region IDisposable

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (con != null && con.State == ConnectionState.Open)
                    {
                        con.Close();
                        con.Dispose();
                        con = null;
                    }
                }
            }
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
