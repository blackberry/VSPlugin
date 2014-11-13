using System;
using System.IO;
using System.Windows.Forms;
using BlackBerry.Package.Components;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.Model.Integration;
using BlackBerry.Package.ViewModels;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.VCProjectEngine;

namespace BlackBerry.Package.Dialogs
{
    /// <summary>
    /// 
    /// </summary>
    internal partial class ImportProjectForm : Form
    {
        private readonly ImportProjectViewModel _vm;

        public ImportProjectForm()
        {
            InitializeComponent();
            _vm = new ImportProjectViewModel();
        }

        #region Properties

        public string SourceProjectPath
        {
            get { return txtSourceProject.Text; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    SetSourceProject(value);
                }
            }
        }

        public string SourcePath
        {
            get;
            private set;
        }

        public bool CopyFiles
        {
            get { return chkCopyFiles.Checked; }
            set { chkCopyFiles.Checked = value; }
        }

        public bool IsNativeCoreAppSelected
        {
            get { return cmbProjects.SelectedIndex == 0; }
            set { cmbProjects.SelectedIndex = 0; }
        }

        public bool IsCascadesAppSelected
        {
            get { return cmbProjects.SelectedIndex == 1; }
            set { cmbProjects.SelectedIndex = 1; }
        }

        public bool IsSaveAtSourceLocation
        {
            get { return chkAtSourceLocation.Checked; }
            set { chkAtSourceLocation.Checked = value; }
        }

        #endregion

        public void SetSourceProject(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            txtSourceProject.Text = path;

            SourcePath = Path.GetDirectoryName(path);
            listFiles.Items.Clear();

            // traverse the source-tree to find all files, that could be added into the final solution:
            var files = _vm.ScanForFiles(SourcePath);

            if (files != null)
            {
                foreach (var file in files)
                {
                    if (file.EndsWith(".qml"))
                    {
                        IsCascadesAppSelected = true;
                    }

                    var item = new ListViewItem(string.IsNullOrEmpty(SourcePath) ? file : file.Substring(SourcePath.Length + 1));
                    item.Checked = true;
                    item.Tag = file;
                    listFiles.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// Start a file-open dialog for the Momentics project file.
        /// </summary>
        public bool UpdateSourceProject()
        {
            var form = DialogHelper.OpenNativeCoreProject("Open Momentics Project", null);

            if (form.ShowDialog() == DialogResult.OK)
            {
                SetSourceProject(form.FileName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Initializes the UI with 'new projects' section and projects from the current solution.
        /// </summary>
        /// <param name="solution"></param>
        public void AddTargetProjects(Solution solution)
        {
            cmbProjects.Items.Clear();
            cmbProjects.Items.Add(new ComboBoxItem("New Native Core project", "NativeProject"));
            cmbProjects.Items.Add(new ComboBoxItem("New Cascades project", "CascadesProject"));

            if (solution != null)
            {
                foreach (Project project in solution.Projects)
                {
                    if (BuildPlatformsManager.IsBlackBerryProject(project))
                    {
                        cmbProjects.Items.Add(new ComboBoxItem(project.Name, project));
                    }
                }
            }

            cmbProjects.SelectedIndex = 0;
        }

        private void bttFindProject_Click(object sender, EventArgs e)
        {
            UpdateSourceProject();
        }

        private void cmbProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = cmbProjects.SelectedItem;
            object tag = ((ComboBoxItem) item).Tag;
            var itemAsProject = tag as Project;

            txtDestinationName.ReadOnly = item == null || itemAsProject != null;
            txtDestinationName.Text = itemAsProject != null ? itemAsProject.Name : tag.ToString();
            chkAtSourceLocation.Enabled = !txtDestinationName.ReadOnly;
        }

        /// <summary>
        /// Creates new project or returns existing one (selected by developer), where all new sources should be placed.
        /// </summary>
        public Project PrepareProject(DTE2 dte)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");

            var item = cmbProjects.SelectedItem;
            object tag = ((ComboBoxItem) item).Tag;
            var itemAsProject = tag as Project;

            if (itemAsProject != null)
            {
                _vm.UpdateProject(itemAsProject);
                return itemAsProject;
            }

            // create new NativeCore or Cascades project:
            var projectName = tag.ToString();

            return _vm.CreateProject(dte, projectName, IsNativeCoreAppSelected, IsSaveAtSourceLocation ? SourcePath : null);
        }

        /// <summary>
        /// Creates new project or takes existing (depending on developer selection) and puts there all the sources.
        /// </summary>
        public Project FillProject(DTE2 dte)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");

            var project = PrepareProject(dte);
            if (project == null)
                return null;

            // copy files and add them into project:
            VCProject vcProject = project.Object as VCProject;
            if (vcProject == null)
                return null;

            bool copyFile = IsNativeCoreAppSelected || IsCascadesAppSelected ? CopyFiles && !IsSaveAtSourceLocation : CopyFiles;
            string projectDir = Path.GetDirectoryName(project.FullName);

            // add only checked items to the project:
            foreach (ListViewItem item in listFiles.CheckedItems)
            {
                _vm.AddFileToProject(projectDir, item.Tag.ToString(), item.Text, copyFile, true);
            }

            project.Save();
            return project;
        }

        private void txtDestinationName_TextChanged(object sender, EventArgs e)
        {
            var item = cmbProjects.SelectedItem as ComboBoxItem;
            if (item != null)
            {
                item.Tag = txtDestinationName.Text;
            }
        }
    }
}
