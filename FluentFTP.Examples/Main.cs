using System;
using System.Diagnostics;

namespace Examples {
    class MainClass {
        public static void Main(string[] args) {
            Debug.Listeners.Add(new ConsoleTraceListener());

            try {
                //BeginConnectExample.BeginConnect();
                //BeginCreateDirectoryExample.BeginCreateDirectory();
                //BeginDeleteDirectoryExample.BeginDeleteDirectory();
                //BeginDeleteFileExample.BeginDeleteFile();
                //BeginDirectoryExistsExample.BeginDirectoryExists();
                //BeginDisconnectExample.BeginDisconnect();
                //BeginDownloadExample.BeginDownload();
                //BeginExecuteExample.BeginExecute();
                //BeginFileExistsExample.BeginFileExists();
                //BeginGetFileSizeExample.BeginGetFileSize();
                //BeginGetListing.BeginGetListingExample();
                //BeginGetModifiedTimeExample.BeginGetModifiedTime();
                //BeginGetWorkingDirectoryExample.BeginGetWorkingDirectory();
                //BeginRenameExample.BeginRename();
                //BeginSetDataTypeExample.BeginSetDataType();
                //BeginSetWorkingDirectoryExample.BeginSetWorkingDirectory();
                //OpenReadURI.OpenURI();
                //BeginGetNameListingExample.BeginGetNameListing();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }

            //Console.WriteLine("--DONE--");
            Console.ReadKey();
        }
    }
}
