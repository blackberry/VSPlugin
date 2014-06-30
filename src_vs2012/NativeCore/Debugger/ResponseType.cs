namespace BlackBerry.NativeCore.Debugger
{
    public enum ResponseType
    {
        ResultRecord,
        ExecAsyncOutput,
        StatusAsyncOutput,
        NotificationAsyncOutput,
        /// <summary>
        /// Comments only record.
        /// </summary>
        StreamRecord
    }
}
