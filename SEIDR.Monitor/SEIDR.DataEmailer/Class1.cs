using SEIDR.DataBase;
using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics.Configurations.UserConfiguration;
//using SEIDR.Processing.Data.DBObjects;
using SEIDR.Dynamics.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
//using SEIDR.MISC;

namespace SEIDR.DataEmailer
{
    [Export(typeof(SEIDR_WindowMenuAddOn)),
       ExportMetadata("Name", "DataTable Mail Export - Setup"),
       ExportMetadata("RequirePermission", false),       
       ExportMetadata("HasParameterMapping", true),
       ExportMetadata("Team", null)]
    public class pluginSetup : SEIDR_WindowMenuAddOn
    {
        public SEIDR_Window callerWindow
        {
            get;  set;
        }

        public Dictionary<string, Type> GetParameterInfo()
        {
            return new Dictionary<string, Type>
            {
                {"SMTP Server", typeof(string) },
                {"Domain", typeof(string) },
                {"Mail Subject", typeof(string) } //Todo: Test if this can encrypt emails when the SMTP servers 
                                                  //uses subject name, even if not from an actual user. Probably does...
            };
        }
        public static string MailSubject = string.Empty;
        /// <summary>
        /// Singleton plugin that sets up a mailer for use in queries when the main window is populating the menu
        /// <para>returns null so that a menu item isn't added</para>
        /// </summary>
        /// <param name="User"></param>
        /// <param name="Connection"></param>
        /// <param name="internalName"></param>
        /// <param name="setParameters"></param>
        /// <returns></returns>
        public System.Windows.Controls.MenuItem Setup(BasicUser User, DatabaseConnection Connection, int internalID, Dictionary<string, object> setParameters)
        {
            if (pluginMailer == null || setParameters["Domain"] as string != null && setParameters["SMTP Server"] as string != null)
            {
                pluginMailer = new Mailer($"{User.UserName} <SEIDR.DataMailer@{setParameters["Domain"] as string}.com>");
                if(setParameters["SMTP Server"] as string != null)
                    Mailer.SMTPServer = setParameters["SMTP Server"] as string;
            }
            string x = setParameters["Mail Subject"] as string;
            if(x != null)
                MailSubject = x;
            return null;
        }
        public static Mailer pluginMailer;
    }
    [Export(typeof(SEIDR_WindowAddOn)),
        ExportMetadata("AddonName", "DataTable Mail Export"),
        ExportMetadata("Description", "Attempts to mail the data of the selected rows as an HTML Table in a NON Encrypted email."),
        ExportMetadata("IDNameParameterName", "Email To List"),
        ExportMetadata("IDNameParameterTooltip", "Comma delimited emails to send the data to.\nOverrides the mapped setting unless empty"),
        ExportMetadata("MultiSelect", true)]
    public class Class1: SEIDR_WindowAddOn
    {
        public BasicUser Caller
        {
            get; set;
        }
        public DatabaseConnection Connection
        {
            get; set;
        }


        public string IDName
        {
            get; set;
        }

        public int? IDValue
        {
            get; set;
        }

        public DataRowView GetParameterInfo()
        {
            using (var pHelper = new SEIDR_WindowAddOn_ParameterHelper()) {
                pHelper.AddParameter(SMTP_SERVER, typeof(string));
                pHelper.AddParameter(SENDER_DOMAIN, typeof(string), "Domain to use for the sender (e.g. Seidr.Window@Domain.com)", Environment.UserDomainName);
                pHelper.AddParameter(MAIL_SUBJ, typeof(string), "Subject of emails to send.");
                pHelper.AddParameter(RECIPIENTS, typeof(string), "List of users to send the email to.");
                return pHelper.GetParameters();
            }
                 //Todo: Test if this can encrypt emails when the SMTP servers 
                                                   //uses subject name, even if not from an actual user. Probably does...
        }
        const string SMTP_SERVER = "SMTP Server";
        const string SENDER_DOMAIN = "Domain";
        const string MAIL_SUBJ = "Mail Subject";
        const string RECIPIENTS = "Recipients";
        /// <summary>
        /// Not used because MultiSelect is true.
        /// </summary>
        /// <param name="selectedRow"></param>
        /// <param name="Parameters"></param>
        /// <returns></returns>
        public string Execute(DataRowView selectedRow, Dictionary<string, object> Parameters)
        {
            throw new NotImplementedException();
        }
        
        public string Execute(DataRowView[] selectedRows, Dictionary<string, object> Parameters)
        {
            string re = Parameters[RECIPIENTS] as string;
            string Dom = Parameters[SENDER_DOMAIN] as string;
            if (Dom.Contains("@"))
                Dom = Dom.Substring(Dom.LastIndexOf('@') + 1);
            if (selectedRows.Length > 0 && !string.IsNullOrWhiteSpace(re) && ! string.IsNullOrWhiteSpace(Dom) ) {
                Mailer m = new Mailer("SEIDR.Window@" + Dom, re);
                
                DataTable d = selectedRows[0].Row.Table;
                string htmlEmail = "<h3>SEIDR Query Data</h3><p>&nbsp</p><table style='background-color: .LightSteelBlue, width=100%'>";
                bool HasColor = false;
                string Header = "<tr>";
                foreach(DataColumn col in d.Columns)
                {
                    string tcol = col.ColumnName.ToUpper();
                    if (tcol == "COLOR")
                    {
                        HasColor = true;
                        continue;
                    }
                    if (tcol.StartsWith("HDN") || tcol.StartsWith("DTL_"))
                        continue;
                    Header += $@"<th>{col.ColumnName}</th>";  
                }
                if (Header == "<tr>")
                    return null;
                htmlEmail += Header + "</tr>" + Environment.NewLine;
                foreach (var row in selectedRows)
                {
                    string rowHTML = "<tr>";
                    if (HasColor)
                    {
                        rowHTML = $"<tr style='background-color: .{row["Color"].ToString()}>";
                    }
                    foreach(DataColumn col in d.Columns)
                    {
                        string tempCol = col.ColumnName.ToUpper();
                        if (tempCol == "COLOR" || tempCol.StartsWith("HDN") || tempCol.StartsWith("DTL_"))
                            continue;
                        rowHTML += $"<td>{row[col.ColumnName]?.ToString() ?? ""}</td>";
                    }
                    htmlEmail += rowHTML + "</tr>";
                }
                htmlEmail += @"</table>";
                try
                {
                    if (string.IsNullOrWhiteSpace(IDName))
                        IDName = null;
                    m.SendMailAlert(Parameters[MAIL_SUBJ] as string, htmlEmail, true, IDName);
                    //pluginSetup.pluginMailer.SendMailAlert(pluginSetup.MailSubject, htmlEmail, recipient: IDName);
                    return null;
                }
                catch(Exception ex)
                {
                    return "Unable to send e-mail: " + ex.Message;
                }
            }
            return "No data to convert to HTML table";
        }

        public string Execute(IEnumerable<DataRowView> selectedRows, Dictionary<string, object> Parameters)
        {
            throw new NotImplementedException();
        }
    }
}
