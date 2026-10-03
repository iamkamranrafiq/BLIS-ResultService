using System;
using System.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    /// <summary>
/// This class is used to return Analyte specific information when using the Report.Fetch function 
/// with the AnalyteCode and TransmitStatus parameters
/// </summary>
/// <remarks></remarks>
    [Serializable()]
    public class ReportAnalyteInfo : ReportInfo
    {


        #region Private Members

        private string m_resultValue = "";
        private string m_analyteCode = "";

        #endregion

        #region Constructor

        internal ReportAnalyteInfo()
        {
        }

        #endregion

        #region Public Properties

        public string ResultValue
        {
            get
            {
                return m_resultValue;
            }
        }

        public string AnalyteCode
        {
            get
            {
                return m_analyteCode;
            }
        }

        #endregion

        #region Data Functions

        internal new void Load(DataRow row)
        {

            base.Load(row);

            m_resultValue = Conversions.ToString(row["ResultValue"]);
            m_analyteCode = Conversions.ToString(row["AnalyteCode"]);

        }

        #endregion

    }
}