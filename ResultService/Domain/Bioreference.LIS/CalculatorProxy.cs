namespace Bioreference.LIS
{
    public class CalculatorProxy : ProxyBase
    {
        #region Constructors

        public CalculatorProxy(string token)
            : base(token)
        { }

        public CalculatorProxy(string userName, IToken token)
            : base(userName, System.Environment.GetEnvironmentVariable("COMPUTERNAME") ?? "px:unknown", token)
        { }

        public CalculatorProxy(string userName, IToken token, bool throwException)
            : base(userName, System.Environment.GetEnvironmentVariable("COMPUTERNAME") ?? "px:unknown", token, throwException)
        { }

        public CalculatorProxy(string userName, string machineName, IToken token)
            : base(userName, machineName, token) { }

        public CalculatorProxy(string userName, string machineName, IToken token, bool throwException)
            : base(userName, machineName, token, throwException) { }

        #endregion

        /// <summary>
        /// Calculate HGCancerRisk from calling external XML web service
        /// </summary>
        /// <param name="patientAge">patient Age example: patientAge = 49.3360710144042M</param>
        /// <param name="freePSA">free PSA example: freePSA = 0.726M</param>
        /// <param name="totalPSA">total PSA example: totalPSA = 6.25M</param>
        /// <param name="intactPSA">intact PSA example: intactPSA = 0.380M</param>
        /// <param name="HK2">HK2 example: HK2 = 0.0549M</param>
        /// <param name="DREResult">DREResult example: DREResult = "NoNodule"; will be  "Nodule" when Nodule is present "No Nodule" is also acceptable for "NoNodule"</param>
        /// <param name="hasPriorBiopsy">hasPriorBiopsy example:hasPriorBiopsy = false</param>
        /// <returns>HG Cancer Risk</returns>
        public double CalculateHGCancerRisk(string bloodSampleType, //"SERUM" or "PLASMA"
                                    decimal patientAge, //decimal patientAge = 49.3360710144042M; //patient age at the time of specimen collection
                                    decimal freePSA, //decimal freePSA = 0.726M;
                                    decimal totalPSA,//decimal totalPSA = 6.25M;
                                    decimal intactPSA, //decimal intactPSA = 0.380M;
                                    decimal HK2,//decimal HK2 = 0.0549M;
                                    string DREResult,//string DREResult = "NoNodule"; // will be  "Nodule" when Nodule is present "No Nodule" is also acceptable for "NoNodule"
                                    bool hasPriorBiopsy)
        {

            string url = String.Format("api/Calculator/CalculateHGCancerRisk?bloodSampleType={0}&patientAge={1}&freePSA={2}&totalPSA={3}&intactPSA={4}&HK2={5}&DREResult={6}&hasPriorBiopsy={7}",
                bloodSampleType, patientAge, freePSA, totalPSA, intactPSA, HK2, DREResult, hasPriorBiopsy);
            return Get<double>(url, 0);
        }

    }
}
