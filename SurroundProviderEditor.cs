using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Seapine
{
    /// <summary>
    /// Custom editor for the Surround SCM provider.
    /// </summary>
    internal sealed class SurroundProviderEditor : ProviderEditorBase
    {
        private TextBox txtUserName;
        private PasswordTextBox txtPassword;
        private TextBox txtServer;
        private SourceControlFileFolderPicker txtSSCMExecutablePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="SurroundProviderEditor"/> class.
        /// </summary>
        public SurroundProviderEditor()
        {
        }

        protected override void CreateChildControls()
        {
            this.txtUserName = new TextBox() { ID = "txtUserName" };
            this.txtPassword = new PasswordTextBox() { ID = "txtPassword" };
            this.txtServer = new TextBox() { ID = "txtServer" };
            this.txtSSCMExecutablePath = new SourceControlFileFolderPicker() { ServerId = this.EditorContext.ServerId };

            CUtil.Add(this,
                new FormFieldGroup("Surround SCM Executable Path",
                    "The path to the SSCM executable on the server.",
                    false,
                    new StandardFormField("SSCM Executable:", this.txtSSCMExecutablePath)),
                new FormFieldGroup("Authentication",
                    "Provide information needed for connecting to a Surround SCM server. The server may optionally include a port - for example <i>surroundserver:4900</i>. If no port is specified, the default port (4900) is used.",
                    false,
                    new StandardFormField("User Name:", this.txtUserName),
                    new StandardFormField("Password:", this.txtPassword),
                    new StandardFormField("Surround SCM Server:", this.txtServer)
                )
            );
        }

        public override void BindToForm(ProviderBase extension)
        {
            EnsureChildControls();

            var ext = (SurroundProvider)extension;
            this.txtUserName.Text = ext.UserName ?? string.Empty;
            this.txtPassword.Text = ext.Password ?? string.Empty;
            this.txtServer.Text = ext.Server ?? string.Empty;
            this.txtSSCMExecutablePath.Text = ext.ExePath ?? string.Empty;
        }

        public override ProviderBase CreateFromForm()
        {
            EnsureChildControls();

            return new SurroundProvider()
            {
                UserName = this.txtUserName.Text,
                Password = this.txtPassword.Text,
                Server = this.txtServer.Text,
                ExePath = this.txtSSCMExecutablePath.Text
            };
        }
    }
}
