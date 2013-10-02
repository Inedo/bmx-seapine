using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.Seapine
{
    /// <summary>
    /// Represents a TestTrack project.
    /// </summary>
    [Serializable]
    internal sealed class TestTrackProject : CategoryBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestTrackProject"/> class.
        /// </summary>
        /// <param name="name">The project name.</param>
        public TestTrackProject(string name)
            : base(name, name, null)
        {
        }
    }
}
