﻿using CsvHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace XESmartTarget.Core.Utils
{
    public class DataTableCSVAdapter
    {
        private DataTable Table { get; set; }
        public String OutputFile { get; set; }

        public DataTableCSVAdapter(DataTable table)
        {
            Table = table;
        }

        public DataTableCSVAdapter(DataTable table, String outFile)
        {
            OutputFile = outFile;
            Table = table;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void WriteToFile(bool writeHeaders)
        {
            using (BufferedStream f = new BufferedStream(new FileStream(OutputFile, FileMode.Append, FileAccess.Write)))
            {
                using (TextWriter textWriter = new StreamWriter(f))
                {
                    var csv = new CsvWriter(textWriter);

                    if (writeHeaders)
                    {
                        foreach (DataColumn dc in Table.Columns)
                        {
                            csv.WriteField(dc.ColumnName);
                        }
                        csv.NextRecord();
                    }

                    foreach (DataRow dr in Table.Rows)
                    {
                        foreach (DataColumn dc in Table.Columns)
                        {
                            csv.WriteField(dr[dc.ColumnName]);
                        }
                        csv.NextRecord();
                    }

                    csv.Flush();
                }
            }

        }
       
    }
}
