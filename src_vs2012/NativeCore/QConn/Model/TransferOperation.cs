namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Description of the transfer performed to or from the target.
    /// </summary>
    public enum TransferOperation
    {
        Unknown,
        Buffering,
        Downloading,
        Uploading,
        Zipping,
        Unzipping
    }
}
