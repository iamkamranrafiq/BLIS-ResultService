using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Mail;
using Bioreference.Common.Lab;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{

    public class FlagResult
    {
        private string m_flagValue = "";
        private DefaultCommentList m_comments;
        internal FlagResult(string flagValue, DefaultCommentList comments)
        {
            m_flagValue = flagValue;
            m_comments = new DefaultCommentList(null);
            foreach (DefaultComment c in comments)
                m_comments.Add(c.AssignedID, c.Text, c.Type, c.IsRepeatable, c.ExternalCommentCode, c.ExternalCommentType);

        }
        public object FlagValue
        {
            get
            {
                return m_flagValue;
            }
        }
        public DefaultCommentList Comments
        {
            get
            {
                return m_comments;
            }
        }
    }

    public class SharedFunctions
    {

        public static FlagResult GetFlagFromRanges(ComplexResultFlag[] flagRanges, string value, Common.Gender gender, string dob, string divisionCode)
        {

            return GetFlagFromRanges(flagRanges, value, gender, dob, default, 0, "", divisionCode);

        }

        public static FlagResult GetFlagFromRanges(ComplexResultFlag[] flagRanges, string value, Common.Gender gender, string dob, DateTime basedFromDate = default, int ageNumber = 0, string ageType = "", string divisionCode = "")
        {

            string flag = "";
            var f = GetMatchingResultFlag(flagRanges, gender, dob, ageNumber, ageType, basedFromDate, divisionCode);

            if (f == null)
            {
                // If not found based on gender and dob, use default
                foreach (ComplexResultFlag crf in flagRanges)
                {
                    if (crf.IsDefault)
                    {
                        f = crf;
                    }
                }
            }

            if (!(f == null))
            {
                var fr = GetFlagFromRange(value, f);
                if (!(fr == null))
                {
                    if (!Configuration.LISSettings.GetList("SuppressCommentByResult").Contains(value.Trim()))
                    {
                        foreach (DefaultComment c in f.DefaultComments)
                            fr.Comments.Add(c);
                    }
                    return fr;
                }
                else
                {
                    return new FlagResult("", f.DefaultComments);
                }
            }
            else
            {
                return new FlagResult("", new DefaultCommentList(null));
            }

        }

        /// <summary>
    /// Calculates age based on Age Number and Type
    /// </summary>
    /// <param name="flagRanges"></param>
    /// <param name="gender"></param>
    /// <param name="ageNbr"></param>
    /// <param name="ageType"></param>
    /// <param name="basedFromDate"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        public static ComplexResultFlag GetMatchingResultFlag(ComplexResultFlag[] flagRanges, Common.Gender gender, string dob, int ageNbr, string ageType, DateTime basedFromDate = default, string divisionCode = "")
        {

            if (string.IsNullOrEmpty(dob) && ageNbr != 0 && !string.IsNullOrEmpty(ageType))
            {

                DateTime evalDate = Conversions.ToDate(Interaction.IIf(basedFromDate == null || basedFromDate < DateTime.Parse("1900-01-01"), DateTime.Now, basedFromDate));

                switch (ageType.ToUpper() ?? "")
                {
                    case "Y":
                    case "YEAR":
                        {
                            dob = Conversions.ToString(evalDate.AddYears(-ageNbr));
                            break;
                        }
                    case "M":
                    case "MONTH":
                        {
                            dob = Conversions.ToString(evalDate.AddMonths(-ageNbr));
                            break;
                        }
                    case "W":
                    case "WEEK":
                        {
                            dob = Conversions.ToString(evalDate.AddDays(-(ageNbr * 7)));
                            break;
                        }
                    // dob = Utilities.General.SubtractWeeks(ageNbr, evalDate)
                    case "D":
                    case "DAY":
                        {
                            dob = Conversions.ToString(evalDate.AddDays(-ageNbr));
                            break;
                        }
                }

            }

            // 'Will use default divisionCode if no code is passed in or if ranges for that code are not found
            var c = GetMatchingResultFlag(flagRanges, gender, dob, basedFromDate, divisionCode);
            if (!(c == null))
            {
                return c;
            }
            else if (!string.IsNullOrEmpty(divisionCode))
            {
                return EvaluateDefaultDivisionCodes(flagRanges, gender, dob, basedFromDate, divisionCode);
            }
            else
            {
                return null;
            }

        }

        /// <summary>
    /// Evaluates from first to last
    /// </summary>
    /// <param name="flagRanges"></param>
    /// <param name="gender"></param>
    /// <param name="dob"></param>
    /// <param name="basedFromDate"></param>
    /// <param name="divisionCode"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        private static ComplexResultFlag EvaluateDefaultDivisionCodes(ComplexResultFlag[] flagRanges, Common.Gender gender, string dob, DateTime basedFromDate, string divisionCode)
        {

            ComplexResultFlag crf;
            foreach (string div in OrderManager.DefaultDivisionCodes)
            {
                if ((divisionCode ?? "") != (div ?? ""))
                {
                    crf = GetMatchingResultFlag(flagRanges, gender, dob, basedFromDate, div);
                    if (!(crf == null))
                        return crf;
                }
            }
            return null;

        }

        private static ComplexResultFlag GetMatchingResultFlag(ComplexResultFlag[] flagRanges, Common.Gender gender, string dob, DateTime basedFromDate = default, string divisionCode = "")
        {

            string flag = "";
            int age = 0;
            string dCode;
            dCode = string.IsNullOrEmpty(divisionCode) ? OrderManager.DefaultDivisionCodes[0] : divisionCode;

            // This should come back sorted by D,W,M,Y
            foreach (ComplexResultFlag f in flagRanges)
            {
                if ((f.DivisionCode ?? "") == (dCode ?? ""))
                {
                    if (f.UseAgeQualifier)
                    {

                        age = Utilities.General.CalcAge(dob, f.AgeType, basedFromDate);

                        if (age >= f.AgeFrom && age <= f.AgeTo && (f.Gender == genderFlagType.Both || gender == Common.Gender.Female && f.Gender == genderFlagType.Female || gender == Common.Gender.Male && f.Gender == genderFlagType.Male))
                        {
                            return f;
                        }
                    }

                    else if (f.Gender == genderFlagType.Both || gender == Common.Gender.Female && f.Gender == genderFlagType.Female || gender == Common.Gender.Male && f.Gender == genderFlagType.Male)
                    {

                        return f;
                    }
                }
            }

            // If not found based on gender and dob, use default
            foreach (ComplexResultFlag crf in flagRanges)
            {
                if ((dCode ?? "") == (crf.DivisionCode ?? "") && crf.IsDefault)
                {
                    return crf;
                }
            }

            // No default, return nothing.
            return null;

        }

        public static FlagResult GetFlagFromRange(string value, ComplexResultFlag f)
        {

            decimal dec = 0m;

            foreach (ComplexValue v in f.Values.List)
            {
                if ((v.Value ?? "") == (value ?? ""))
                {
                    // Return v.Flag
                    return new FlagResult(v.Flag, v.DefaultComments);
                }
            }

            foreach (ComplexRange r in f.Ranges.List)
            {
                if (decimal.TryParse(value, out dec))
                {
                    if (dec >= r.RangeFrom && dec <= r.RangeTo)
                    {
                        // Return r.FlagValue
                        return new FlagResult(r.FlagValue, r.DefaultComments);
                    }
                }
            }

            return null;

        }

        public static string FormatCommentText(string text, int maxLineLength)
        {

            List<string> s;
            string formattedComment = "";

            foreach (string line in text.Split('\r'))
            {
                string l = line.Replace("\n", "");
                if (l.Length > maxLineLength)
                {

                    int i;
                    s = new List<string>(); // RESET the list
                    while (l.Length > maxLineLength)
                    {
                        i = maxLineLength - 1;
                        while (i != 0)
                        {
                            if (l.Substring(i, 1) == " ")
                            {
                                s.Add(string.Concat(l.Substring(0, i), System.Environment.NewLine));
                                l = l.Substring(i + 1);
                                break;
                            }
                            i -= 1;
                        }
                    }
                    s.Add(l);
                    foreach (string c in s)
                        formattedComment = string.Concat(formattedComment, c);
                    formattedComment = string.Concat(formattedComment, System.Environment.NewLine);
                }
                else
                {
                    formattedComment = string.Concat(formattedComment, l, System.Environment.NewLine);
                }
            }

            return formattedComment;

        }

        public static bool ValidateAndClearInequality(string value, ref decimal numericResult)
        {
            char[] signs = new char[] { '>', '<', '=' };
            return decimal.TryParse(Remove(value, signs), out numericResult);
        }

        public static string Remove(string s, IEnumerable<char> chars)
        {
            return new string(s.Where(c => !chars.Contains(c)).ToArray());
        }

        internal static object HasBeenReleased(transmitStatusType status)
        {

            if (status == transmitStatusType.Released || status == transmitStatusType.StatusSentToVertex || status == transmitStatusType.SentToReporting || status == transmitStatusType.ReadByReporting || status == transmitStatusType.Reported)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public static bool IsNotPerformedResult(string result)
        {
            return Configuration.LISSettings.GetList("NotPerformedResult").Contains(result, new CompareText());
        }

        public static bool IsTNP(string result)
        {
            return Configuration.LISSettings.GetList("SPMTNPTriggers").Contains(result.Trim(), new CompareText());
        }

        public static bool IsATP(string result)
        {
            return Configuration.LISSettings.GetList("SPMATPTriggers").Contains(result.Trim(), new CompareText());
        }

        public static bool AccessionAgeInRange(DateTime dos)
        {
            return (DateTime.Now - dos).Days <= Configuration.LISSettings.GetInt("DuplicateAccessionTimeSpan");
        }

        public static List<string> SplitStringList(List<string> objIntList, int splitSize = 500)
        {
            var strList = new List<string>();

            // we have a limitation of 8,000 characters.

            var iWorkList = new List<string>();

            foreach (string item in objIntList)
            {
                if (iWorkList.Count == splitSize)
                {
                    // build the string from newlist
                    string[] arWorkList = iWorkList.ToArray();
                    strList.Add(string.Join(",", arWorkList));
                    iWorkList = new List<string>();
                }

                iWorkList.Add(item);
            }

            if (iWorkList.Count > 0)
            {
                // build another string from newlist
                string[] arWorkList = iWorkList.ToArray();
                strList.Add(string.Join(",", arWorkList));
            }

            return strList;
        }

        public static object BuildDBLoggingMessage(string proc, List<DbParameter> paramList)
        {

            string message = "";
            try
            {
                message = $"exec {proc}";
                foreach (DbParameter p in paramList)
                    message += $" {p.ParameterName}='{p.Value}';";
            }
            catch (Exception ex)
            {
                message += $"Error ({ex.Message}) formatting message.";
            }
            return message;

        }

        public static bool TextEquals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        public static void SendMail(string @from, List<string> sendTo, string subject, string body)
        {
            SendMail(from, sendTo, subject, body, null, null, null);
        }

        public static void SendMail(string @from, List<string> sendTo, string subject, string body, List<string> ccTo, List<string> bccTo, List<System.Net.Mail.Attachment> attachments)
        {
            using (var msg = new MailMessage() { From = new MailAddress(from), Subject = subject, Body = body })
            {
                if (sendTo is not null)
                {
                    foreach (string addr in sendTo)
                        msg.To.Add(new MailAddress(addr));
                }
                if (ccTo is not null)
                {
                    foreach (string addr in ccTo)
                        msg.CC.Add(new MailAddress(addr));
                }
                if (bccTo is not null)
                {
                    foreach (string addr in bccTo)
                        msg.Bcc.Add(new MailAddress(addr));
                }
                if (attachments is not null)
                {
                    foreach (System.Net.Mail.Attachment attach in attachments)
                        msg.Attachments.Add(attach);
                }
                SendMail(msg);
            }
        }

        public static void SendMail(MailMessage message)
        {
            using (var smtpClient = new SmtpClient(Configuration.LISSettings.GetString("SMTP")))
            {
                smtpClient.Send(message);
            }
        }
        public static bool IsFinalOrCorrected(resultStatusType status)
        {
            return status == resultStatusType.Final || status == resultStatusType.Corrected;
        }
        public static string AlternateCode(string prefix, string code, int padLength)
        {
            string newCode = $"{prefix}{code.PadLeft(padLength, '0')}";
            if (newCode.Length > 6)
            {
                newCode = newCode.Substring(0, 6);
            }
            return newCode;
        }

    }

    public class CompareText : EqualityComparer<string>
    {

        public override bool Equals(string x, string y)
        {
            return x.Equals(y, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }

    }
}