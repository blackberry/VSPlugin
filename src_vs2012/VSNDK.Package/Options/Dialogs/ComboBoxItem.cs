namespace RIM.VSNDK_Package.Options.Dialogs
{
    /// <summary>
    /// Item class to feed Windows Forms ComboBox.
    /// </summary>
    public sealed class ComboBoxItem
    {
        public ComboBoxItem()
        {
        }

        public ComboBoxItem(string text)
        {
            Text = text;
        }

        public ComboBoxItem(string text, object tag)
        {
            Text = text;
            Tag = tag;
        }

        #region Properties

        public string Text
        {
            get;
            set;
        }

        public object Tag
        {
            get;
            set;
        }

        #endregion

        public override string ToString()
        {
            return Text ?? string.Empty;
        }
    }
}
