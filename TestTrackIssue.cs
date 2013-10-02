using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.Seapine
{
    /// <summary>
    /// Represents a TestTrack Pro defect.
    /// </summary>
    [Serializable]
    internal sealed class TestTrackIssue : IssueTrackerIssue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestTrackIssue"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="status">The status.</param>
        /// <param name="title">The title.</param>
        /// <param name="description">The description.</param>
        /// <param name="release">The release.</param>
        public TestTrackIssue(string id, string status, string title, string description, string release, bool isClosed)
            : base(id, status, title, description, release)
        {
            this.IsClosed = isClosed;
        }

        public bool IsClosed { get; private set; }
    }
}
