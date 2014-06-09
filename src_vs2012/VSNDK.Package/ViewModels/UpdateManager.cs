using System;
using System.Collections.Generic;
using System.IO;
using RIM.VSNDK_Package.Diagnostics;

namespace RIM.VSNDK_Package.ViewModels
{
    internal sealed class UpdateManager
    {
        #region Internal Classes

        /// <summary>
        /// Description of actions scheduled to execution by UpdateManager.
        /// </summary>
        public sealed class ActionData
        {
            #region Properties

            internal ActionData(UpdateManager manager, UpdateActions action, UpdateActionTargets target, string name, Version version)
            {
                if (manager == null)
                    throw new ArgumentNullException("manager");
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentNullException("name");
                if (version == null)
                    throw new ArgumentNullException("version");

                UpdateManager = manager;
                Action = action;
                Target = target;
                Name = name;
                Version = version;
            }

            private UpdateManager UpdateManager
            {
                get;
                set;
            }

            public bool CanAbort
            {
                get { return true; }
            }

            public UpdateActions Action
            {
                get;
                private set;
            }

            public UpdateActionTargets Target
            {
                get;
                private set;
            }

            public string Name
            {
                get;
                private set;
            }

            public Version Version
            {
                get;
                private set;
            }

            #endregion

            public override string ToString()
            {
                return string.Concat(Action, "-Action of ", Target, "-", Version);
            }

            internal void Abort()
            {

            }
        }

        #endregion

        private readonly PackageViewModel _vm;
        private readonly List<ActionData> _actions;

        public UpdateManager(PackageViewModel vm, string folder)
        {
            if (vm == null)
                throw new ArgumentNullException("vm");
            if (string.IsNullOrEmpty(folder))
                throw new ArgumentNullException("folder");

            _vm = vm;
            Folder = folder;
            SyncFilePath = Path.Combine(Folder, "vsplugin.pid");
            _actions = new List<ActionData>();
            Actions = new ActionData[0];
        }

        #region Properties

        /// <summary>
        /// Gets the currently running action.
        /// </summary>
        public ActionData CurrentAction
        {
            get;
            private set;
        }

        /// <summary>
        /// Thread-safe read-only collection of performed actions.
        /// </summary>
        public ActionData[] Actions
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the playground folder for sync between different instances of Visual Studio.
        /// </summary>
        private string Folder
        {
            get;
            set;
        }

        private string SyncFilePath
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// Requests installation or removal over specified item and version.
        /// </summary>
        public void Request(UpdateActions action, UpdateActionTargets target, string name, Version version)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (version == null)
                throw new ArgumentNullException("version");

            Schedule(new ActionData(this, action, target, name, version));
        }

        private void Schedule(ActionData action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            lock (GetType())
            {
                TraceLog.WriteLine("Scheduled: {0}", action);

                // add action to execution:
                _actions.Add(action);
                Actions = _actions.ToArray();
            }

            // and try to start it:
            Start();
        }

        private void Remove(ActionData action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            lock (GetType())
            {
                TraceLog.WarnLine("Removed: {0}", action);

                // remove from execution:
                if (action.CanAbort)
                {
                    action.Abort();
                    if (_actions.Remove(action))
                    {
                        Actions = _actions.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Checks, if there exists scheduled action for specified item.
        /// </summary>
        public bool IsProcessing(UpdateActionTargets target, Version version)
        {
            if (version == null)
                return false;

            foreach (var action in Actions)
            {
                if (action.Target == target && action.Version == version)
                    return true;
            }

            return false;
        }

        private void Start()
        {

            
        }
    }
}
