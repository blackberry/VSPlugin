namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Enumeration describing, what action can be performed over specified API Level.
    /// </summary>
    public enum ApiLevelTask
    {
        /// <summary>
        /// Nothing can be done with this API Level (it was installed separately and just detected).
        /// </summary>
        Nothing = 0,
        /// <summary>
        /// New API Level can be installed.
        /// </summary>
        Install,
        /// <summary>
        /// Installation of the API Level must be done manually (specified URL should be opened in a browser).
        /// </summary>
        InstallManually,
        /// <summary>
        /// Installation of the API Level was already done, but not detected automatically.
        /// </summary>
        AddExisting,
        /// <summary>
        /// Installed API Level, owned by the plugin, can be removed.
        /// </summary>
        Uninstall,
        /// <summary>
        /// Link added by the developer to custom API Level (installed NDK), can be just removed.
        /// </summary>
        Forget,
        /// <summary>
        /// No action is allowed to be performed.
        /// </summary>
        Hide,
        /// <summary>
        /// Data is out-dated and needs to be reloaded.
        /// </summary>
        Refresh
    }
}
