namespace IqraCore.Entities.App.Lifecycle
{
    public enum AppLifecycleStatus
    {
        /// <summary>
        /// Database is empty or AppInstalled flag is false.
        /// Redirect to Installer Wizard.
        /// </summary>
        NotInstalled = 0,

        /// <summary>
        /// Database Version != Code Version.
        /// Redirect to Migration/Update page.
        /// </summary>
        VersionMismatch = 1,

        /// <summary>
        /// Healthy state.
        /// </summary>
        Running = 2
    }
}