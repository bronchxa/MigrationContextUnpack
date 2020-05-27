using MigrationContextUnpack.Sources.CSharp;
using MigrationContextUnpack.Sources.Tracing;
using MigrationContextUnpack.Sources.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using static MigrationContextUnpack.Sources._7z.SevenZipHandler.SevenZipReturnCodeExt;
using static MigrationContextUnpack.Sources.CSharp.FileSystemInfoExtension;

namespace MigrationContextUnpack.Sources._7z
{
    public class SevenZipHandler
    {
        internal class SevenZipReturnCodeExt
        {
            public enum SevenZipReturnCode
            {
                UnknownCode = -1,
                NoError = 0,
                Warning = 1,
                Fatal = 2,
                CommandLineError = 7,
                NotEnoughMemoryForOperation = 8,
                UserAbort = 255
            }

            public static bool TryParse(string value, out SevenZipReturnCode result)
            {
                result = SevenZipReturnCode.UnknownCode;

                return Enum.TryParse<SevenZipReturnCode>(value, out result);
            }
            public static SevenZipReturnCode Parse(string value)
            {
                var retval = (SevenZipReturnCode)Enum.Parse(typeof(SevenZipReturnCode), value);
                if (retval == SevenZipReturnCode.UnknownCode) throw new SevenZipHandlerException("Unsupported 7Z return code");
                return retval;
            }
            public static string ToString(SevenZipReturnCode sevenZipReturnCode)
            {
                switch (sevenZipReturnCode)
                {
                    case SevenZipReturnCode.NoError: return "No error";
                    case SevenZipReturnCode.Warning: return "Warning";
                    case SevenZipReturnCode.Fatal: return "Fatal error";
                    case SevenZipReturnCode.CommandLineError: return "Command line error";
                    case SevenZipReturnCode.NotEnoughMemoryForOperation: return "Not enough memory for operation";
                    case SevenZipReturnCode.UserAbort: return "User stopped the process";
                }
                throw new SevenZipHandlerException("Unsupported 7Z return code: " + sevenZipReturnCode);
            }
        }

        public class SevenZipHandlerException : CustomException
        {
            public SevenZipHandlerException(string message) : base(message) { }
        }

        private string sevenZipExe;


        public SevenZipHandler(string sevenZipExe)
        {
            Process p = null;
            try
            {
                p = Process.Start(new ProcessStartInfo("where.exe", "\"" + sevenZipExe + "\"") { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden});
                p.WaitForExit();
                if (p.ExitCode != 0 && !File.Exists(sevenZipExe))
                    throw new SevenZipHandlerException("File " + sevenZipExe + " not found");
                this.sevenZipExe = sevenZipExe;
            }
            finally
            {
                if (p == null) p.Close();
            }

        }


        private SevenZipReturnCode run(string arguments, out string output)
        {
            output = string.Empty;
            Process process = null;
            StringBuilder dataReceived = new StringBuilder();
            try
            {
                var processName = Path.GetFileName(sevenZipExe);
                process = new Process()
                {
                    StartInfo = new ProcessStartInfo(sevenZipExe)
                    {
                        Arguments = arguments,
                        CreateNoWindow = true,
                        ErrorDialog = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    },
                    EnableRaisingEvents = true,
                };
                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        dataReceived.AppendLine(e.Data);
                        Logger.Log(string.Concat("\t", e.Data));
                    }
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        dataReceived.AppendLine(e.Data);
                        Logger.Log(string.Concat("\t", e.Data), LogLevel.Warning);
                    }
                };
                var args = process.StartInfo.Arguments + (process.StartInfo.Arguments.IndexOf(" -y") < 0 ? " -y" : string.Empty);
                Logger.Log(@"Launching {0} {1}", process.StartInfo.FileName, args);
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();
                Logger.Log("{0} returned: {1}", processName, process.ExitCode.ToString());
                return SevenZipReturnCodeExt.Parse(process.ExitCode.ToString());
            }
            catch (Exception ex)
            {
                throw new SevenZipHandlerException(ex.Message) { StackTrace = ex.StackTrace };
            }
            finally
            {
                output = dataReceived.ToString();
                if (process != null) process.Dispose();
            }
        }

        public bool AddFile(string inputFile, FileSystemInfo fileToAdd, SecureString minga = null, string outputFile = null)
        {
            return AddFiles(inputFile, new FileSystemInfo[] { fileToAdd }, minga, outputFile);
        }
        public bool AddFiles(string inputFile, FileSystemInfo[] filesToAdd, SecureString minga = null, string outputFile = null)
        {            
            string output = string.Empty;
            try
            {
                if (!File.Exists(inputFile))
                    throw new FileNotFoundException("File " + inputFile + " not found");

                if (filesToAdd == null) throw new FileSystemItemNullException();
                if (filesToAdd.Length == 0) throw new FileSystemItemNoItem();

                string itemsToAddStr = string.Empty;
                filesToAdd.ToList().ForEach(i =>
                {
                    if (!i.Exists) throw new FileSystemItemNotFoundException(i);
                    itemsToAddStr += @"""" + i.FullName + @""" ";
                });
                
                Logger.Log("Adding {0} into {1}", itemsToAddStr, inputFile, LogLevel.Debug);
                var args = string.Format(@"a -mhe{0} -t7z ""{1}"" {2} {3}", (minga == null ? string.Empty : string.Format(" -p{0}", minga.SecureStringToString())), inputFile, filesToAdd);
                SevenZipReturnCode returnCode = run(args, out output);
                if (!string.IsNullOrEmpty(outputFile)) Filesystem.CreateTextFile(outputFile, output);
                if (SevenZipReturnCode.NoError != returnCode && SevenZipReturnCode.Warning != returnCode)
                    throw new SevenZipHandlerException("Update error: " + SevenZipReturnCodeExt.ToString(returnCode));
                
                Logger.Log("File {0} updated", inputFile, LogLevel.Debug);

                return true;
            }
            catch (FileSystemItemException)
            {
                throw;
            }
            catch (SevenZipHandlerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SevenZipHandlerException(ex.Message) { StackTrace = ex.StackTrace };
            }
        }
        public bool CreateHash(string folderToHash, out string hashValue, string outputFile = null)
        {
            return CreateHash(new string[] { folderToHash }, out hashValue, outputFile);
        }
        public bool CreateHash(string[] foldersToHash, out string hashValue, string outputFile = null)
        {
            hashValue = string.Empty;
            string output = string.Empty;
            try
            {
                if (foldersToHash == null || foldersToHash.Length == 0)
                    throw new SevenZipHandlerException("No folder(s) to hash");

                StringBuilder foldersToHashStr = new StringBuilder();
                foldersToHash.ToList().ForEach(f =>
                {
                    if (!Directory.Exists(f)) throw new DirectoryNotFoundException("Folder to hash " + f + " not found");
                    foldersToHashStr.AppendFormat(@"""" + f + @"\*.*"" ");
                });

                Logger.Log("Generating hash from {0} folder(s) contents", foldersToHashStr.ToString(), LogLevel.Debug);
                var args = string.Format(@"h -scrcsha256 -r {0}", foldersToHashStr);
                var commandOutput = string.Empty; 
                SevenZipReturnCode returnCode = run(args, out commandOutput);
                if (SevenZipReturnCode.NoError != returnCode) throw new SevenZipHandlerException("Hash generation error: " + SevenZipReturnCodeExt.ToString(returnCode));
                Regex regularExpression = new Regex("SHA256 for data and names:[ ]+([A-Z0-9]{64,64}).*", RegexOptions.IgnoreCase);                
                foreach (string line in commandOutput.Split(Environment.NewLine.ToArray()))
                {
                    var match = regularExpression.Match(line);
                    if (match.Success && match.Groups.Count > 0)
                    {
                        hashValue = match.Groups[0].Value;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(outputFile)) Filesystem.CreateTextFile(outputFile, hashValue);

                Logger.Log("Hash generated", LogLevel.Debug);

                return true;
            }
            catch (SevenZipHandlerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SevenZipHandlerException(ex.Message) { StackTrace = ex.StackTrace };
            }
        }
        public bool Compress(FileSystemInfo itemToCompress, string fileToGenerate, string itemToSkip = null, SecureString minga = null, string outputFile = null)
        {
            return Compress(new FileSystemInfo[] { itemToCompress }, fileToGenerate, new string[] { itemToSkip }, minga, outputFile);
        }
        public bool Compress(FileSystemInfo itemToCompress, string fileToGenerate, string[] itemsToSkip = null, SecureString minga = null, string outputFile = null)
        {
            return Compress(new FileSystemInfo[] { itemToCompress }, fileToGenerate, itemsToSkip, minga, outputFile);
        }
        public bool Compress(FileSystemInfo[] itemsToCompress, string fileToGenerate, string itemToSkip = null, SecureString minga = null, string outputFile = null)
        {
            return Compress(itemsToCompress, fileToGenerate, new string[] { itemToSkip }, minga, outputFile);
        }
        public bool Compress(FileSystemInfo[] itemsToCompress, string fileToGenerate, string[] itemsToSkip = null, SecureString minga = null, string outputFile = null)
        {
            string output = string.Empty;
            try
            {
                if (itemsToCompress == null) throw new FileSystemItemNullException();
                if (itemsToCompress.Length == 0) throw new FileSystemItemNoItem();

                string itemsToCompressStr = string.Empty;
                itemsToCompress.ToList().ForEach(i =>
                {
                    if (!i.Exists) throw new FileSystemItemNotFoundException(i);
                    itemsToCompressStr += @"""" + i.FullName + @""" ";
                });

                string itemsToSkipStr = itemsToSkip == null || itemsToSkip.Length == 0 ? string.Empty : "-x!";
                if (!string.IsNullOrEmpty(itemsToSkipStr))
                {
                    itemsToSkip.ToList().ForEach(i =>
                    {
                        itemsToSkipStr += @"""" + i + @""" ";
                    });
                }

                if (File.Exists(fileToGenerate))
                {
                    Logger.Log("Deleting {0}", fileToGenerate, LogLevel.Debug);
                    File.SetAttributes(fileToGenerate, FileAttributes.Normal);
                    File.Delete(fileToGenerate);
                }

                Logger.Log("Compressing {0} into {1}", itemsToCompressStr, fileToGenerate, LogLevel.Debug);
                var args = string.Format(@"a -mhe{0} ""{1}"" {2} {3}", (minga == null ? string.Empty : string.Format(" -p{0}", minga.SecureStringToString())), fileToGenerate, itemsToCompressStr, itemsToSkipStr);
                SevenZipReturnCode returnCode = run(args, out output);
                if (!string.IsNullOrEmpty(outputFile)) Filesystem.CreateTextFile(outputFile, output);
                if (SevenZipReturnCode.NoError != returnCode && SevenZipReturnCode.Warning != returnCode)
                    throw new SevenZipHandlerException("Compression error: " + SevenZipReturnCodeExt.ToString(returnCode));

                if (!File.Exists(fileToGenerate)) throw new FileNotFoundException("File to generate " + fileToGenerate + " not created");

                Logger.Log("File {0} created", fileToGenerate, LogLevel.Debug);

                return true;
            }
            catch (FileSystemItemException)
            {
                throw;
            }
            catch (SevenZipHandlerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SevenZipHandlerException(ex.Message) { StackTrace = ex.StackTrace };
            }
        }
        public bool CheckCompressedFileHealth(string fileToCheck, SecureString minga = null, string outputFile = null)
        {
            string output = string.Empty;
            try
            {
                if (!File.Exists(fileToCheck)) throw new FileNotFoundException("File to check" + fileToCheck + " not found");

                Logger.Log("Checking {0} file health", fileToCheck, LogLevel.Debug);
                var args = string.Format(@"t{0} ""{1}""", (minga == null ? string.Empty : string.Format(" -p{0}", minga.SecureStringToString())), fileToCheck);
                run(args, out output);
                if (!string.IsNullOrEmpty(outputFile))
                {
                    Filesystem.CreateTextFile(outputFile, output);
                }

                return !string.IsNullOrEmpty(output) && output.ToLower().IndexOf("everything is ok") >= 0;
            }
            catch (SevenZipHandlerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SevenZipHandlerException(ex.Message) { StackTrace = ex.StackTrace };
            }
        }

        public bool Decompress(string fileToDecompress, string targetFolder, SecureString minga = null, string outputFile = null)
        {
            string output = string.Empty;
            try
            {
                if (!File.Exists(fileToDecompress)) throw new FileNotFoundException("File to check" + fileToDecompress + " not found");
                if (!Directory.Exists(targetFolder)) throw new DirectoryNotFoundException("Folder " + targetFolder + " not found");
                
                Logger.Log("Decompressing {0} file under {1}", fileToDecompress, targetFolder, LogLevel.Debug);
                var args = string.Format(@"x {0} ""{1}"" -o""{2}"" -y -r", 
                    (minga == null ? string.Empty : string.Format(" -p{0}", minga.SecureStringToString())),
                    fileToDecompress, targetFolder);
                SevenZipReturnCode returnCode = run(args, out output);
                if (!string.IsNullOrEmpty(outputFile)) Filesystem.CreateTextFile(outputFile, output);
                if (SevenZipReturnCode.NoError != returnCode && SevenZipReturnCode.Warning != returnCode)
                    throw new SevenZipHandlerException("Decompression error: " + SevenZipReturnCodeExt.ToString(returnCode));
                
                Logger.Log("File decompressed", LogLevel.Debug);

                return true;
            }
            catch (FileSystemItemException)
            {
                throw;
            }
            catch (SevenZipHandlerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SevenZipHandlerException(ex.Message) { StackTrace = ex.StackTrace };
            }
        }
    }
}
