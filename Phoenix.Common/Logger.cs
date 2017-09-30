using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Profile;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Text.RegularExpressions;
using System.IO;

namespace Phoenix.Common
{
    public class Logger
    {
        #region Properties

        public const int EventTypeIndex = 0;
        public const int UsernameIndex = 1;
        public const int MessageIndex = 2;

        private const string UNKNOWN = "unknown";
        public const char MESSAGE_DELIMITER = '^';


        public enum EventType
        {
            Critical,
            Error,
            Warning,
            Information,
            Verbose,
            Audit,
            APIAudit
        }

        #endregion

        #region Overloaded Public Methods

        public static void Write(string message, EventType eventType, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            writeTraceLog(message, eventType, null, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void WriteInfo(string message, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            writeTraceLog(message, EventType.Information, null, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void WriteAudit(string message)
        {
            writeTraceLog(message, EventType.Audit, null, null, null, 0);

        }

        public static void WriteAPIAudit(string message)
        {
            writeTraceLog(message, EventType.APIAudit, null, null, null, 0);

        }

        public static void WriteError(string message, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            writeTraceLog(message, EventType.Error, null, memberName, sourceFilePath, sourceLineNumber);

        }

        public static void WriteError(string message, Exception ex,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            writeTraceLog(message, EventType.Error, ex, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void WriteError(Exception ex,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            writeTraceLog(string.Empty, EventType.Error, ex, memberName, sourceFilePath, sourceLineNumber);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// This method writes log to the trace or to a filesystem depending on the running config
        /// </summary>
        /// <param name="msg">Message to be written</param>
        /// <param name="eventType">Category of the log</param>
        private static void writeTraceLog(string msg, EventType eventType, Exception ex, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0)
        {

            // format the exception details
            if (ex != null)
            {
                msg = string.Format("{0} Exception:{1}", msg, ex);
                //msg = string.Format("{0} Exception:{1}{2}{3}",
                //                    msg, 
                //                    ex.Message,
                //                    ex.StackTrace,
                //                    ex.InnerException == null ? string.Empty : ex.InnerException.Message,
                //                    ex.InnerException == null ? string.Empty : ex.InnerException.StackTrace);
            }

            // format caller info
            if (string.IsNullOrEmpty(memberName) == false)
            {
                msg = string.Format("{0} [Caller:{1}->{2},{3}]", msg, Path.GetFileName(@sourceFilePath), memberName, sourceLineNumber);
            }

            // add extra info for audit
            if (eventType == EventType.Audit)
            {
                // try getting the username
                string userName = string.Empty;
                try
                {
                    // get the current user name
                    ProfileBase profile = HttpContext.Current.Profile;
                    userName = string.Format("{0} {1}", profile.GetPropertyValue("FirstName"), profile.GetPropertyValue("LastName"));
                }
                catch
                {
                    // do nothing
                }

                if (string.IsNullOrEmpty(userName) || string.IsNullOrWhiteSpace(userName))
                {
                    userName = UNKNOWN;
                }

                msg = string.Format("{0}{1}{2}{3}", MESSAGE_DELIMITER, userName, MESSAGE_DELIMITER, msg);
            }
            // remove control characters -- like the msg from XMLException
            msg = Regex.Replace(msg, @"(?![\r\n])\p{Cc}", string.Empty);

            // write to log
            Trace.WriteLine(msg, Enum.GetName(typeof(EventType), eventType));


            //if (RoleEnvironment.IsAvailable)                            
            //{
            //    Trace.WriteLine(msg, Enum.GetName(typeof(EventType), eventType));
            //}
            //else
            //{
            //    System.IO.Directory.CreateDirectory(@"c:\temp");
            //    System.IO.File.AppendAllText(@"c:\temp\AzureTraceLog.txt", string.Format("{0} : {1}:{2}{3}",
            //        DateTime.Now.ToString("MM/dd/yy HH:mm:ss"),
            //        eventType,
            //        msg,
            //        Environment.NewLine));
            //}
        }

        #endregion

    }
}
