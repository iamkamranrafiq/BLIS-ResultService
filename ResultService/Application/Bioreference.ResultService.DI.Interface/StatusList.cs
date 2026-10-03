using Bioreference.ResultService.DI.Interface;
public class StatusList
{
    private List<ReportStatus> oList;

    public StatusList()
    {
        oList = new List<ReportStatus>();
    }

    public void Add(ReportStatus rpt)
    {
        oList.Add(rpt);
    }

    public void Clear()
    {
        oList.Clear();
    }

    public int Count
    {
        get { return oList.Count; }
    }

    public ReportStatus[] List
    {
        get { return oList.ToArray(); }
    }

    public void Save(string strStatusPath, string strArchivePath)
    {
        if (oList.Count > 0)
        {
            // Save the archive
            string sFullMsg = string.Empty;
            string sAccessionNum = oList[0].AccessionNumber;
            foreach (ReportStatus rs in oList)
            {
                string sMsg = rs.GetMessage();
                sFullMsg += sMsg + Environment.NewLine;
            }
            File.AppendAllText(strArchivePath +
                "StatusUpdate_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt", sFullMsg);

            // Save the real file
            string fileName = strStatusPath + "StatusUpdate_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
            using (StreamWriter sw = new StreamWriter(fileName, true))
            {
                foreach (ReportStatus rs in oList)
                {
                    string sMsg = rs.GetMessage();
                    sw.WriteLine(sMsg);
                }
            }
        }
    }

    public bool ReportExists(string strAccession, string strTestCode)
    {
        foreach (ReportStatus r in oList)
        {
            if (r.AccessionNumber == strAccession && r.TestCode == strTestCode)
            {
                return true;
            }
        }
        return false;
    }
}