using System.Diagnostics;
using System.Runtime.InteropServices;
using Bioreference.Common.TestMaster;

namespace Bioreference.LIS
{

    public class TestMasterWrapper
    {

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static TestInfo Fetch(string analyteCode, [Optional, DefaultParameterValue(0)] int refLabId, [Optional] ref Order order)
        {
            TestInfo Tinfo = null;
            var timer = new Stopwatch();
            timer.Start();
            if (order is null | order.DivisionId == 0)
            {
                if (refLabId == 0)
                {
                    Tinfo = TestInfo.Fetch(analyteCode);
                }
                else
                {
                    Tinfo = TestInfo.Fetch(analyteCode, refLabId);
                }
            }
            else if (refLabId == 0)
            {
                Tinfo = TestInfo.Fetch(analyteCode, divisionId: order.DivisionId);
            }
            else
            {
                Tinfo = TestInfo.Fetch(analyteCode, refLabId, order.DivisionId);
            }
            timer.Stop();
            object elapsedTime = timer.ElapsedMilliseconds;
            Log.InfoFormat("Test Master fetch call for test code {0}, took {1} milliseconds", analyteCode, elapsedTime);
            return Tinfo;
        }
    }
}