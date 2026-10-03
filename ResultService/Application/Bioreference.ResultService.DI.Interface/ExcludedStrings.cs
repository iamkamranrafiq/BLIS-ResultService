
namespace Bioreference.ResultService.DI.Interface
{
    public class ExcludedStrings
    {
        private List<string> oStringList;
        public ExcludedStrings()
        {
            oStringList = new List<string>();
        }

        public List<string> StringList
        {
            get
            {
                return oStringList;
            }
        }

        public string CreateList(string file)
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string localDependencyPath = Path.Combine(baseDirectory, "SuppressNte");

                string fileName = $"{file}.txt";
                string fullPath = Path.Combine(localDependencyPath, fileName);

                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"SuppressNtes file not found: {fileName}");

                using (StreamReader sr = new StreamReader(fullPath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        oStringList.Add(line);
                    }
                }

                return "SUCCESS";
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.Message;
            }
        }

    }
}