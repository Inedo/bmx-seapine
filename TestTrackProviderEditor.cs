using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Seapine
{
    /// <summary>
    /// Custom editor for the Test Track issue tracker provider.
    /// </summary>
    internal sealed class TestTrackProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtUserName;
        private ValidatingTextBox txtBaseUrl;
        private PasswordTextBox txtPassword;
        private ValidatingTextBox txtFolderFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestTrackProviderEditor"/> class.
        /// </summary>
        public TestTrackProviderEditor()
        {
        }

        public override void BindToForm(ProviderBase extension)
        {
            this.EnsureChildControls();

            var provider = (TestTrackProvider)extension;
            this.txtUserName.Text = provider.UserName;
            this.txtPassword.Text = provider.Password;
            this.txtBaseUrl.Text = provider.BaseUrl;
            this.txtFolderFilter.Text = Util.CoalesceStr(provider.ReleaseFilter, "%RELNO%");
        }
        public override ProviderBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new TestTrackProvider
            {
                UserName = this.txtUserName.Text,
                Password = this.txtPassword.Text,
                BaseUrl = this.txtBaseUrl.Text,
                ReleaseFilter = this.txtFolderFilter.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtUserName = new ValidatingTextBox { Width = 300, Required = true };
            this.txtBaseUrl = new ValidatingTextBox { Width = 300, Required = true };
            this.txtPassword = new PasswordTextBox { Width = 270 };
            this.txtFolderFilter = new ValidatingTextBox { Width = 300, Required = true, Text = "%RELNO%" };

            this.Controls.Add(
                new FormFieldGroup("Test Track Server URL",
                    "The URL of the Test Track SOAP service, for example: http://testtrack/scripts/ttsoapcgi.exe",
                    false,
                    new StandardFormField("Server URL:", this.txtBaseUrl)
                ),
                new FormFieldGroup("Authentication",
                    "Provide a username and password to connect to the Test Track server.",
                    false,
                    new StandardFormField("User Name:", this.txtUserName),
                    new StandardFormField("Password:", this.txtPassword)
                ),
                new FormFieldGroup("Release Mapping",
                    "A <a href=\"http://msdn.microsoft.com/en-us/library/az24scfc.aspx\">.NET-style regular expression</a> used to match any folder which contains each issue with a BuildMaster release. Use <i>%RELNO%</i> for the BuildMaster release number. For example, " +
                    "<i>%RELNO%</i> will match folder with the release number in its name.",
                    false,
                    new StandardFormField("Regular Expression:", this.txtFolderFilter)
                )
            );
        }
    }
}
