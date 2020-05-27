using MigrationContextUnpack.Sources._7z;
using MigrationContextUnpack.Sources.Tracing;
using System;
using System.IO;
using System.Security;
using System.Text;
using static MigrationContextUnpack.Sources._7z.SevenZipHandler;

namespace MigrationContextUnpack.Sources
{
    public class UnpackHandler
    {
        public static int Unpack(string contextFile, string targetFolder)
        {
            int retval = -15;
            string exceptionMessage = string.Empty;
            string exceptionStack = string.Empty;

            try
            {
                Logger.Log("Unpack "+ contextFile + " started");
                
                if (!File.Exists(contextFile))
                {
                    retval = 1;
                    throw new FileNotFoundException("Context file " + contextFile + " not found");
                }
                if (!Directory.Exists(targetFolder))
                {
                    retval = 2;
                    throw new DirectoryNotFoundException("Target folder " + targetFolder + " not found");
                }

                SevenZipHandler sevenZipper = new SevenZipHandler("7za.exe");
                if (sevenZipper == null)
                {
                    retval = 3;
                    throw new ApplicationException("Seven zip object not created");
                }
                Logger.Log("Seven zip object created");

                SecureString minga = loadPassword();
                Logger.Log("Password {0}found", minga == null ? "NOT " : string.Empty);

                if (!sevenZipper.CheckCompressedFileHealth(contextFile, minga))
                {
                    retval = 4;
                    throw new ApplicationException("File " + contextFile + " health check failed");
                }
                Logger.Log("File health check passed");

                string uncompressedFolder = Path.Combine(targetFolder, "Context");
                if (Directory.Exists(uncompressedFolder))
                {
                    Logger.Log("Removing " + uncompressedFolder);
                    Directory.Delete(uncompressedFolder, true);
                    if (Directory.Exists(uncompressedFolder))
                        throw new ApplicationException("Could not remove folder");
                }
                if (!sevenZipper.Decompress(contextFile, targetFolder, minga))
                {
                    retval = 5;
                    throw new ApplicationException("Decompression failed");
                }

                Logger.Log("All done");
                retval = 0;
            }

            catch (SevenZipHandlerException szhe)
            {
                exceptionMessage = szhe.Message;
                exceptionStack = szhe.StackTrace;
            }

            catch (Exception ex)
            {
                exceptionMessage = ex.Message;
                exceptionStack = ex.StackTrace;
            }

            if (!string.IsNullOrEmpty(exceptionMessage))
            {
                Logger.Log("Exception raised " + exceptionMessage, LogLevel.Error);
                foreach (string line in exceptionStack.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    Logger.Log("   " + line, LogLevel.Error);                
            }

            return retval;
        }

        #region Helpers

        private static SecureString loadPassword()
        {            
            StringBuilder pwd = new StringBuilder(Environment.GetEnvironmentVariable("InstallerStarter.ContextPwd", EnvironmentVariableTarget.Machine));
            if (pwd.Length == 0) return null;
            SecureString retval = new SecureString();
            foreach (char c in pwd.ToString()) retval.AppendChar(c);
            pwd.Clear();
            retval.MakeReadOnly();
            return retval;
        }

        #endregion
    }
}
