using System;
using System.Collections.Generic;
using Bioreference.LIS.Helper;

namespace Bioreference.LIS
{
    public class RequestSpecimenInfo
    {

        public List<Specimens> Specimens { get; set; }

        public List<RequestPriority> RequestPriorities { get; set; }

        public List<string> CommentTypes { get; set; }

        public List<string> RequestTypes { get; set; }

    }

    public class RequestPriority
    {

        public int PriorityTypeID { get; set; }

        public string PriorityTypeName { get; set; }

    }

    public class Specimens
    {

        public int SpecimenID { get; set; }

        public int SpecimenTypeID { get; set; }

        public string SpecimenNumber { get; set; }

        public int FridgeId { get; set; }

        public string FridgeNumber { get; set; }

        public bool IsQNS { get; set; }

        public DateTime EntryDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsExpired { get; set; }

        public List<RequestDepartment> SpecimenRequestDepts { get; set; }

    }

    public class RequestDepartment
    {

        public int RequestDeptID { get; set; }

        public string RequestDeptName { get; set; }

        public bool HasPendingRequest { get; set; }

        public List<SpecimenReqestDeptLocation> SpecimenReqestDeptLocations { get; set; }

    }

    public class SpecimenReqestDeptLocation
    {

        public int LocationID { get; set; }

        public string LocationName { get; set; }

    }

    public class RequestSpecimenResponseStatus
    {
        private string _errorMessage;

        public string SpecimenNumber { get; set; }

        public string RequestTypeFridge { get; set; }

        public string Status { get; set; }

        public string ErrorMessage 
        { 
            get => _errorMessage;
            set => _errorMessage = value.NormalizeToWindows();
        }

        public string BatchNumber { get; set; }

    }

    public class BRADSpecimenRequestResponse
    {
        public int NoOfSuccess { get; set; }
        public List<BRADSpecimenRequestDetails> SpecimenResponseDetails { get; set; }
    }

    public class BRADSpecimenRequestDetails
    {
        private string _errorMessage;

        public string SpecimenNumber { get; set; }

        public string Status { get; set; }

        public string ErrorMessage 
        { 
            get => _errorMessage;
            set => _errorMessage = value.NormalizeToWindows();
        }

        public string SpecimenBatchNumber { get; set; }
    }

    public enum FridgeType
    {
        WalkIn,
        NonWalkIn
    }

    public enum BradResponseStatus
    {
        Success,
        Fail
    }
}