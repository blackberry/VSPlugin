using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackBerry.Package.ViewModels.TargetNavigator;

namespace BlackBerry.Package.ViewModels.Common
{
    /// <summary>
    /// Support class that allows switching data-templates at runtime based on content's type.
    /// </summary>
    public sealed class ContentTemplateSelector : DataTemplateSelector
    {
        #region Properties

        public DataTemplate ImageTemplate
        {
            get;
            set;
        }

        public DataTemplate StringTemplate
        {
            get;
            set;
        }

        public DataTemplate Int32Template
        {
            get;
            set;
        }

        public DataTemplate Int32ArrayTemplate
        {
            get;
            set;
        }

        public DataTemplate BinaryTemplate
        {
            get;
            set;
        }

        public DataTemplate ViewItemArrayTemplate
        {
            get;
            set;
        }

        public DataTemplate FileViewTemplate
        {
            get;
            set;
        }

        public DataTemplate FileViewArrayTemplate
        {
            get;
            set;
        }

        public DataTemplate ProcessViewTemplate
        {
            get;
            set;
        }

        public DataTemplate ProcessViewArrayTemplate
        {
            get;
            set;
        }

        public DataTemplate DefaultTemplate
        {
            get;
            set;
        }

        #endregion

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Image || item is ImageSource)
                return ImageTemplate ?? DefaultTemplate;
            if (item is string || item is MessageViewItem)
                return StringTemplate ?? DefaultTemplate;
            if (item is int)
                return Int32Template ?? DefaultTemplate;
            if (item is IEnumerable<int>)
                return Int32ArrayTemplate ?? DefaultTemplate;
            if (item is IEnumerable<byte>)
                return BinaryTemplate ?? DefaultTemplate;
            if (item is IEnumerable<FileViewItem>)
                return FileViewArrayTemplate ?? DefaultTemplate;
            if (item is FileViewItem)
                return FileViewTemplate;
            if (item is ProcessViewItem)
                return ProcessViewTemplate ?? DefaultTemplate;
            if (item is IEnumerable<ProcessViewItem>)
                return ProcessViewArrayTemplate ?? DefaultTemplate;
            if (item is IEnumerable<BaseViewItem>)
                return ViewItemArrayTemplate ?? DefaultTemplate;

            return DefaultTemplate;
        }
    }
}
