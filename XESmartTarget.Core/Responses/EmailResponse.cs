﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.XEvent.Linq;
using NLog;
using System.Data;
using XESmartTarget.Core.Utils;
using System.Net.Mail;
using SmartFormat;
using System.IO;

namespace XESmartTarget.Core.Responses
{
    [Serializable]
    public class EmailResponse : Response
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public string SMTPServer { get; set; }
        public string Sender { get; set; }
        public string To { get; set; }
        public string Cc { get; set; }
        public string Bcc { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool HTMLFormat { get; set; }
        public string Attachment { get; set; }
        public string AttachmentFileName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        protected DataTable EventsTable = new DataTable("events");
        private XEventDataTableAdapter xeadapter;

        public EmailResponse()
        {
            logger.Info(String.Format("Initializing Response of Type '{0}'", this.GetType().FullName));
        }

        public override void Process(PublishedEvent evt)
        {
            if (xeadapter == null)
            {
                xeadapter = new XEventDataTableAdapter(EventsTable);
                xeadapter.Filter = this.Filter;
                xeadapter.OutputColumns = new List<string>();
            }
            xeadapter.ReadEvent(evt);

            lock (EventsTable)
            {

                foreach (DataRow dr in EventsTable.Rows)
                {
                    string formattedBody = Body;
                    string formattedSubject = Subject;

                    Dictionary<string, object> tokens = new Dictionary<string, object>();
                    foreach (DataColumn dc in EventsTable.Columns)
                    {
                        tokens.Add(dc.ColumnName, dr[dc]);
                    }
                    formattedBody = Smart.Format(Body, tokens);
                    formattedSubject = Smart.Format(Subject, tokens);

                    using (MailMessage msg = new MailMessage(Sender, To, formattedSubject, formattedBody))
                    {
                        using (MemoryStream attachStream = new MemoryStream())
                        {


                            if (!String.IsNullOrEmpty(Attachment) && dr.Table.Columns.Contains(Attachment))
                            {

                                StreamWriter wr = new StreamWriter(attachStream);
                                wr.Write(dr[Attachment].ToString());
                                wr.Flush();
                                attachStream.Position = 0;

                                System.Net.Mime.ContentType ct = new System.Net.Mime.ContentType(System.Net.Mime.MediaTypeNames.Text.Plain);
                                Attachment at = new Attachment(attachStream, ct);
                                at.ContentDisposition.FileName = AttachmentFileName;
                                msg.Attachments.Add(at);

                            }
                            msg.IsBodyHtml = HTMLFormat;

                            using (SmtpClient client = new SmtpClient(SMTPServer))
                            {
                                if (!String.IsNullOrEmpty(UserName))
                                {
                                    client.Credentials = new System.Net.NetworkCredential(UserName, Password);
                                }
                                // could be inefficient: sends synchronously
                                client.Send(msg);
                            }

                        }

                    }

                }

                EventsTable.Clear();

            }


        }
    }
}
