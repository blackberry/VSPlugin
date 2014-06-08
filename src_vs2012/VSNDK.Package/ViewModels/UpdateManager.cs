using System;
using System.Collections.Generic;
using RIM.VSNDK_Package.Diagnostics;

namespace RIM.VSNDK_Package.ViewModels
{
    internal sealed class UpdateManager
    {
        #region Internal Classes

        class ActionData
        {
            #region Properties

            public ActionData(UpdateActions action, UpdateActionTargets target, Version version)
            {
                if (version == null)
                    throw new ArgumentNullException("version");

                Action = action;
                Target = target;
                Version = version;
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
        }

        #endregion

        private PackageViewModel _vm;
        private List<ActionData> _actions;

        public UpdateManager(PackageViewModel vm)
        {
            if (vm == null)
                throw new ArgumentNullException("vm");

            _vm = vm;
            _actions = new List<ActionData>();
        }

        public void Request(UpdateActions action, UpdateActionTargets target, Version version)
        {
            if (version == null)
                throw new ArgumentNullException("version");

            Schedule(new ActionData(action, target, version));
        }

        private void Schedule(ActionData action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            TraceLog.WriteLine("Scheduled: {0}", action);

            // PH: TODO:
            _actions.Add(action);
        }

        public bool IsProcessing(Version version, UpdateActionTargets target)
        {
            if (version == null)
                return false;

            foreach (var action in _actions)
            {
                if (action.Target == target && action.Version == version)
                    return true;
            }

            return false;
        }
    }
}
