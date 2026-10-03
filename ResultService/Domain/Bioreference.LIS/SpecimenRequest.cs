using Bioreference.LIS.Helper;

namespace Bioreference.LIS
{


    public class SpecimenRequest
    {
        private string _comments;

        public string Comments 
        { 
            get => _comments;
            set => _comments = value.NormalizeToWindows();
        }

        public bool IsMasterRequired { get; set; }

        public int FridgeId { get; set; }

        public int PriorityTypeID { get; set; }

        public int RequestDeptID { get; set; }

        public string RequestedBy { get; set; }

        public string SpecimenNumbers { get; set; }

        public int SpecimenTypeID { get; set; }

        public int Quantity { get; set; }

    }
}