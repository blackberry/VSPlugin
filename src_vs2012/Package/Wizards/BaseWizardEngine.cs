using EnvDTE;

namespace BlackBerry.Package.Wizards
{
    /// <summary>
    /// Base class for any wizard engines responsible for collecting all information from developer to correctly create new project or project item.
    /// </summary>
    public abstract class BaseWizardEngine : IDTWizard
    {
        public void Execute(object application, int hwndOwner, ref object[] contextParams, ref object[] customParams, ref wizardResult returnValue)
        {
            
        }
    }
}
