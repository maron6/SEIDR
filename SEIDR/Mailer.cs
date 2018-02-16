using System.Net.Mail;


namespace SEIDR
{
    /// <summary>
    /// Wrapper for sending mail messages
    /// </summary>
    public class Mailer
    {
       
        /// <summary>
        /// Default Recipients of email
        /// </summary>
        public string SendTo{ get; set; }
        #region SMTP settings
        /// <summary>
        /// The SMTP Server that your mailer connects to. E.g. gmail.com
        /// </summary>
        public string SMTPServer { get; set; }
        public int Port { get; set; } = 25;
        public System.Net.NetworkCredential SmtpCredential { get; set; } = null;
        public SmtpDeliveryMethod DeliveryMethod { get; set; } = SmtpDeliveryMethod.Network;
        public bool UseSSL { get; set; } = UseSSLDefault;
        public static bool UseSSLDefault { get; set; } = false;
        #endregion

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
        /// <summary>
        /// Name of the sender. Will have Domain added when sending if there is no '@'
        /// </summary>
        public MailAddress Sender;
        string _display;
        public string SenderDisplayName
        {
            get => _display;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _display = null;
                    return;
                }
                _display = value;
            }
        }
        private Mailer(string sendTo, string Server, int Port)
        {
            SendTo = sendTo;
            SMTPServer = Server;            
            this.Port = Port;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mailSender">Address to use for the sender</param>
        /// <param name="SendTo"></param>
        /// <param name="Server"></param>
        /// <param name="Port"></param>
        public Mailer(string mailSender, string SendTo = null, string Server = null, int Port = 25)
            :this(sendTo:SendTo, Server: Server, Port: Port)
        {
            if (mailSender.IndexOf('@') < 0)
                mailSender += "@" + _Domain;

            Sender = new MailAddress(mailSender);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mailSender">MailAddress for the sender to use</param>
        /// <param name="SendTo">Default Recipient of emails</param>
        /// <param name="Server"></param>
        /// <param name="Port"></param>
        public Mailer(MailAddress mailSender, string SendTo = null, string Server = null, int Port = 25)
            :this(SendTo, Server: Server, Port: Port)
        {            
            Sender = mailSender;                     
        }
        /// <summary>
        /// Sends the mail message using the Smtp configurations for the SMTP server
        /// </summary>
        /// <param name="message"></param>
        public void SendMail(MailMessage message)
        {
            // Create a SMTP client to send the email            
            using (SmtpClient mySmtpClient = new SmtpClient(SMTPServer, Port))
            {
                mySmtpClient.EnableSsl = UseSSL;
                mySmtpClient.DeliveryMethod = DeliveryMethod;
                if (SmtpCredential != null)
                {
                    mySmtpClient.Credentials = SmtpCredential;
                    mySmtpClient.UseDefaultCredentials = false;
                }
                else
                    mySmtpClient.UseDefaultCredentials = true;

                mySmtpClient.Send(message);
            }
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
        /// <param name="CCList">List of mail addresses to CC</param>
        /// <param name="BCCList">List of mail addresses to BCC</param>
        /// <param name="replyToList">List of mail address for recipients to reply to, if supported by SmtpServer. (In addition to Sender)</param>
        /// <param name="attachmentList">Optional List of attachments to include when sending email</param>
        /// <param name="mailPriority">Message priority</param>
        public void SendMail(string subject, string MailBody, bool isHtml = true, 
            MailAddress recipient = null,
            string CCList = null, string BCCList = null, string replyToList = null, 
            System.Collections.Generic.ICollection<Attachment> attachmentList = null, 
            MailPriority mailPriority = MailPriority.Normal)
        {
            if (Sender == null || SMTPServer == null)
                return;
            if (recipient == null)
                recipient = new MailAddress(SendTo);
            if (recipient == null)
                throw new System.ArgumentNullException(nameof(recipient));

            if (string.IsNullOrWhiteSpace(SenderDisplayName))
                SenderDisplayName = _display;
            // Create an email and change the format to HTML
            MailAddress from = new MailAddress(Sender.Address, SenderDisplayName);                 
            MailMessage mail = new MailMessage(from, recipient)
            {
                Subject = subject,
                Body = MailBody,
                IsBodyHtml = isHtml,
                Priority = mailPriority                
            };
            
            if (!string.IsNullOrWhiteSpace(CCList))
            {
                mail.CC.Add(CCList);
            }
            if (!string.IsNullOrWhiteSpace(BCCList))
            {
                mail.Bcc.Add(BCCList);
            }
            if (!string.IsNullOrWhiteSpace(replyToList))
            {             
                mail.ReplyToList.Add(replyToList);
            }
            if(attachmentList != null)
            {
                attachmentList.ForEach(a => mail.Attachments.Add(a));
            }
            SendMail(mail);
        }
    }
    
}
