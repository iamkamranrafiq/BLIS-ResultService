using Bioreference.LIS.Helper;

namespace Bioreference.LIS
{
    public class TestManager
    {

        public static TestStatus SaveTestDetail(Common.TestMaster.Test test)
        {

            if (test.IsDirty)
            {
                return new TestStatus(true, "The test supplied is dirty, unable to save.");
            }
            if (test.IsNew)
            {
                return new TestStatus(true, "The test supplied is new, unable to save.");
            }

            bool isError = false;

            if (test.IsProfile)
            {
                var t = new TestLookup(test.TestCode, test.RefLab, test.RefLabTest, true, false);
                foreach (Common.TestMaster.TestComponent c in test.Components)
                    t.ComponentCodes.Add(new TestLookupComponent(t, c.ComponentCode, c.AlternateOutboundTestCode, c.AlternateOutboundDescription, c.AlternateInboundTestCode));
                t.Save();
                if (t.Id == 0)
                    return new TestStatus(true, string.Format("Error saving profile {0}. Please check error log.", test.TestCode));
            }
            else if (test.IsPanel)
            {
                var r = new TestAnalytePanel(test);
                r.Save();
                if (r.Id == 0L)
                    return new TestStatus(true, string.Format("Error saving panel {0}. Please check error log.", test.TestCode));
            }
            else
            {
                var a = new TestAnalyte(test);
                a.Save();
                if (a.Id == 0L)
                    return new TestStatus(true, string.Format("Error saving analyte {0}. Please check error log.", test.TestCode));
            }

            return new TestStatus(false, "");

        }

        public class TestStatus
        {
            private bool m_isError;
            private string m_message;
            public bool IsError
            {
                get
                {
                    return m_isError;
                }
            }
            public string Message
            {
                get
                {
                    return m_message;
                }
            }
            internal TestStatus(bool iserror, string message)
            {
                m_isError = iserror;
                m_message = message.NormalizeToWindows();
            }
        }

    }
}