using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using static SEIDR.WindowMonitor.SettingManager;
using System.IO;
using SEIDR.Dynamics.Windows;
using SEIDR.Dynamics;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Maybe ToDo: Add an interface to allow overriding the destination of log messages by setting an iLog with methods that can correspond to the levels?
    /// </summary>
    public static class sExceptionManager
    {                
        public static ExceptionLevel alertLevel { get; set; }
        public static void Handle(string Message, ExceptionLevel handlingLevel = ExceptionLevel.UI,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            if (Message == null)
                return;            
            Exception logException = new Exception($"Caller Information: {memberName} {Environment.NewLine}File Path:'{sourceFilePath}'{Environment.NewLine}Line #: {sourceLineNumber}");
            Exception detail = new Exception("UI Message: " + Message, logException); 
            WriteToFile(FormatException(detail, false, handlingLevel));            
            if (alertLevel >= handlingLevel)
                new Alert(Message, Choice: false).ShowDialog();
        }
        public const string LOG_SUBFOLDER = "Logs";
        private static void WriteToFile(string content)
        {
            string fName = myMiscSettings.ErrorLog;            
            if (string.IsNullOrWhiteSpace(fName))
                return;
            fName = UserFriendlyDateRegex.Eval(fName);
            string FilePath = SEIDR.Dynamics.Configurations.ConfigFolder.GetSafePath(MyAppName, LOG_SUBFOLDER , fName);
            try
            {
                File.AppendAllText(FilePath, content);
            }
            catch (Exception e)
            {
                if (alertLevel >= ExceptionLevel.UI)
                {
                    new Alert("Unable to log exception to log file because: " + e.Message, Choice: false).ShowDialog();
                }
            }
        }
        public static void Handle(Exception ex, string Message, ExceptionLevel handleLevel = ExceptionLevel.Background,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {

            if (Message == null && ex == null)
                return;
            bool exMsg = ex != null;
            Exception logException = ex ?? new Exception(Message);
            if (Message == null)
                Message = ex.Message;
            else if (Message != ex.Message){                 
                logException = new Exception($"UI Message: {Message}", ex);
                if (alertLevel >= ExceptionLevel.UI_Advanced)
                    Message += Environment.NewLine + Environment.NewLine + "EXCEPTION: " + ex.Message;
            }
            logException = new Exception($"Caller Information: {memberName} {Environment.NewLine}File Path:'{sourceFilePath}'{Environment.NewLine}Line #: {sourceLineNumber}", logException);
            
            WriteToFile(FormatException(logException, exMsg, handleLevel));
            
            if( alertLevel >= handleLevel)
            {
                new Alert(Message, Choice: false).ShowDialog();
            }
        }
                
        private static string FormatException(Exception ex, bool ExceptionMessages, ExceptionLevel loggedExceptionLevel,
            bool start = true, Exception LastException  = null)
        {
            if (ex == null)
                return "[START " + loggedExceptionLevel.ToString().ToUpper() + " " + (ExceptionMessages? "EXCEPTION" : "MESSAGE") + $" LIST] - {{{System.DateTime.Now.ToString("MMM dd, yyyy hh:mm:ss")}}}" + Environment.NewLine + Environment.NewLine;
            if (LastException != null && ex.Message == LastException.Message)
            {
                return FormatException(ex.InnerException, ExceptionMessages, loggedExceptionLevel, false, ex);
            }
            
            string x = (ExceptionMessages? "["+ ex.GetType().Name.ToUpper() + "]" : "[MESSAGE]") + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine;
            if (start)
                x = x + Environment.NewLine + "[END]" + Environment.NewLine + Environment.NewLine + Environment.NewLine;
            

            return FormatException(ex.InnerException, ExceptionMessages, loggedExceptionLevel, false) + x;
        }
    }
}
