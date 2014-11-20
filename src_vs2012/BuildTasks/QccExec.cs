using BlackBerry.BuildTasks.Helpers;
using Microsoft.Build.Tasks;

namespace BlackBerry.BuildTasks
{
    /// <summary>
    /// Simple extension of Exec task that also presets some environment variables (copied ideas from bbndk-env_xxx.bat script from any NDK).
    /// </summary>
    public sealed class QccExec : Exec
    {
        public override bool Execute()
        {
            EnvironmentVariables = ProcessSetupHelper.Update(EnvironmentVariables, QnxHost, QnxTarget);
            return base.Execute();
        }

        #region Properties

        public string QnxHost
        {
            get;
            set;
        }

        public string QnxTarget
        {
            get;
            set;
        }

        #endregion
    }
}
