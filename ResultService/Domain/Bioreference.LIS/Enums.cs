
namespace Bioreference.LIS
{
    public enum resultStatusType
    {
        DeltaHold = -2,
        OnHold = -1,
        Pending = 0,
        Preliminary = 1,
        Final = 2,
        Corrected = 3
    }

    public enum transmitStatusType
    {
        NotSet = -1, // Order as Follows: 1,2,4,5,6,3 : -1,0 should not be used.
        None = 0,
        PendingRelease = 1,
        Released = 2,
        Reported = 3,
        SentToReporting = 4,
        ReadByReporting = 5,
        StatusSentToVertex = 6,
        // 'StatusSentToReporting = 7
        HeldForRerun = 10
    }

    public enum reReleaseStatusType
    {
        ResultChange = 1,
        ReReleased = 2
    }

    public enum HoldType
    {
        None = 0,
        TestHold = 3
    }

    public enum hasResultsType
    {
        NotSet = -1,
        No = 0,
        Yes = 1
    }

    // This is for Accession Level Status.
    public enum reportStatusType
    {
        Pending = 0,
        Preliminary = 1,
        Final = 2
    }

    public enum orderParentType
    {
        Unknown = 0,
        Report = 1,
        ReportAnalytePanel = 2,
        ReportAnalyte = 3
    }

    public enum AddAccessionStatusType
    {
        Success = 1,
        AlreadyExists = 2,
        NoMatchingAnalytes = 3,
        AccessionNotExists = 4,
        InvalidStatus = 5,
        TypeNotOnAccession = 6,
        ExistsOnOtherWorksheet = 7
    }

    public enum CocBatchCreateStatus
    {
        Success = 1,
        Failure = 2
    }

    public enum CocBatchValidationStatus
    {
        Success = 1,
        Failure = 2
    }

    public enum CocBatchAccessionEditStatus
    {
        Success = 1,
        Failure = 2
    }

    public enum rackWorksheetType
    {
        NotSet = -1,
        Urinalysis = 1,
        POC = 2
    }

    public enum DiffPadCellType
    {
        Count = 1,
        Morphology = 2,
        Absolute = 3,
        ViewOnly = 4
    }

    public enum outBoundMessageSendTo
    {
        None = 0,
        Vertex = 1,
        Reporting = 2,
        ReportingAccessionStatus = 3 // indicates outbound status channel sends an Accession level status - the actual message is not defined in the OutboundMessage
    }

    public enum processOddEvenType
    {
        Odd = -1,
        All = 0,
        Even = 1
    }

    public enum clientType
    {
        None = 0,
        BlueBag = 1,
        PurpleBag = 2
    }

    public enum externalApplicationType
    {
        B2 = 0,
        CareEvolve = 1,
        SPM = 2
    }

    public enum SampleRequestType
    {
        None = 0,
        SampleRequest = 1,
        SampleEnRoute = 2,
        NoSample = 3,
        Closed = 4
    }

    public enum SPMStatusValue
    {
        None = 0,
        TNP = 1,
        ATP = 2,
        Reversed = 3
    }

    public enum FourKSampleType
    {
        None = 0,
        Serum = 1,
        Plasma = 2
    }
    public enum MessageType
    {
        UnsolicitedMessage
    }
    public enum CodeType
    {
        Test = 1,
        Profile = 2,
        Panel = 3,
        Calculation = 4,
        AOE = 5,
        Metadata = 6
    }
}