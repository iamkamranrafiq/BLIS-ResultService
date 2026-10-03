namespace Bioreference.ResultService.DI.Interface
{
    public class OrderTest
    {
        private string sTestCode;
        private string sTestDesc;
        private string sTestComment;

        public OrderTest(string strTestCode, string strTestComment = "", string strTestDesc = "")
        {
            sTestCode = strTestCode;
            sTestDesc = strTestDesc;
            sTestComment = strTestComment;
        }

        public string TestCode
        {
            get { return sTestCode; }
            set { sTestCode = value; }
        }

        public string TestDesc
        {
            get { return sTestDesc; }
            set { sTestDesc = value; }
        }

        public string TestComment
        {
            get { return sTestComment; }
            set { sTestComment = value; }
        }
    }

}
