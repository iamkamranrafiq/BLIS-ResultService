using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.Enum
{
    public enum AddAccessionStatusTypeModel
    {
        Success = 1,
        AlreadyExists = 2,
        NoMatchingAnalytes = 3,
        AccessionNotExists = 4,
        InvalidStatus = 5,
        TypeNotOnAccession = 6
    }
    public enum transmitStatusTypeModel
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
    public enum resultStatusTypeModel
    {
        DeltaHold = -2,
        OnHold = -1,
        Pending = 0,
        Preliminary = 1,
        Final = 2,
        Corrected = 3
    } 
    public enum CocBatchCreateStatusModel
    {
        Success = 1,
        Failure = 2
    }
    public enum reReleaseStatusTypeModel
    {
        ResultChange = 1,
        ReReleased = 2
    }

    public enum SPMStatusValueModel
    {
        None = 0,
        TNP = 1,
        ATP = 2,
        Reversed = 3
    }
    public enum criticalTypeModel
    {

        NotEvaluated = -1,
        NotCritical = 0,
        IsCritical = 1,
        IsAbnormal = 2       
    }

    public enum OrderPriorityModel
    {

        Unknown = 0,
        Routine = 1,
        STAT = 2

    }
    public enum GenderModel
    {
        Unknown = 0,
        Male = 1,
        Female = 2
    }
    public enum parentIdentifierTypeModel
    {
        None = 0,
        AccessionNbr_ServiceDate = 1
    }
    public enum genderFlagTypeModel
    {

        Both = 0,
        Male = 1,
        Female = 2

    }
    public enum reportingTypeModel
    {

        Discrete = 0,
        Terse = 1,
        TabularText = 2,
        TabularTextWithOutHeaders = 3

    }

    public enum commentTypeModel
    {
        Unknown = -1,
        Canned = 1,       
        TestNotPerformed = 4,
        TestSpecific = 5
    }
    public enum resultTypeModel
    {
        StringFormat = 1,
        NumericFormat = 2,
        DecimalFormat = 3,
        PickListItem = 4
    }

    public enum downloadTypeModel
    {

        None = 0,
        Query = 1,
        Dynamic = 2

    }

    public enum CommentTypeModel
    {
        Unknown = -1,
        Canned = 1,
        TestNotPerformed = 4,
        TestSpecific = 5,
        AccessionSpecific = 6,
        RefLab = 7,
        [Description("Specimen Comment")]
        SpecimenComment = 8
    }
    public enum ExternalCommentTypeModel
    {
        Comment = 1,
        Statement = 2
    }

    public enum IsFastingTypeModel
    {
        No = 0,
        Yes = 1,
        Unknown = -1
    }

    public enum AddAccessionStatusType
    {
        Success = 1,
        AlreadyExists = 2,
        NoMatchingAnalytes = 3,
        AccessionNotExists = 4,
        InvalidStatus = 5,
        TypeNotOnAccession = 6
    }
}
