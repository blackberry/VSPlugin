using System;
using System.Collections.Generic;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;

namespace BlackBerry.Package.Components
{
    /// <summary>
    /// Class to simplify adding files into the project. It places them into correct filter, that maps local directory structure.
    /// All missing items on the relative path are automatically created.
    /// </summary>
    sealed class ProjectFolderTree
    {
        public const string FilterNameAssets = "assets";
        public const string FilterNameSourceFiles = "src";
        public const string FilterNameConfig = "config";
        public const string FilterNameTranslations = "trans";
        public const string DefaultFilters = FilterNameAssets + ";" + FilterNameConfig + ";" + FilterNameSourceFiles + ";" + FilterNameTranslations;

        private readonly VCProject _vcProject;
        private readonly string _projectFolder;
        private VCFilter _sourceFilter;
        private VCFilter _assetFilter;
        private VCFilter _translationFilter;
        private Dictionary<string, VCFilter> _filters;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ProjectFolderTree(Project project, bool createDefaultFolders)
        {
            if (project == null)
                throw new ArgumentNullException("project");

            _vcProject = project.Object as VCProject;
            if (_vcProject == null)
                throw new ArgumentOutOfRangeException("project", "This is not Visual C++ project");

            _projectFolder = Path.GetDirectoryName(_vcProject.ProjectFile) + Path.DirectorySeparatorChar;
            Refresh(createDefaultFolders);
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ProjectFolderTree(VCProject project, bool createDefaultFolders)
        {
            if (project == null)
                throw new ArgumentNullException("project");

            _vcProject = project;
            _projectFolder = Path.GetDirectoryName(_vcProject.ProjectFile) + Path.DirectorySeparatorChar;
            Refresh(createDefaultFolders);
        }

        /// <summary>
        /// Refreshes info about existing top-level filters.
        /// </summary>
        public void Refresh(bool createNonExisting)
        {
            IVCCollection filters = _vcProject.Filters;

            _filters = new Dictionary<string, VCFilter>();
            _sourceFilter = filters.Item("Source Files");
            if (_sourceFilter == null && createNonExisting)
            {
                _sourceFilter = CreateFilters(FilterNameSourceFiles, _vcProject)[0];
            }

            _assetFilter = filters.Item("Assets");
            if (_assetFilter == null && createNonExisting)
            {
                _assetFilter = CreateFilters(FilterNameAssets, _vcProject)[0];
            }

            _translationFilter = filters.Item("Translations");
            if (_translationFilter == null && createNonExisting)
            {
                _translationFilter = CreateFilters(FilterNameTranslations, _vcProject)[0];
            }

            // default directories already point to specific filters to shorten the path:
            if (_sourceFilter != null)
            {
                _filters.Add("src", _sourceFilter);
                _filters.Add("sources", _sourceFilter);
                _filters.Add("code", _sourceFilter);
            }

            if (_assetFilter != null)
            {
                _filters.Add("asset", _assetFilter);
                _filters.Add("assets", _assetFilter);
            }

            if (_translationFilter != null)
            {
                _filters.Add("translations", _translationFilter);
            }
        }

        /// <summary>
        /// Adds new file into the project. It will try to evaluate the relativePath based on the project's location.
        /// </summary>
        public void AddFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            AddFile(path, path.StartsWith(_projectFolder, StringComparison.OrdinalIgnoreCase) ? path.Substring(_projectFolder.Length) : null);
        }

        /// <summary>
        /// Adds new file into the project.
        /// </summary>
        public void AddFile(string fullPath, string relativePath)
        {
            if (string.IsNullOrEmpty(fullPath))
                throw new ArgumentNullException("fullPath");

            var filter = GetFileFilter(relativePath);
            if (filter != null)
            {
                filter.AddFile(fullPath);
            }
            else
            {
                _vcProject.AddFile(fullPath);
            }
        }

        private VCFilter GetFileFilter(string relativePath)
        {
            var dir = Path.GetDirectoryName(relativePath);
            return GetFolderFilter(dir);
        }

        private VCFilter GetFolderFilter(string relativePath)
        {
            // should be added directly into the project?
            if (string.IsNullOrEmpty(relativePath))
                return null;

            VCFilter filter;
            if (_filters.TryGetValue(relativePath, out filter))
                return filter;

            var parent = Path.GetDirectoryName(relativePath);
            var parentFilter = GetFolderFilter(parent);
            if (parentFilter == null)
                return null;

            var name = Path.GetFileName(relativePath);
            IVCCollection filters = parentFilter.Filters;
            VCFilter currentFilter = filters.Item(name);

            if (currentFilter == null)
            {
                currentFilter = parentFilter.AddFilter(name);
            }

            _filters[relativePath] = currentFilter;
            return currentFilter;
        }

        /// <summary>
        /// Creates basic set of filters inside the project.
        /// </summary>
        public VCFilter[] CreateFilters(string filtersDefinition)
        {
            var result = CreateFilters(filtersDefinition, _vcProject);

            if (result != null)
            {
                Refresh(false);
            }

            return result;
        }

        /// <summary>
        /// Creates basic set of filters inside the project.
        /// </summary>
        public static VCFilter[] CreateFilters(string filtersDefinition, Project project)
        {
            if (project == null)
                throw new ArgumentNullException("project");

            var vcProject = project.Object as VCProject;
            if (vcProject != null)
            {
                return CreateFilters(filtersDefinition, vcProject);
            }

            return null;
        }

        /// <summary>
        /// Creates basic set of filters inside the project.
        /// </summary>
        public static VCFilter[] CreateFilters(string filtersDefinition, VCProject vcProject)
        {
            if (string.IsNullOrEmpty(filtersDefinition))
                return null;

            var result = new List<VCFilter>();

            foreach (var filterName in filtersDefinition.Split(';', ',', ' '))
            {
                if (string.Compare(filterName, "sources", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    VCFilter filter = FindOrAddFilter("Source Files", vcProject);
                    filter.Filter = "cpp;c;cc;cxx;asm";
                    result.Add(filter);
                }

                if (string.Compare(filterName, "headers", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    VCFilter filter = FindOrAddFilter("Header Files", vcProject);
                    filter.Filter = "h;hpp;hxx;hm;def;inl;inc";
                    result.Add(filter);
                }

                if (string.Compare(filterName, FilterNameSourceFiles, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    VCFilter filter = FindOrAddFilter("Source Files", vcProject);
                    filter.Filter = "cpp;c;cc;cxx;asm;h;hpp;hxx;hm;def;inl;inc";
                    result.Add(filter);
                }

                if (string.Compare(filterName, FilterNameAssets, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    VCFilter filter = FindOrAddFilter("Assets", vcProject);
                    filter.Filter = "qml;js;qmljs;jpg;png;gif;bmp;ico;amd;wav;mp3;mp4";
                    result.Add(filter);
                }

                if (string.Compare(filterName, FilterNameConfig, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    VCFilter filter = FindOrAddFilter("Config Files", vcProject);
                    filter.Filter = "pri;pro;mk;properties;project;cproject;xml;xsd;bat;cmd;ps;ps1";
                    result.Add(filter);
                }

                if (string.Compare(filterName, FilterNameTranslations, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    VCFilter filter = FindOrAddFilter("Translations", vcProject);
                    filter.Filter = "ts;qm";
                    result.Add(filter);
                }
            }

            return result.ToArray();
        }

        private static VCFilter FindOrAddFilter(string name, VCProject vcProject)
        {
            IVCCollection filters = vcProject.Filters;
            VCFilter filter = filters.Item(name);

            return filter ?? vcProject.AddFilter(name);
        }
    }
}
