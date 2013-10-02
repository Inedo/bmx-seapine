using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Seapine
{
    /// <summary>
    /// Seapine TestTrack Pro issue tracker provider.
    /// </summary>
    [ProviderProperties(
      "TestTrack Pro",
      "Supports Seapine TestTrack Pro/RM 2013.")]
    [CustomEditor(typeof(TestTrackProviderEditor))]
    public sealed class TestTrackProvider : IssueTrackingProviderBase, ICategoryFilterable
    {
        private TestTrack.ttsoapcgi soapcgi;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestTrackProvider"/> class.
        /// </summary>
        public TestTrackProvider()
        {
        }

        [Persistent]
        public string BaseUrl { get; set; }
        [Persistent]
        public string UserName { get; set; }
        [Persistent]
        public string Password { get; set; }
        [Persistent]
        public string ReleaseFilter { get; set; }

        public string[] CategoryIdFilter { get; set; }
        public string[] CategoryTypeNames
        {
            get { return new[] { "Project" }; }
        }

        /// <summary>
        /// Gets the TestTrack SOAP proxy.
        /// </summary>
        private TestTrack.ttsoapcgi Soap
        {
            get
            {
                if (this.soapcgi == null)
                    this.soapcgi = new TestTrack.ttsoapcgi { Url = this.BaseUrl };

                return this.soapcgi;
            }
        }

        public override IssueTrackerIssue[] GetIssues(string releaseNumber)
        {
            var releaseFilterRegex = new Regex(Util.CoalesceStr(this.ReleaseFilter, "%RELNO%").Replace("%RELNO%", Regex.Escape(releaseNumber)), RegexOptions.IgnoreCase);
            var cookie = OpenProject();
            try
            {
                var releaseFolderNames = new List<string>();
                var issues = new List<TestTrackIssue>();

                // Folders are typically used to plan releases in TestTrack
                var folders = this.GetTableData(cookie, "Folder", "Record ID", "Path", "Name");
                foreach (var folder in folders)
                {
                    var path = folder[1];
                    if (releaseFilterRegex.IsMatch(path))
                        releaseFolderNames.Add(folder[2]);
                }

                // Return if there are no matching folders
                if (releaseFolderNames.Count == 0)
                    return new IssueTrackerIssue[0];

                var tables = this.Soap.getTableList(cookie).Select(t => t.name).ToList();
                
                // If defects are available, get the columns
                if (tables.Contains("Defect", StringComparer.OrdinalIgnoreCase))
                {
                    var defects = this
                        .GetTableData(cookie, "Defect", "Folders", "Number", "Summary", "Description", "Status", "Closed Date")
                        .Where(d => releaseFolderNames.Contains(d[0], StringComparer.OrdinalIgnoreCase))
                        .Select(d => new TestTrackIssue("BUG-" + d[1], d[4], d[2], d[3], releaseNumber, !string.IsNullOrEmpty(d[5])));

                    issues.AddRange(defects);
                }

                // If requirements are available, get the columns
                if (tables.Contains("Requirement", StringComparer.OrdinalIgnoreCase))
                {
                    var requirements = this
                        .GetTableData(cookie, "Requirement", "Folders", "Number", "Summary", "Description", "Status", "Tag", "Closed Date")
                        .Where(d => releaseFolderNames.Contains(d[0], StringComparer.OrdinalIgnoreCase))
                        .Select(d => new TestTrackIssue(Util.CoalesceStr(d[5], "REQ-" + d[1]), d[4], d[2], d[3], releaseNumber, !string.IsNullOrEmpty(d[6])));

                    issues.AddRange(requirements);
                }

                return issues.ToArray();
            }
            finally
            {
                this.Soap.DatabaseLogoff(cookie);
            }
        }
        public override bool IsIssueClosed(IssueTrackerIssue issue)
        {
            return ((TestTrackIssue)issue).IsClosed;
        }
        public override bool IsAvailable()
        {
            return true;
        }
        public override void ValidateConnection()
        {
            try
            {
                GetCategories();
            }
            catch (Exception ex)
            {
                throw new NotAvailableException(ex.Message, ex);
            }
        }
        public override string ToString()
        {
            return "Connects to the TestTrack Pro or TestTrack RM system.";
        }
        public IssueTrackerCategory[] GetCategories()
        {
            var projects = this.Soap.getProjectList(this.UserName, this.Password);

            var list = new List<TestTrackProject>();
            foreach (var project in projects)
                list.Add(new TestTrackProject(project.database.name));

            return list.ToArray();
        }

        private long OpenProject()
        {
            var project = new TestTrack.CProject
            {
                database = new TestTrack.CDatabase
                {
                    name = this.CategoryIdFilter[0]
                },
                options = new[]
                {
                    new TestTrack.CProjectDataOption { name = "TestTrack Pro" },
                    new TestTrack.CProjectDataOption { name = "TestTrack RM" }
                }
            };

            return this.Soap.ProjectLogon(project, this.UserName, this.Password);
        }
        private string[][] GetTableData(long cookie, string tableName, params string[] columns)
        {
            try
            {
                var results = this.Soap.getRecordListForTable(
                    cookie,
                    tableName,
                    string.Empty,
                    columns.Select(c => new TestTrack.CTableColumn { name = c }).ToArray()
                );

                if (results == null)
                    return new string[0][];

                return results
                    .records
                    .Select(r => r.row.Select(c => c.value).ToArray())
                    .ToArray();
            }
            catch
            {
                return new string[0][];
            }
        }
    }
}
