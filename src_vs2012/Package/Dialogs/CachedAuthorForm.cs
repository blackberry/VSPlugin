using System.IO;
using System.Windows.Forms;
using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;
using BlackBerry.Package.Helpers;

namespace BlackBerry.Package.Dialogs
{
    /// <summary>
    /// Dialog that let developer to specify publisher data, placed automatically in bar-descriptor.xml, when new project is created.
    /// </summary>
    public sealed partial class CachedAuthorForm : Form
    {
        private DebugTokenInfoRunner _debugTokenInfoRunner;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public CachedAuthorForm(string title, AuthorInfo info)
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(title))
            {
                Text = title;
            }

            txtID.Text = info != null ? info.ID : null;
            txtName.Text = info != null ? info.Name : null;
        }

        /// <summary>
        /// Gets the author info input on UI.
        /// </summary>
        public AuthorInfo ToAuthor()
        {
            if (string.IsNullOrWhiteSpace(txtID.Text) && string.IsNullOrWhiteSpace(txtName.Text))
            {
                return null;
            }

            return new AuthorInfo(txtID.Text.Trim(), txtName.Text.Trim());
        }

        private void bttClear_Click(object sender, System.EventArgs e)
        {
            txtID.Text = null;
            txtName.Text = null;
        }

        private void bttLoad_Click(object sender, System.EventArgs e)
        {
            // is running...
            if (_debugTokenInfoRunner != null)
                return;

            var form = DialogHelper.OpenBarFile("Select debug token", ConfigDefaults.DataDirectory);
            if (form.ShowDialog() == DialogResult.OK)
            {
                if (!File.Exists(form.FileName))
                {
                    MessageBoxHelper.Show("Unable to locate the debug-token file", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    TraceLog.WarnLine("Unable to locate debug-token: \"{0}\"", form.FileName);
                    return;
                }

                _debugTokenInfoRunner = new DebugTokenInfoRunner(form.FileName);
                _debugTokenInfoRunner.Dispatcher = EventDispatcher.From(this);
                _debugTokenInfoRunner.Finished += DebugTokenInfoLoaded;
                _debugTokenInfoRunner.ExecuteAsync();
            }
        }

        private void DebugTokenInfoLoaded(object sender, ToolRunnerEventArgs e)
        {
            var debugToken = _debugTokenInfoRunner.DebugToken;
            _debugTokenInfoRunner = null;

            if (e.IsSuccessfull && debugToken != null && debugToken.Author != null)
            {
                txtName.Text = debugToken.Author.Name;
                txtID.Text = debugToken.Author.ID;
            }
            else
            {
                // notify about the error:
                MessageBoxHelper.Show("Unable to load author info from specified debug token", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                TraceLog.WarnLine("Unable to load info from debug-token");
            }
        }
    }
}
