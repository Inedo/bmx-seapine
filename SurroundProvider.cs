using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Seapine
{
    /// <summary>
    /// Provides functionality for getting files, browsing folders, and applying labels in Surround SCM.
    /// </summary>
    [ProviderProperties(
        "Surround SCM",
        "Supports Surround SCM 2010-2011; requires Surround SCM command line client to be installed.",
        RequiresTransparentProxy = true)]
    [CustomEditor(typeof(SurroundProviderEditor))]
    public sealed class SurroundProvider : SourceControlProviderBase, ILabelingProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SurroundProvider"/> class.
        /// </summary>
        public SurroundProvider()
        {
        }

        /// <summary>
        /// Gets or sets the server name and port.
        /// </summary>
        [Persistent]
        public string Server { get; set; }
        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        [Persistent]
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the user's password.
        /// </summary>
        [Persistent]
        public string Password { get; set; }
        /// <summary>
        /// Gets or sets the path to the SSCM.EXE file.
        /// </summary>
        [Persistent]
        public string ExePath { get; set; }
        /// <summary>
        /// When implemented in a derived class, gets the char that's used by the
        /// provider to separate directories/files in a path string
        /// </summary>
        public override char DirectorySeparator
        {
            get { return '/'; }
        }

        /// <summary>
        /// Gets the Surround Server name with a default port if necessary.
        /// </summary>
        private string ServerWithPort
        {
            get
            {
                if (string.IsNullOrEmpty(this.Server))
                    return string.Empty;

                if (!this.Server.Contains(":"))
                    return this.Server + ":4900";
                else
                    return this.Server;
            }
        }

        public override void GetLatest(string sourcePath, string targetPath)
        {
            if (string.IsNullOrEmpty(sourcePath))
                throw new ArgumentNullException("sourcePath");
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");

            var path = new SurroundPath(sourcePath);

            Directory.CreateDirectory(targetPath);
            SSCM_NoOutput("get", path.Path, "-d" + targetPath, "-r", "-p" + path.Repository, "-b" + path.Branch);
        }
        public override DirectoryEntryInfo GetDirectoryEntryInfo(string sourcePath)
        {
            sourcePath = (sourcePath ?? string.Empty).Trim('/');
            var scmPath = new SurroundPath(sourcePath);

            if (scmPath.IsTopLevel)
            {
                var mainlines = SSCM("lsmainline");
                var mainlineEntries = new List<DirectoryEntryInfo>();

                foreach (var mainline in mainlines)
                {
                    var branches = SSCM("lsbranch", "-p" + mainline);
                    foreach (var branch in branches)
                    {
                        if (branch.EndsWith("(mainline)", StringComparison.InvariantCultureIgnoreCase))
                        {
                            mainlineEntries.Add(new DirectoryEntryInfo(mainline, mainline, new DirectoryEntryInfo[0], new FileEntryInfo[0]));
                        }
                        else
                        {
                            var branchName = branch.Substring(0, branch.LastIndexOf(" ("));
                            var branchPath = GetBranchPath(branchName, mainline);

                            mainlineEntries.Add(new DirectoryEntryInfo(branchPath + ":" + branchName, branchPath + ":" + branchName, new DirectoryEntryInfo[0], new FileEntryInfo[0]));
                        }
                    }
                }

                return new DirectoryEntryInfo(string.Empty, string.Empty, mainlineEntries.ToArray(), new FileEntryInfo[0]);
            }
            else
                return GetDirectoryEntry(scmPath);
        }
        public override byte[] GetFileContents(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException("filePath");

            var path = filePath.Split(new[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var mainline = path[0];
            var subpath = (path.Length > 1 && !string.IsNullOrEmpty(path[1])) ? path[1] : "/";

            var tempPath = Path.Combine(Path.GetTempPath(), "bmsurround2");
            Directory.CreateDirectory(tempPath);

            SSCM_NoOutput("get", subpath, "-d" + tempPath, "-r", "-p" + mainline);

            int fileNameIndex = filePath.LastIndexOf('/');
            var fileName = fileNameIndex >= 0 ? filePath.Substring(fileNameIndex + 1) : filePath;
            return File.ReadAllBytes(Path.Combine(tempPath, fileName));
        }
        public override bool IsAvailable()
        {
            return true;
        }
        public override void ValidateConnection()
        {
            SSCM("version");
        }
        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "Provides functionality for getting files, browsing folders, and applying labels in Surround SCM.";
        }
        public void ApplyLabel(string label, string sourcePath)
        {
            if (string.IsNullOrEmpty(label))
                throw new ArgumentNullException("label");
            if (string.IsNullOrEmpty(sourcePath))
                throw new ArgumentNullException("sourcePath");

            sourcePath = sourcePath.Trim('/');
            if (sourcePath == string.Empty)
                throw new ArgumentException("Invalid source path. Must specify a repository.");

            var path = new SurroundPath(sourcePath);

            SSCM_NoOutput("label", path.Path, "-b" + path.Branch, "-l" + label, "-c-", "-p" + path.Repository, "-r");
        }
        public void GetLabeled(string label, string sourcePath, string targetPath)
        {
            if (string.IsNullOrEmpty(label))
                throw new ArgumentNullException("label");
            if (string.IsNullOrEmpty(sourcePath))
                throw new ArgumentNullException("sourcePath");
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");

            var path = new SurroundPath(sourcePath);

            Directory.CreateDirectory(targetPath);
            SSCM_NoOutput("get", path.Path + "*", "-d" + targetPath, "-r", "-p" + path.Repository, "-l" + label, "-b" + path.Branch);
        }

        private List<string> SSCM(string command, params string[] args)
        {
            return this.SSCMPath(true, null, command, args);
        }
        private void SSCM_NoOutput(string command, params string[] args)
        {
            this.SSCMPath(false, null, command, args);
        }
        private List<string> SSCMPath(bool captureOutput, string workingDirectory, string command, params string[] args)
        {
            var argBuffer = new StringBuilder(command);
            argBuffer.Append(' ');

            foreach (var arg in args)
                argBuffer.AppendFormat("\"{0}\" ", arg);

            if (!string.IsNullOrEmpty(this.Server))
                argBuffer.AppendFormat("\"-z{0}\" ", this.ServerWithPort);

            if (!string.IsNullOrEmpty(this.UserName))
            {
                argBuffer.AppendFormat("\"-y{0}", this.UserName);
                if (!string.IsNullOrEmpty(this.Password))
                    argBuffer.AppendFormat(":{0}", this.Password);

                argBuffer.Append("\" ");
            }

            var startInfo = new ProcessStartInfo(this.ExePath, argBuffer.ToString())
            {
                UseShellExecute = false,
                RedirectStandardOutput = captureOutput,
                RedirectStandardError = true
            };

            if (!string.IsNullOrEmpty(workingDirectory))
                startInfo.WorkingDirectory = workingDirectory;

            this.LogProcessExecution(startInfo);

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            var lines = captureOutput ? new List<string>() : null;
            var errorBuffer = new StringBuilder();

            if (captureOutput)
            {
                process.OutputDataReceived +=
                    (s, e) =>
                    {
                        lock (lines)
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                                lines.Add(e.Data);
                        }
                    };
            }

            process.ErrorDataReceived +=
                (s, e) =>
                {
                    lock (errorBuffer)
                    {
                        errorBuffer.Append(e.Data);
                    }
                };

            this.LogInformation("Running SSCM...");

            process.Start();

            if (captureOutput)
                process.BeginOutputReadLine();

            process.BeginErrorReadLine();

            process.WaitForExit();

            this.LogDebug("SSCM has exited.");

            if (process.ExitCode != 0)
            {
                var errorMessage = errorBuffer.ToString();
                throw new InvalidOperationException(errorMessage);
            }

            return lines;
        }
        private DirectoryEntryInfo GetDirectoryEntry(SurroundPath path)
        {
            var results = SSCM("ls", path.Path, "-b" + path.Branch, "-p" + path.Repository, "-r");
            if (results.Count > 1)
            {
                results.RemoveAt(results.Count - 1);
                int index = 0;
                return ParseDirectory(ref index, results, path);
            }

            return null;
        }
        private string GetBranchPath(string branch, string mainline)
        {
            var branchProperties = SSCM("bp", "-b" + branch, "-p" + mainline);
            foreach (var property in branchProperties)
            {
                var fields = property.Split(new[] { ':' }, 2, StringSplitOptions.None);
                if (string.Equals(fields[0], "Created from path", StringComparison.InvariantCultureIgnoreCase))
                    return fields[1].TrimStart();
            }

            throw new InvalidOperationException(string.Format("Could not determine root path of '{0}' branch.", branch));
        }
        private static DirectoryEntryInfo ParseDirectory(ref int index, List<string> lines, SurroundPath branchPath)
        {
            var path = lines[index++];
            var newPath = path;
            var subdirs = new List<DirectoryEntryInfo>();
            var files = new List<FileEntryInfo>();

            if (branchPath.IsBranch)
                newPath = branchPath.Repository + ":" + branchPath.Branch + "/" + path.Substring(branchPath.Repository.Length).TrimStart('/');

            newPath = newPath.Trim('/');

            while (index < lines.Count)
            {
                if (!char.IsWhiteSpace(lines[index][0])) // directory
                {
                    if (!lines[index].StartsWith(path + "/"))
                        break;

                    subdirs.Add(ParseDirectory(ref index, lines, branchPath));
                }
                else
                {
                    int trimIndex = lines[index].LastIndexOf(" current ");
                    if (trimIndex > 0)
                    {
                        var fileName = lines[index].Substring(0, trimIndex).Trim();
                        files.Add(new FileEntryInfo(fileName, newPath + "/" + fileName));
                    }

                    index++;
                }
            }

            int pathTrimIndex = newPath.LastIndexOf('/');
            var pathName = pathTrimIndex >= 0 ? newPath.Substring(pathTrimIndex + 1) : newPath;

            return new DirectoryEntryInfo(pathName, newPath, subdirs.ToArray(), files.ToArray());
        }

        private sealed class SurroundPath
        {
            private readonly string repository;
            private readonly string branch;
            private readonly string path;

            public SurroundPath(string path)
            {
                if (string.IsNullOrEmpty(path))
                    return;

                int colonIndex = path.IndexOf(':');
                if (colonIndex >= 0)
                {
                    this.repository = path.Substring(0, colonIndex);
                    int pathIndex = path.IndexOf('/', colonIndex);
                    if (pathIndex < 0)
                    {
                        this.branch = path.Substring(colonIndex + 1);
                    }
                    else
                    {
                        this.branch = path.Substring(colonIndex + 1, pathIndex - colonIndex - 1);
                        this.path = path.Substring(pathIndex + 1).Trim('/') + "/";
                    }
                }
                else
                {
                    int pathIndex = path.IndexOf('/');
                    if (pathIndex < 0)
                    {
                        this.repository = path;
                    }
                    else
                    {
                        this.repository = path.Substring(0, pathIndex);
                        this.path = path.Substring(pathIndex + 1).Trim('/') + "/";
                    }
                }
            }

            public string Repository
            {
                get { return this.repository; }
            }
            public string Branch
            {
                get { return Util.CoalesceStr(this.branch, this.repository); }
            }
            public string Path
            {
                get { return Util.CoalesceStr(this.path, "/"); }
            }
            public bool IsTopLevel
            {
                get { return string.IsNullOrEmpty(this.repository); }
            }
            public bool IsBranch
            {
                get { return !string.IsNullOrEmpty(this.branch); }
            }
        }
    }
}
