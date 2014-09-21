using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Visitors;

namespace BlackBerry.Package.ViewModels.TargetNavigator
{
    /// <summary>
    /// Wrapper class to visualize downloading progress.
    /// </summary>
    public sealed class DownloadUploadProgress : INotifyPropertyChanged
    {
        private readonly TargetNavigatorViewModel _viewModel;
        private readonly IFileServiceVisitorMonitor _monitor;

        private string _text;
        private int _percent;

        internal DownloadUploadProgress(TargetNavigatorViewModel viewModel, IFileServiceVisitorMonitor monitor)
        {
            if (viewModel == null)
                throw new ArgumentNullException("viewModel");
            if (monitor == null)
                throw new ArgumentNullException("monitor");

            _viewModel = viewModel;
            _monitor = monitor;

            _monitor.Started += OnStarted;
            _monitor.Completed += OnCompleted;
            _monitor.ProgressChanged += OnProgress;
        }

        #region Properties

        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    NotifyPropertyChanged("Text");
                }
            }
        }

        public int Percent
        {
            get { return _percent; }
            set
            {
                if (_percent != value)
                {
                    _percent = value;
                    NotifyPropertyChanged("Percent");
                }
            }
        }

        #endregion

        #region Handlers

        private void OnStarted(object sender, VisitorEventArgs e)
        {
            Text = "Initializing...";
            Percent = 0;
        }

        private void OnProgress(object sender, VisitorProgressChangedEventArgs e)
        {
            Text = e.Name;
            Percent = e.Progress;
        }

        private void OnCompleted(object sender, VisitorEventArgs e)
        {
            _monitor.Started -= OnStarted;
            _monitor.Completed -= OnCompleted;
            _monitor.ProgressChanged -= OnProgress;
            _viewModel.DownloadUploadFinished(this);
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        public void Cancel()
        {
            _monitor.IsCancelled = true;
        }
    }
}
