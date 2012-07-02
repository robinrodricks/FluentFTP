using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace System.Net.FtpClient.Tests {
    public class Config {
        /// <summary>
        /// The test upload all of the files in the FtpSourceCode directory as
        /// part of the test procedures
        /// </summary>
        public static readonly string FtpSourceCode = @"..\..\..\..\";
        public static readonly string FtpServer = "localhost";
        public static readonly int FtpPort = 21;
        public static readonly string FtpUser = "ftptest";
        public static readonly string FtpPass = "ftptest";

        public static System.IO.FileStream OpenTransactionLog() {
            StackTrace trace = new StackTrace();

            return new IO.FileStream(string.Format("{0}-TransactionLog.txt", trace.GetFrame(1).GetMethod().Name), 
                IO.FileMode.OpenOrCreate, IO.FileAccess.Write);
        }
    }
}
