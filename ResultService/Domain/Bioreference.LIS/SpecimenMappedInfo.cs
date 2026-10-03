using System;

namespace Bioreference.LIS
{
    [Serializable()]
    public class SpecimenMappedInfo
    {


        public int SpecimenID { get; set; }

        public int SpecimenTypeID { get; set; }

        public string SpecimenNumber { get; set; }

        public string AvailableQty { get; set; }

        public string RequestType { get; set; }

        public int Quantity { get; set; }

        public string MasterTube { get; set; }

        public DateTime EntryDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsQNS { get; set; }

        public int FridgeID { get; set; }

        public string FridgeNumber { get; set; }

        public DateTime EntryDateInFridge { get; set; }

        public string RackNumber { get; set; }

        public string Position { get; set; }

        public string ShelfNumber { get; set; }

        public string ShelfCellNumber { get; set; }

    }
}