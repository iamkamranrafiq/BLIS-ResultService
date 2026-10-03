using Bioreference.LIS;
using Bioreference.UI;

namespace Bioreference.ResultService.Common.Common
{

   public static class GlobalModule
    {

        private readonly static ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string ApplicationName = "B2";
        public const int APP_ID = 7; // Lab Results UI
        public const string ClinicalTrialDivisionID = "NJ1ClinicalTrials";
        public const string DefaultDivisionID = "NJ1";

        private static Bioreference.Security.IBioreferenceIdentity m_user; // 'This is set on login.
        private static string m_userDivisionCode;
        private static TestCodeGroups m_testCodeGroups;
        private static object m_lock = new object();

        internal static TestCodeGroups GetTestCodeGroups()
        {
            if (m_testCodeGroups == null)
            {
                m_testCodeGroups = TestCodeGroups.Fetch(true, false);
            }
            return m_testCodeGroups;
        }


        public static bool HasAnalyteReleasePermissions(string testCode)

        {
            if (HasFullAnalytePermissions())
                return true;
            TestCodeGroup[] t = GetTestCodeGroups().FindGroups(testCode);
            if (t.Length == 0)
                return true;
            foreach (TestCodeGroup Item in t)
            {
                // 'If Not Item.IsForSecurity Then Continue For
                if (m_user.IsInRole(string.Concat(Item.RoleKey, "_RELEASE")))
                    return true;
            }
            return false;
        }

        public static bool HasFullAnalytePermissions()
        {
            return true; //m_user.IsInRole(RoleKeys.b2_TestCodeGroup_Full); ToDo when we have roles implementation
        }
  
    }
}
