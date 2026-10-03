using Bioreference.ResultService.DI.Interface;
using System.Collections.Generic;

namespace Bioreference.ResultService.DI.Interface
{
    public class Patient
    {
        private string sId = string.Empty;
        private string sIdentifierList = string.Empty;
        private string sLastName = string.Empty;
        private string sFirstName = string.Empty;
        private string sMiddleName = string.Empty;
        private string sDob = string.Empty;
        private string sSex = string.Empty;
        private string sStreet1 = string.Empty;
        private string sStreet2 = string.Empty;
        private string sCity = string.Empty;
        private string sState = string.Empty;
        private string sZip = string.Empty;
        private string sHPhone = string.Empty;
        private string sWPhone = string.Empty;
        private string sSsn = string.Empty;
        private string sComments = string.Empty;
        private List<NTE> cNtes = null;
        private string sDocAcct = string.Empty;
        private string sUpdateTrackingId = string.Empty;

        public Patient(string strId = "", string strIdentifierList = "", string strLastName = "", string strFirstName = "", string strMiddleName = "", string strDOB = "", string strSex = "", string strStreet1 = "", string strStreet2 = "", string strCity = "", string strState = "", string strZip = "", string strHomePhone = "", string strWorkPhone = "", string strSSN = "", string strComments = "", string strPhysicianAcct = "")




        {
            cNtes = new List<NTE>();
            if (!string.IsNullOrEmpty(strId))
                sId = strId;
            if (!string.IsNullOrEmpty(strIdentifierList))
                sIdentifierList = strIdentifierList;
            if (!string.IsNullOrEmpty(strLastName))
                sLastName = strLastName;
            if (!string.IsNullOrEmpty(strFirstName))
                sFirstName = strFirstName;
            if (!string.IsNullOrEmpty(strMiddleName))
                sMiddleName = strMiddleName;
            if (!string.IsNullOrEmpty(strDOB))
                sDob = strDOB;
            if (!string.IsNullOrEmpty(strSex))
                sSex = strSex;
            if (!string.IsNullOrEmpty(strStreet1))
                sStreet1 = strStreet1;
            if (!string.IsNullOrEmpty(strStreet2))
                sStreet2 = strStreet2;
            if (!string.IsNullOrEmpty(strCity))
                sCity = strCity;
            if (!string.IsNullOrEmpty(strState))
                sState = strState;
            if (!string.IsNullOrEmpty(strZip))
                sZip = strZip;
            if (!string.IsNullOrEmpty(strHomePhone))
                sHPhone = strHomePhone;
            if (!string.IsNullOrEmpty(strWorkPhone))
                sWPhone = strWorkPhone;
            if (!string.IsNullOrEmpty(strSSN))
                sSsn = strSSN;
            if (!string.IsNullOrEmpty(strPhysicianAcct))
                sDocAcct = strPhysicianAcct;
            if (!string.IsNullOrEmpty(strComments))
            {
                sComments = strComments;
                AddNteInternal();
            }
        }

        public string UpdateTrackingId
        {
            get
            {
                return sUpdateTrackingId;
            }
            set
            {
                sUpdateTrackingId = value;
            }
        }
        public string Id
        {
            get
            {
                return sId;
            }
            set
            {
                sId = value;
            }
        }

        public string IdentifierList
        {
            get
            {
                return sIdentifierList;
            }
            set
            {
                sIdentifierList = value;
            }
        }

        public string LastName
        {
            get
            {
                return sLastName;
            }
            set
            {
                sLastName = value;
            }
        }

        public string FirstName
        {
            get
            {
                return sFirstName;
            }
            set
            {
                sFirstName = value;
            }
        }

        public string MiddleName
        {
            get
            {
                return sMiddleName;
            }
            set
            {
                sMiddleName = value;
            }
        }

        public string DOB
        {
            get
            {
                return sDob;
            }
            set
            {
                sDob = value;
            }
        }

        public string Sex
        {
            get
            {
                return Common.SexConverter(sSex);
            }
            set
            {
                sSex = value;
            }
        }

        public string Street1
        {
            get
            {
                return sStreet1;
            }
            set
            {
                sStreet1 = value;
            }
        }

        public string Street2
        {
            get
            {
                return sStreet2;
            }
            set
            {
                sStreet2 = value;
            }
        }

        public string City
        {
            get
            {
                return sCity;
            }
            set
            {
                sCity = value;
            }
        }

        public string State
        {
            get
            {
                return sState;
            }
            set
            {
                sState = value;
            }
        }

        public string Zip
        {
            get
            {
                return sZip;
            }
            set
            {
                sZip = value;
            }
        }

        public string HomePhone
        {
            get
            {
                return sHPhone;
            }
            set
            {
                sHPhone = value;
            }
        }

        public string WorkPhone
        {
            get
            {
                return sWPhone;
            }
            set
            {
                sWPhone = value;
            }
        }

        public string SocSecNum
        {
            get
            {
                return sSsn;
            }
            set
            {
                sSsn = value;
            }
        }

        public string PhysicianAcct
        {
            get
            {
                return sDocAcct;
            }
            set
            {
                sDocAcct = value;
            }
        }

        public string Comments
        {
            get
            {
                return sComments;
            }
            set
            {
                sComments = value;
                AddNteInternal();
            }
        }

        public List<NTE> NTEs
        {
            get
            {
                return cNtes;
            }
        }

        private void AddNteInternal()
        {
            if (cNtes is null)
                cNtes = new List<NTE>();
            else
                cNtes.Clear();
            List<NTE> cComNte;
            cComNte = Common.GetNteFromComment(sComments);
            foreach (NTE Note in cComNte)
                cNtes.Add(Note);
        }

        public void AddNTE(NTE oNote)
        {
            if (cNtes is null)
                cNtes = new List<NTE>();
            cNtes.Add(oNote);
        }

        public int FindNTEGroupId(string externalId)
        {

            foreach (NTE n in NTEs)
            {
                if ((n.ExternalId ?? "") == (externalId ?? ""))
                {
                    return n.GroupId;
                }
            }
            return 0;

        }


    }
}



//using Org.BouncyCastle.Utilities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Bioreference.ResultService.DI.Interface
//{
//    public class DIPatient
//    {
//        public required string Id { get; set; }
//        public required string IdentifierList { get; set; }
//        public required string LastName { get; set; }
//        public string FirstName { get; set; }
//        public string MiddleName { get; set; }
//        public string Dob { get; set; }
//        public string Street1 { get; set; }
//        public string Street2 { get; set; }
//        public string City { get; set; }
//        public string State { get; set; }
//        public string Zip { get; set; }
//        public string HomePhone { get; set; }
//        public string WorkPhone { get; set; }
//        public string SocSecNum { get; set; }
//        private string comments;
//        public string PhysicianAcct { get; set; }
//        public string UpdateTrackingId { get; set; }
//        private List<NTE> ntes;
//        private string sex;

//        public DIPatient()
//        {
//            Id = string.Empty;
//            IdentifierList = string.Empty;
//            LastName = string.Empty;
//            FirstName = string.Empty;
//            MiddleName = string.Empty;
//            Dob = string.Empty;
//            sex = string.Empty;
//            Street1 = string.Empty;
//            Street2 = string.Empty;
//            City = string.Empty;
//            State = string.Empty;
//            Zip = string.Empty;
//            HomePhone = string.Empty;
//            WorkPhone = string.Empty;
//            SocSecNum = string.Empty;
//            Comments = string.Empty;
//            PhysicianAcct = string.Empty;
//            ntes = new List<NTE>();
//        }

//        public DIPatient(string id, string identifierList, string lastName, string firstName, string middleName, string dob, string sex, string street1, string street2, string city, string state, string zip, string homePhone, string workPhone, string socSecNum, string comments, string physicianAcct, string updateTrackingId)
//        {
//            ntes = new List<NTE>();
//            if (!string.IsNullOrEmpty(id)) this.Id = id;
//            if (!string.IsNullOrEmpty(identifierList)) this.IdentifierList = identifierList;
//            if (!string.IsNullOrEmpty(lastName)) this.LastName = lastName;
//            if (!string.IsNullOrEmpty(firstName)) this.FirstName = firstName;
//            if (!string.IsNullOrEmpty(middleName)) this.MiddleName = middleName;
//            if (!string.IsNullOrEmpty(dob)) this.Dob = dob;
//            if (!string.IsNullOrEmpty(sex)) this.Sex = sex;
//            if (!string.IsNullOrEmpty(street1)) this.Street1 = street1;
//            if (!string.IsNullOrEmpty(street2)) this.Street2 = street2;
//            if (!string.IsNullOrEmpty(city)) this.City = city;
//            if (!string.IsNullOrEmpty(state)) this.State = state;
//            if (!string.IsNullOrEmpty(zip)) this.Zip = zip;
//            if (!string.IsNullOrEmpty(homePhone)) this.HomePhone = homePhone;
//            if (!string.IsNullOrEmpty(workPhone)) this.WorkPhone = workPhone;
//            if (!string.IsNullOrEmpty(SocSecNum)) this.SocSecNum = SocSecNum;
//            if (!string.IsNullOrEmpty(physicianAcct)) this.PhysicianAcct = physicianAcct;
//            if (!string.IsNullOrEmpty(updateTrackingId)) this.UpdateTrackingId = updateTrackingId;
//            if (!string.IsNullOrEmpty(physicianAcct)) this.PhysicianAcct=physicianAcct;
//            if (!string.IsNullOrEmpty(comments))
//            {
//                this.Comments = comments;
//                AddNteInternal();
//            }
//        }

//        public string Sex
//        {
//            get { return Common.SexConverter(sex); }
//            set { sex = value; }
//        }

//        public string Comments
//        {
//            get
//            {
//                return comments;
//            }
//            set
//            {
//                comments = value;
//                AddNteInternal();
//            }
//        }

//        public List<NTE> NTEs
//        {
//            get
//            {
//                return ntes;
//            }
//        }

//        private void AddNteInternal()
//        {
//            ntes = new List<NTE>();
//            List<NTE> notes = Common.GetNteFromComment(comments);
//            foreach (NTE note in notes)
//            {
//                ntes.Add(note);
//            }
//        }

//        public void AddNTE(NTE note)
//        {
//            if (ntes == null)
//                ntes = new List<NTE>();
//            ntes.Add(note);
//        }

//        public int FindNTEGroupId(string externalId)
//        {
//            foreach (NTE nte in ntes)
//            {
//                if (nte.ExternalId == externalId)
//                    return nte.GroupId;
//            }
//            return 0;
//        }
//    }
//}
