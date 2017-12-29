using System.Net.Mail;


namespace SEIDR
{
    /// <summary>
    /// Wrapper for sending mail messages
    /// </summary>
    public class Mailer
    {
       
        /// <summary>
        /// Recipients of email
        /// </summary>
        public string SendTo{ get; set; }
        /// <summary>
        /// The SMTP Server that your mailer connects to. E.g. gmail.com
        /// </summary>
        public static string SMTPServer { get; set; }
        public static int Port { get; set; } = 25;
        static string _Domain;
        /// <summary>
        /// Domain, e.g. gmail, gmail.com, @gmail.com
        /// <para>Stored without the '@'</para>
        /// </summary>
        public static string Domain
        {
            get { return _Domain; }
            set
            {
                if (value.IndexOf(".") < 0)
                    _Domain = value.Replace("@", "") + ".com";
                else
                    _Domain = value.Replace("@", "");
            }
        }        
        string mailAddresses;
        /// <summary>
        /// Name of the sender. Will have Domain added when sending if there is no '@'
        /// </summary>
        public string sender;
        
        /// <summary>
        /// Constructor. Requires name of application for in the eventthat the DS_Application_Settings table is used
        /// </summary>
        /// <param name="mailSender">Name of the sender to use</param>
        /// <param name="SendTo">Recipient of emails</param>
        public Mailer(string mailSender = null, string SendTo = null)
        {            
            sender = mailSender;                        
        }        

        /// <summary>
        /// Sends a mail from the specified sender
        /// <para>Will send to the mail list specified by SendTo</para>
        /// <para>Note: If sender or SMTP Server is null, will return immediately.</para>
        /// </summary>
        /// <param name="subject">Email's subject line</param>
        /// <param name="MailBody">Content making up the email's body.</param>
        /// <param name="isHtml">If true, sends as an HTML mail</param>
        /// <param name="recipient">Allow for overriding the SendToList without overriding for overall</param>
        public void SendMailAlert(string subject, string MailBody, bool isHtml = true, string recipient = null)
        {
            if (sender == null || SMTPServer == null)
                return;
            if (sender.IndexOf("@") < 0)
                sender += "@" + Domain;
            if (string.IsNullOrWhiteSpace(recipient))
                recipient = SendTo;   
            // Create an email and change the format to HTML
            MailMessage myHtmlFormattedMail = new MailMessage(sender, recipient, subject, MailBody);
            myHtmlFormattedMail.IsBodyHtml = isHtml;

            // Create a SMTP client to send the email            
            using (SmtpClient mySmtpClient = new SmtpClient(SMTPServer))
            {
                mySmtpClient.Port = Port;
                mySmtpClient.EnableSsl = useSSL;
                mySmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                mySmtpClient.UseDefaultCredentials = true;

                mySmtpClient.Send(myHtmlFormattedMail);
            }            
        }
        public static bool useSSL { get; set; } = false;
    }
    
}
