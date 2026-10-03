
namespace Bioreference.ResultService.DI.Interface
{
    public class NTE
    {
        private string sText;
        private int iGroupid = 0;
        private string sExternalId = "";

        public NTE(string strText, int groupid = 0, string externalid = "")
        {
            if (strText == null || string.IsNullOrEmpty(strText))
            {
                sText = " ";
            }
            else
            {
                sText = strText;
            }
            iGroupid = groupid;
            sExternalId = externalid;
        }

        public string Text
        {
            get
            {
                return sText;
            }
            set
            {
                sText = value;
            }
        }

        public int GroupId
        {
            get
            {
                return iGroupid;
            }
            set
            {
                iGroupid = value;
            }
        }

        public string ExternalId
        {
            get
            {
                return sExternalId;
            }
            set
            {
                sExternalId = value;
            }
        }
    }
}