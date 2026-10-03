using Newtonsoft.Json.Linq;
using System.Text;

namespace Bioreference.LIS
{

    public class OrderSnapshot
    {

        private JObject _snapShot;
        private List<SpecimenCode> _testSpecimens = new List<SpecimenCode>();
        private List<ParentTestCode> _parentTests = new List<ParentTestCode>();
        private List<Specimen> _orderSpecimens = new List<Specimen>();
        private Patient _patient=null;

        public OrderSnapshot(string orderSnapShot)
        {
            try
            {
                if (orderSnapShot is null || orderSnapShot.Length == 0)
                    return;
                _snapShot = JObject.Parse(orderSnapShot);
                var specimens = _snapShot["Specimens"];
                if (specimens is not null)
                {
                    foreach (JToken spec in specimens)
                        _orderSpecimens.Add(new Specimen() { SpecimenCode = (string)spec["SpecimenCode"], Quantity = (int)spec["Quantity"] });
                }
                var tests = _snapShot["Tests"];
                if (tests is not null)
                {
                    foreach (JToken test in tests)
                    {
                        foreach (JToken specimenCode in test["PrimarySpecimenCodes"])
                            _testSpecimens.Add(new SpecimenCode() { TestCode = (string)test["TestCode"], Specimen = (string)specimenCode });
                        foreach (JToken specimenCode in test["AlternateSpecimenCodes"])
                            _testSpecimens.Add(new SpecimenCode() { TestCode = (string)test["TestCode"], Specimen = (string)specimenCode });
                        if ((string)test["ParentTestCode"] != "")
                        {
                            _parentTests.Add(new ParentTestCode() { TestCode = (string)test["TestCode"], ParentTestCodeField = (string)test["ParentTestCode"] });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        public OrderSnapshot(List<Bioreference.LIS.OrderSnapshot.Specimen> specimenList, List<SpecimenCode> testSpecimens, List<ParentTestCode> parentTests)
        {
            try
            {
                _orderSpecimens = specimenList;
                _testSpecimens = testSpecimens;
                _parentTests = parentTests;
            }
            catch (Exception ex) { }
        }

        public Patient PatientInfo
        {
            get
            {
                if (_snapShot is null || _snapShot["Patient"] is null)
                    return null;
                if (_patient != null)
                    return _patient;
                _patient = new Patient();                
                _patient.FirstName = _snapShot["Patient"]?["FirstName"]?.ToString() ?? "";
                _patient.LastName = _snapShot["Patient"]?["LastName"]?.ToString() ?? "";
                _patient.StreetLine1 = _snapShot["Patient"]?["StreetLine1"]?.ToString() ?? "";
                _patient.StreetLine2 = _snapShot["Patient"]?["StreetLine2"]?.ToString() ?? "";
                _patient.City = _snapShot["Patient"]?["City"]?.ToString() ?? "";
                _patient.State = _snapShot["Patient"]?["State"]?.ToString() ?? "";
                _patient.ZipCode = _snapShot["Patient"]?["ZipCode"]?.ToString() ?? "";
                _patient.Country = _snapShot["Patient"]?["Country"]?.ToString() ?? "";
                _patient.HomePhoneNumber = _snapShot["Patient"]?["HomePhoneNumber"]?.ToString() ?? "";
                return _patient;
            }
        }

        public List<Specimen> OrderSpecimens
        {
            get
            {
                return _orderSpecimens;
            }
        }

        public string SpecimenSummary
        {
            get
            {
                var s = new StringBuilder();
                foreach (Specimen spec in _orderSpecimens)
                    s.Append($"{spec.SpecimenCode}({spec.Quantity}) ");
                return s.ToString().Trim();
            }
        }

        public List<SpecimenCode> SpecimenCodes
        {
            get
            {
                return _testSpecimens;
            }
        }

        public IEnumerable<string> SpecimenTestCodes
        {
            get
            {
                return (from n in _testSpecimens
                        select n.TestCode).Distinct();
            }
        }

        public List<ParentTestCode> ParentTestCodes
        {
            get
            {
                return _parentTests;
            }
        }

        public class SpecimenCode
        {
            public string TestCode;
            public string Specimen;
        }

        public class ParentTestCode
        {
            public string TestCode;
            public string ParentTestCodeField;
        }

        public class Specimen
        {
            public string SpecimenCode;
            public int Quantity;
        }

        public class Patient
        {
            public string FirstName;
            public string LastName;
            public string StreetLine1;
            public string StreetLine2;
            public string City;
            public string State;
            public string ZipCode;
            public string Country;
            public string HomePhoneNumber;
        }

    }
}