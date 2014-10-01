using System;
using System.Windows.Forms;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.Model.Integration;
using EnvDTE;

namespace BlackBerry.Package.Dialogs
{
    /// <summary>
    /// Dialog to let to select a value from a predefined non-editable set.
    /// By default used to display a set of projects and optionally 'custom...' item.
    /// </summary>
    internal partial class BinaryDiscoveryForm : Form
    {
        public BinaryDiscoveryForm(string title)
        {
            InitializeComponent();
            Text = title;
        }

        #region Property

        public string SelectedPath
        {
            get { return txtPath.Text; }
        }

        #endregion

        /// <summary>
        /// Adds a project, that will be a read-only item with predefined outputPath as result.
        /// </summary>
        public void AddTarget(Project project, string outputPath, bool isSelected)
        {
            if (project == null)
                throw new ArgumentNullException("project");

            var item = new ComboBoxItem(project.Name, outputPath ?? "");
            int index = cmbProjects.Items.Add(item);

            if (isSelected)
            {
                cmbProjects.SelectedIndex = index;
                RefreshUI();
            }
        }

        /// <summary>
        /// Adds a custom item, that will let the user specify any value.
        /// </summary>
        public void AddCustomTarget(string title)
        {
            if (string.IsNullOrEmpty(title))
                throw new ArgumentNullException("title");

            var item = new ComboBoxItem(title, null);
            int index = cmbProjects.Items.Add(item);

            if (cmbProjects.SelectedIndex < 0)
            {
                cmbProjects.SelectedIndex = index;
                RefreshUI();
            }
        }

        private void cmbProjects_SelectionChangeCommitted(object sender, EventArgs e)
        {
            RefreshUI();
        }

        private void bttOpen_Click(object sender, EventArgs e)
        {
            var form = DialogHelper.OpenAnyFile("Locate executable with matching debug symbols");

            if (form.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = form.FileName;
            }
        }

        private void RefreshUI()
        {
            var item = cmbProjects.SelectedItem as ComboBoxItem;

            if (item != null)
            {
                txtPath.Text = (string)item.Tag;
                txtPath.Enabled = item.Tag == null;
                bttOpen.Enabled = item.Tag == null;
            }
        }
    }
}
