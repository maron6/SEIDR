using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SEIDR.Dynamics.Windows;
using SEIDR.Dynamics.Configurations;

namespace SEIDR.Dynamics
{
    public enum ExceptionLevel
    {
        UI_Basic,
        UI,
        UI_Advanced,
        Background
    }
    public class ExceptionManager
    {    
        /// <summary>
        /// Sets the minimum exception level from user settings for Showing the top message as an <see cref="Alert"/>.
        /// </summary>
        public ExceptionLevel alertLevel { get; set; }
        /// <summary>
        /// Logs the message to file. If the handling level is greater or equal to <see cref="alertLevel"/>, an Alert Dialog will be shown to the user
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="handlingLevel"></param>
        /// <param name="window"></param>
        /// <param name="memberName"></param>
        /// <param name="sourceFilePath"></param>
        /// <param name="sourceLineNumber"></param>
        public virtual void Handle(string Message, ExceptionLevel handlingLevel = ExceptionLevel.UI, BasicSessionWindow window = null,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            if (Message == null)
                return;
            string WindowMessage = window == null ? "" : $"Session Window:'{window.WindowName}'{Environment.NewLine}";
            Exception logException = new Exception($"{WindowMessage}Caller Information: {memberName} {Environment.NewLine}File Path:'{sourceFilePath}'{Environment.NewLine}Line #: {sourceLineNumber}");
            Exception detail = new Exception("UI Message: " + Message, logException);
            WriteToFile(FormatException(detail, false, handlingLevel));            
            if (alertLevel >= handlingLevel && Environment.UserInteractive)
                new Alert(Message, Choice: false).ShowDialog();
        }
        /// <summary>
        /// Sets up an Exception manager. Should be done by Managing application
        /// </summary>
        /// <param name="AppName">Used to get the base folder</param>
        /// <param name="FileNameFormat">File name format, e.g. Log_&gt;YYYY>&gt;MM>&gt;DD> to get Log_20160918 for  </param>
        /// <param name="ex"></param>
        public ExceptionManager(string AppName, string FileNameFormat, ExceptionLevel ex = ExceptionLevel.UI)
        {
            alertLevel = ex;
            MyAppName = AppName;
            fileNameFormat = FileNameFormat;
        }
        string MyAppName;
        string fileNameFormat;
        public const string LOG_SUBFOLDER = "Logs";
        private void WriteToFile(string content)
        {
            string FilePath = GetDirectory();
            if (FilePath == null)
                return;
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
        /// <summary>
        /// User visible log directory
        /// </summary>
        /// <returns></returns>
        public string GetDirectory()
        {
            string fName = fileNameFormat;
            if (string.IsNullOrWhiteSpace(fName))
                return null;
            fName = UserFriendlyDateRegex.Eval(fName);
            return ConfigFolder.GetSafePath(MyAppName, LOG_SUBFOLDER, fName);
        }
        /// <summary>
        /// Logs the exception/Message and displays an alert if the handleLevel is sufficiently high
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="Message"></param>
        /// <param name="handleLevel">Level required to display an alert to users</param>
        /// <param name="window">Calling session window, the WindowName property is included in log details if not null</param>
        /// <param name="memberName">Do not pass a value</param>
        /// <param name="sourceFilePath">Do not pass a value</param>
        /// <param name="sourceLineNumber">Do not pass a value</param>
        public virtual void Handle(Exception ex, string Message, 
            ExceptionLevel handleLevel = ExceptionLevel.Background,
            BasicSessionWindow window = null,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {

            if (Message == null && ex == null)
                return;
            bool exMsg = ex != null;
            string WindowMessage = window == null ? "" : $"Session Window:'{window.WindowName}'{Environment.NewLine}";
            Exception logException = ex ?? new Exception(Message);
            if (Message == null)
                Message = ex.Message;
            else if (Message != ex.Message){                 
                logException = new Exception($"UI Message: {Message}", ex);
                if (alertLevel >= ExceptionLevel.UI_Advanced)
                    Message += Environment.NewLine + Environment.NewLine + "EXCEPTION: " + ex.Message;
            }
            logException = new Exception($"{WindowMessage}Caller Information: {memberName} {Environment.NewLine}File Path:'{sourceFilePath}'{Environment.NewLine}Line #: {sourceLineNumber}", logException);
            
            WriteToFile(FormatException(logException, exMsg, handleLevel));
            
            if( alertLevel >= handleLevel && Environment.UserInteractive)
            {
                new Alert(Message, Choice: false).ShowDialog();
            }
        }
                
        protected static string FormatException(Exception ex, bool ExceptionMessages, ExceptionLevel handlingLevel,
            bool start = true, Exception LastException  = null)
        {
            if (ex == null)
                return "[START " + handlingLevel.ToString().ToUpper() + " " + (ExceptionMessages? ex.GetType().Name.ToUpper() : "MESSAGE") + $" LIST] - {{{System.DateTime.Now.ToString("MMM dd, yyyy hh:mm:ss")}}}" + Environment.NewLine + Environment.NewLine;
            if (LastException != null && ex.Message == LastException.Message)
            {
                return FormatException(ex.InnerException, ExceptionMessages, handlingLevel, false, ex);
            }
            
            string x = (ExceptionMessages? $"[{ex.GetType().Name.ToUpper()}]" : "[MESSAGE]") + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine;
            if (start)
                x = x + Environment.NewLine + "[END]" + Environment.NewLine + Environment.NewLine + Environment.NewLine;
            
            return FormatException(ex.InnerException, ExceptionMessages, handlingLevel, false) + x;
        }
    }
}
