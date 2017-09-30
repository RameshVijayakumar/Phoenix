using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;
using Phoenix.Common;

namespace Phoenix.Web.Models
{
    public static class EmailHelper
    {
        public static void SendEmail(MailMessage mail)
        {
            NameValueCollection appSettings = WebConfigurationManager.AppSettings;
            if (appSettings != null)
            {
                if (mail.From == null)
                {
                    mail.From = new MailAddress(appSettings["FromEmailId"], "Menu Master");
                }

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new System.Net.NetworkCredential(appSettings["FromEmailId"], appSettings["FromEmailPass"]);
                smtp.EnableSsl = true;

                // timeout after 30seconds
                smtp.Timeout = 30000;

                smtp.Send(mail);

                Logger.WriteInfo("Mail sent to " + mail.To.First());
            }
        }

        public static void SendEmail(string to, string subject, string body)
        {
            string from = string.Empty;
            NameValueCollection appSettings = WebConfigurationManager.AppSettings;
            if (appSettings != null)
            {
                from = appSettings["FromEmailId"];
            }
            SendEmail(new MailMessage(from, to, subject, body));
        }

    }
}