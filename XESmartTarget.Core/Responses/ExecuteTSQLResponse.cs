﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Microsoft.SqlServer.XEvent.Linq;
using System.Data.SqlClient;
using System.Data;
using XESmartTarget.Core.Utils;
using SmartFormat;

namespace XESmartTarget.Core.Responses
{
    [Serializable]
    public class ExecuteTSQLResponse : Response
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();


        public string TSQL { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }


        protected DataTable EventsTable = new DataTable("events");
        private XEventDataTableAdapter xeadapter;


        protected string ConnectionString
        {
            get
            {
                int ConnectionTimeout = 15;
                string s = "Server=" + ServerName + ";";
                s += "Database=" + DatabaseName + ";";
                if (String.IsNullOrEmpty(UserName))
                {
                    s += "Integrated Security = True;";
                }
                else
                {
                    s += "User Id=" + UserName + ";";
                    s += "Password=" + Password + ";";
                }
                s += "Connection Timeout=" + ConnectionTimeout;
                logger.Debug(s);
                return s;
            }
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
                    Dictionary<string, object> tokens = new Dictionary<string, object>();
                    foreach (DataColumn dc in EventsTable.Columns)
                    {
                        tokens.Add(dc.ColumnName, dr[dc]);
                    }

                    string formattedTSQL = Smart.Format(TSQL, tokens);

                    Task t = Task.Factory.StartNew(() => ExecuteTSQL(formattedTSQL));
                }
            }
        }


        private void ExecuteTSQL(string TSQLString)
        {
            logger.Trace("Executing TSQL command");
            using (SqlConnection conn = new SqlConnection())
            {

                try
                {
                    conn.ConnectionString = ConnectionString;
                    conn.Open();
                }
                catch(Exception e)
                {
                    logger.Error(String.Format("Error: {0}", e.Message));
                    throw;
                }
                

                try
                {

                    SqlCommand cmd = new SqlCommand(TSQLString);
                    cmd.Connection = conn;
                    cmd.ExecuteNonQuery();
                    logger.Trace(String.Format("SUCCES - {0}", TSQLString));
                }
                catch (SqlException e)
                {
                    logger.Error(String.Format("Error: {0}", TSQLString));
                    throw;
                }


            }
        }

    }
}
