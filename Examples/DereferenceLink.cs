using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    static class DereferenceLink {
        /// <summary>
        /// Example illustrating how to dereference a symbolic link
        /// in a file listing. You can also pass the FtpListOption.DerefLinks
        /// flag to GetListing() to have automatically done in which
        /// case the FtpListItem.LinkObject property will contain the
        /// FtpListItem representing the object the link points at. The
        /// LinkObject property will be null if there was a problem resolving
        /// the target.
        /// </summary>
        public static void DereferenceLinkExample() {
            using (FtpClient client = new FtpClient()) {
                client.Credentials = new NetworkCredential("user", "pass");
                client.Host = "somehost";

                // This propety controls the depth of recursion that
                // can be done before giving up on resolving the link.
                // You can set the value to -1 for infinite depth 
                // however you are strongly discourage from doing so.
                // The default value is 20, the following line is
                // only to illustrate the existance of the property.
                // It's also possible to override this value as one
                // of the overloaded arguments to the DereferenceLink() method.
                client.MaximumDereferenceCount = 20;

                // Notice the FtpListOption.ForceList flag being passed. This is because
                // symbolic links are only supported in UNIX style listings. My personal
                // experience has been that in practice MLSD listings don't specify an object
                // as a link, but rather list the link as a regular file or directory
                // accordingly. This may not always be the case however that's what I've
                // observed over the life of this project so if you run across the contrary
                // please report it. The specification for MLSD does include links so it's
                // possible some FTP server implementations do include links in the MLSD listing.
                foreach (FtpListItem item in client.GetListing(null, FtpListOption.ForceList | FtpListOption.Modify)) {
                    Console.WriteLine(item);

                    // If you call DerefenceLink() on a FtpListItem.Type other
                    // than Link a FtpException will be thrown. If you call the
                    // method and the LinkTarget is null a FtpException will also
                    // be thrown.
                    if (item.Type == FtpFileSystemObjectType.Link && item.LinkTarget != null) {
                        item.LinkObject = client.DereferenceLink(item);

                        // The return value of DerefenceLink() will be null
                        // if there was a problem.
                        if (item.LinkObject != null) {
                            Console.WriteLine(item.LinkObject);
                        }
                    }
                }

                // This example is similar except it uses the FtpListOption.DerefLinks
                // flag to have symbolic links automatically resolved. You must manually
                // specify this flag because of the added overhead with regards to resolving
                // the target of a link.
                foreach (FtpListItem item in client.GetListing(null,
                    FtpListOption.ForceList | FtpListOption.Modify | FtpListOption.DerefLinks)) {

                    Console.WriteLine(item);

                    if (item.Type == FtpFileSystemObjectType.Link && item.LinkObject != null) {
                        Console.WriteLine(item.LinkObject);
                    }
                }
            }
        }
    }
}
