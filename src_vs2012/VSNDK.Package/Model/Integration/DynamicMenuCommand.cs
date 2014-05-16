using System;
using System.Collections;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace RIM.VSNDK_Package.Model.Integration
{
    /// <summary>
    /// Encapsulates displaying a collection of items as multiple menu items within Visual Studio.
    /// </summary>
    internal sealed class DynamicMenuCommand : OleMenuCommand
    {
        private readonly Predicate<int> _displayPredicate;
        private readonly Action<DynamicMenuCommand, ICollection, int> _invokeHandler;
        private readonly Action<DynamicMenuCommand, ICollection, int> _processedQueryStatus;
        private readonly Func<ICollection> _getCollection; 
        private readonly ICollection _collection;

        public DynamicMenuCommand(Predicate<int> displayPredicate, EventHandler invokeHandler, EventHandler beforeQueryStatusHandler, CommandID id)
            : base(invokeHandler, null, beforeQueryStatusHandler, id)
        {
            if (displayPredicate == null)
                throw new ArgumentNullException("displayPredicate");

            _displayPredicate = displayPredicate;
        }

        public DynamicMenuCommand(ICollection collection, EventHandler invokeHandler, Action<DynamicMenuCommand, ICollection, int> beforeQueryStatus, CommandID id)
            : base(invokeHandler, null, InternalBeforeCollectionQueryStatusUpdate, id)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            if (beforeQueryStatus == null)
                throw new ArgumentNullException("beforeQueryStatus");

            _displayPredicate = InternalCollectionPredicate;
            _processedQueryStatus = beforeQueryStatus;
            _collection = collection;
        }

        public DynamicMenuCommand(Func<ICollection> collectionHandler, Action<DynamicMenuCommand, ICollection, int> invokeHandler, Action<DynamicMenuCommand, ICollection, int> beforeQueryStatus, CommandID id)
            : base(InternalInvokeHandler, null, InternalBeforeCollectionQueryStatusUpdate, id)
        {
            if (collectionHandler == null)
                throw new ArgumentNullException("collectionHandler");
            if (beforeQueryStatus == null)
                throw new ArgumentNullException("beforeQueryStatus");

            _displayPredicate = InternalCollectionPredicate;
            _invokeHandler = invokeHandler;
            _processedQueryStatus = beforeQueryStatus;
            _getCollection = collectionHandler;
        }

        private ICollection Collection
        {
            get
            {
                ICollection result = null;

                if (_getCollection != null)
                    result = _getCollection();

                if (result == null)
                    result = _collection;

                return result;
            }
        }

        public override bool DynamicItemMatch(int cmdId)
        {
            if (_displayPredicate(cmdId))
            {
                MatchedCommandId = cmdId;
                return true;
            }

            MatchedCommandId = 0;
            return false;
        }

        private bool InternalCollectionPredicate(int cmdId)
        {
            var collection = Collection;

            if (collection == null)
                return false;

            return cmdId >= CommandID.ID && (cmdId - CommandID.ID) < collection.Count;
        }

        private static void InternalBeforeCollectionQueryStatusUpdate(object sender, EventArgs e)
        {
            var command = (DynamicMenuCommand)sender;

            int index = command.MatchedCommandId == 0 ? 0 : command.MatchedCommandId - command.CommandID.ID;

            // and ask for further status update:
            command._processedQueryStatus(command, command.Collection, index);
        }

        private static void InternalInvokeHandler(object sender, EventArgs e)
        {
            var command = (DynamicMenuCommand) sender;

            int index = command.MatchedCommandId == 0 ? 0 : command.MatchedCommandId - command.CommandID.ID;

            // and notify about selection made:
            if (command._invokeHandler != null)
            {
                command._invokeHandler(command, command.Collection, index);
            }
        }
    }
}
