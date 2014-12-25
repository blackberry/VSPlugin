using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Forms;
using BlackBerry.NativeCore.Model;
using BlackBerry.Package.Components;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.Model.Integration;
using BlackBerry.Package.ViewModels;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.VCProjectEngine;

namespace BlackBerry.Package.Dialogs
{
    internal partial class ImportProjectForm : Form
    {
        private const string SeparatorChar = ";";
        private readonly char[] Separators = { ' ', '\t', ';' };

        private readonly ImportProjectViewModel _vm;
        private Solution _solution;
        private string _newProjectName;

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
            set
            {
                if (cmbProjects.Items.Count > 1)
                {
                    cmbProjects.SelectedIndex = 1;
                }
            }
        }

        public bool IsSaveAtSourceLocation
        {
            get { return chkAtSourceLocation.Checked; }
            set { chkAtSourceLocation.Checked = value; }
        }

        public bool BuildOutputDependsOnTargetArch
        {
            get;
            private set;
        }

        #endregion

        public void SetSourceProject(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            txtSourceProject.Text = path;
            SourcePath = Path.GetDirectoryName(path);

            // load info about the project:
            var info = ImportProjectInfo.Load(SourcePath);

            _newProjectName = info != null ? info.Name : "NewProject";
            txtDefines.Text = info != null ? AsString(SeparatorChar, info.Defines) : null;
            txtDependencies.Text = info != null ? AsString(SeparatorChar, info.Dependencies) : null;
            BuildOutputDependsOnTargetArch = info != null && info.BuildOutputDependsOnTargetArch;
            listFiles.Items.Clear();

            bool isCascadesProject = false;

            if (info != null && info.Files.Length > 0)
            {
                foreach (var file in info.Files)
                {
                    if (file.EndsWith(".qml"))
                    {
                        isCascadesProject = true;
                    }

                    var item = new ListViewItem(string.IsNullOrEmpty(SourcePath) || !file.StartsWith(SourcePath) ? file : file.Substring(SourcePath.Length + 1));
                    item.Checked = true;
                    item.Tag = file;
                    listFiles.Items.Add(item);
                }
            }

            cmbProjects.SelectedIndex = isCascadesProject ? 1 : 0;
        }

        private static string AsString(string separator, IEnumerable<string> list)
        {
            return string.Join(separator, list);
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
        public void AddTargetProjects(Solution solution)
        {
            cmbProjects.Items.Clear();
            cmbProjects.Items.Add(new ComboBoxItem("New Native Core project", null));
            cmbProjects.Items.Add(new ComboBoxItem("New Cascades project", null));

            _solution = solution;
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
        }

        private void bttFindProject_Click(object sender, EventArgs e)
        {
            UpdateSourceProject();
        }

        private void cmbProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = cmbProjects.SelectedItem;
            object tag = item != null ? ((ComboBoxItem) item).Tag : null;
            var itemAsProject = tag as Project;

            txtDestinationName.ReadOnly = item == null || itemAsProject != null;
            txtDestinationName.Text = itemAsProject != null ? itemAsProject.Name : _newProjectName;
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
                _vm.UpdateProject(itemAsProject, txtDefines.Text.Split(Separators, StringSplitOptions.RemoveEmptyEntries), txtDependencies.Text.Split(Separators, StringSplitOptions.RemoveEmptyEntries), false);
                return itemAsProject;
            }

            // create new NativeCore or Cascades project:
            return _vm.CreateProject(dte, _newProjectName, IsNativeCoreAppSelected, IsSaveAtSourceLocation ? SourcePath : null,
                txtDefines.Text.Split(Separators, StringSplitOptions.RemoveEmptyEntries),
                txtDependencies.Text.Split(Separators, StringSplitOptions.RemoveEmptyEntries),
                BuildOutputDependsOnTargetArch);
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
                _newProjectName = txtDestinationName.Text;
            }
        }

        private void bttOK_Click(object sender, EventArgs e)
        {
            // verify inputs:
            if (string.IsNullOrEmpty(_newProjectName))
            {
                MessageBoxHelper.Show("Sorry, name of the new project can't be empty", "Validation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ActiveControl = txtDestinationName;
                return;
            }

            if (SolutionHasProject(_solution, _newProjectName))
            {
                MessageBoxHelper.Show("Project with name \"" + _newProjectName + "\" already exists inside the solution", "Validation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ActiveControl = txtDestinationName;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Checks, if project with specified name is already inside the solution.
        /// </summary>
        private static bool SolutionHasProject(Solution solution, string projectName)
        {
            if (solution == null)
                return false;
            if (string.IsNullOrEmpty(projectName))
                return false;

            foreach (Project project in solution.Projects)
            {
                if (string.Compare(projectName, project.Name, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }

            return false;
        }

        private void contextMenuFiles_Opening(object sender, CancelEventArgs e)
        {
            if (listFiles.SelectedItems.Count == 0)
            {
                e.Cancel = true;
                return;
            }

            e.Cancel = false;
            var folders = GetFolderNames(listFiles.SelectedItems, SourcePath);

            contextCheckMenuItem.DropDownItems.Clear();
            contextUncheckMenuItem.DropDownItems.Clear();
            contextRemoveMenuItem.DropDownItems.Clear();

            contextToggleMenuItem.Text = listFiles.SelectedItems.Count > 1 ? "Toggle items" : "Toggle item";

            if (folders != null)
            {
                bool allChecked = AreAllChecked(listFiles.SelectedItems);
                bool allUnchecked = AreAllUnchecked(listFiles.SelectedItems);

                if (!allChecked)
                {
                    foreach (var folder in folders)
                    {
                        var item = new ToolStripMenuItem(folder + "/*");
                        item.Click += CheckForFolderButton;
                        item.Tag = folder;

                        contextCheckMenuItem.DropDownItems.Add(item);
                    }
                }

                if (!allUnchecked)
                {
                    foreach (var folder in folders)
                    {
                        var item = new ToolStripMenuItem(folder + "/*");
                        item.Click += UncheckForFolderButton;
                        item.Tag = folder;

                        contextUncheckMenuItem.DropDownItems.Add(item);
                    }
                }

                foreach (var folder in folders)
                {
                    var item = new ToolStripMenuItem(folder + "/*");
                    item.Click += RemoveForFolderButton;
                    item.Tag = folder;

                    contextRemoveMenuItem.DropDownItems.Add(item);
                }

                checkAllToolStripMenuItem.Visible = false;
                uncheckAllToolStripMenuItem.Visible = false;
            }
            else
            {
                checkAllToolStripMenuItem.Visible = true;
                uncheckAllToolStripMenuItem.Visible = true;
            }

            contextCheckMenuItem.Visible = contextCheckMenuItem.DropDownItems.Count > 0;
            contextUncheckMenuItem.Visible = contextUncheckMenuItem.DropDownItems.Count > 0;
            contextRemoveMenuItem.Visible = contextRemoveMenuItem.DropDownItems.Count > 0;
        }

        private bool AreAllUnchecked(ListView.SelectedListViewItemCollection items)
        {
            if (items == null)
                return false;

            foreach (ListViewItem item in items)
            {
                if (item.Checked)
                    return false;
            }

            return true;
        }

        private bool AreAllChecked(ListView.SelectedListViewItemCollection items)
        {
            if (items == null)
                return false;

            foreach (ListViewItem item in items)
            {
                if (!item.Checked)
                    return false;
            }

            return true;
        }

        private static IEnumerable<string> GetFolderNames(ListView.SelectedListViewItemCollection activeItems, string sourcePath)
        {
            if (activeItems == null || activeItems.Count == 0)
                return null;

            var result = new List<string>();
            var buffer = new StringBuilder();

            foreach (ListViewItem item in activeItems)
            {
                var fileName = item.Tag != null ? item.Tag.ToString() : null;
                if (string.IsNullOrEmpty(fileName))
                    continue;

                if (!string.IsNullOrEmpty(sourcePath) && fileName.StartsWith(sourcePath))
                {
                    fileName = fileName.Substring(sourcePath.Length);
                }
                else
                {
                    if (fileName.Length > 1 && fileName[1] == Path.VolumeSeparatorChar)
                    {
                        fileName = fileName.Substring(2);
                    }
                }

                var chunks = fileName.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < chunks.Length - 1; i++)
                {
                    buffer.Remove(0, buffer.Length);

                    for (int j = 0; j <= i; j++)
                    {
                        buffer.Append(chunks[j]);
                        if (j != i)
                        {
                            buffer.Append('/');
                        }
                    }

                    var pathPart = buffer.ToString();
                    if (!result.Contains(pathPart))
                    {
                        result.Add(pathPart);
                    }
                }
            }

            return result.Count == 0 ? null : result;
        }

        private static bool Matches(ListViewItem item, string path)
        {
            return item != null && !string.IsNullOrEmpty(path) && item.Text.StartsWith(path, StringComparison.Ordinal);
        }

        private void CheckForFolderButton(object sender, EventArgs eventArgs)
        {
            var contextMenu = (ToolStripMenuItem) sender;
            var partPath = contextMenu != null && contextMenu.Tag != null ? contextMenu.Tag.ToString().Replace('/', '\\') : null;

            if (partPath != null)
            {
                foreach (ListViewItem item in listFiles.Items)
                {
                    if (Matches(item, partPath))
                    {
                        item.Checked = true;
                    }
                }
            }
        }

        private void UncheckForFolderButton(object sender, EventArgs eventArgs)
        {
            var contextMenu = (ToolStripMenuItem)sender;
            var partPath = contextMenu != null && contextMenu.Tag != null ? contextMenu.Tag.ToString().Replace('/', '\\') : null;

            if (partPath != null)
            {
                foreach (ListViewItem item in listFiles.Items)
                {
                    if (Matches(item, partPath))
                    {
                        item.Checked = false;
                    }
                }
            }
        }

        private void RemoveForFolderButton(object sender, EventArgs eventArgs)
        {
            var contextMenu = (ToolStripMenuItem)sender;
            var partPath = contextMenu != null && contextMenu.Tag != null ? contextMenu.Tag.ToString().Replace('/', '\\') : null;

            if (partPath != null)
            {
                var toRemove = new List<ListViewItem>();

                foreach (ListViewItem item in listFiles.Items)
                {
                    if (Matches(item, partPath))
                    {
                        toRemove.Add(item);
                    }
                }

                foreach (var item in toRemove)
                {
                    listFiles.Items.Remove(item);
                }
            }
        }

        private void contextToggleMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listFiles.SelectedItems)
            {
                item.Checked = !item.Checked;
            }
        }

        private void checkAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listFiles.SelectedItems)
            {
                item.Checked = true;
            }
        }

        private void uncheckAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listFiles.SelectedItems)
            {
                item.Checked = false;
            }
        }
    }
}
