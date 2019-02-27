using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Examples {
    class MainClass {
        public static async Task Main(string[] args) {
            Debug.Listeners.Add(new ConsoleTraceListener());

            try {
                // await AsyncConnectExample.AsyncConnectAsync();
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
