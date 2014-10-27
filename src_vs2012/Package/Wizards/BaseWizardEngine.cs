using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.Package.Model.Wizards;
using EnvDTE;
using EnvDTE80;

namespace BlackBerry.Package.Wizards
{
    /// <summary>
    /// Base class for any wizard engines responsible for collecting all information from developer to correctly create new project or project item.
    /// </summary>
    [ComVisible(true)]
    public abstract class BaseWizardEngine : IDTWizard
    {
        /// <summary>
        /// Entry point of the wizard engine.
        /// </summary>
        void IDTWizard.Execute(object application, int hwndOwner, ref object[] contextParams, ref object[] customParams, ref wizardResult returnValue)
        {
            var dte = application as DTE2;
            if (dte == null)
            {
                TraceLog.WarnLine("Invalid instance of DTE received ({0})", GetType().Name);
                returnValue = wizardResult.wizardResultFailure;
                return;
            }

            // parameters passed to the wizard are described here:
            // http://msdn.microsoft.com/en-us/library/bb164728.aspx && http://msdn.microsoft.com/en-us/library/tz690efs.aspx

            var wizardType = contextParams[0] != null ? contextParams[0].ToString() : null;

            // add new project:
            if (String.Compare(wizardType, NewProjectParams.TypeGuid, StringComparison.OrdinalIgnoreCase) == 0)
            {
                returnValue = ExecuteNewProject(dte, new IntPtr(hwndOwner), new NewProjectParams(contextParams), ParseCustomParams(customParams));
                return;
            }

            // add new project item:
            if (String.Compare(wizardType, NewProjectItemParams.TypeGuid, StringComparison.OrdinalIgnoreCase) == 0)
            {
                returnValue = ExecuteNewProjectItem(dte, new IntPtr(hwndOwner), new NewProjectItemParams(contextParams), ParseCustomParams(customParams));
                return;
            }

            TraceLog.WarnLine("Unknown wizard type to launch ({0}: {1})", GetType().Name, wizardType);
            returnValue = wizardResult.wizardResultFailure;
        }

        private KeyValuePair<string, string>[] ParseCustomParams(object[] customParams)
        {
            var result = new List<KeyValuePair<string,string>>();

            if (customParams != null)
            {
                foreach (var cp in customParams)
                {
                    if (cp != null)
                    {
                        var textParam = cp.ToString();

                        if (!String.IsNullOrEmpty(textParam))
                        {
                            int separatorIndex = textParam.IndexOf('=');

                            if (separatorIndex < 0)
                            {
                                result.Add(new KeyValuePair<string, string>(textParam.Trim(), String.Empty));
                            }
                            else
                            {
                                result.Add(new KeyValuePair<string, string>(textParam.Substring(0, separatorIndex).Trim(), textParam.Substring(separatorIndex + 1).Trim()));
                            }
                        }
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Gets the C/C++ code safe name from specified 'name' (understood as file name).
        /// </summary>
        protected static string CreateSafeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "New1";

            var result = new StringBuilder();

            // remove all non-standard letters from name:
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsLetterOrDigit(c))
                {
                    result.Append(c);
                }
                else
                {
                    if (c == '_' || c == ' ')
                    {
                        result.Append('_');
                    }
                }

            }

            return result.ToString();
        }

        /// <summary>
        /// Get the 'source' part of the item specified path.
        /// Format of the path:
        ///  * &lt;source-template&gt;
        ///  * &lt;source-template&gt; -&gt; &lt;new-name&gt;
        ///  * &lt;source-template&gt; #&gt; &lt;new-extension&gt;
        ///  * &lt;source-template&gt; ~&gt; &lt;new-folder&gt;
        /// </summary>
        protected string GetSourceName(string path, out bool canAddToProject)
        {
            if (string.IsNullOrEmpty(path))
            {
                canAddToProject = true;
                return null;
            }

            if (path[0] == '!')
            {
                canAddToProject = false;
                path = path.Substring(1).TrimStart();
            }
            else
            {
                canAddToProject = true;
            } 

            var index = path.IndexOf("->", 0, StringComparison.Ordinal);
            if (index < 0)
            {
                index = path.IndexOf("#>", 0, StringComparison.Ordinal);
            }
            if (index < 0)
            {
                index = path.IndexOf("~>", 0, StringComparison.Ordinal);
            }

            return index >= 0 ? Unwrap(path.Substring(0, index).Trim()) : Unwrap(path);
        }

        private static string GetDestinationNameRequest(string marker, string path, TokenProcessor tokenProcessor)
        {
            if (string.IsNullOrEmpty(marker))
                throw new ArgumentNullException("marker");

            var index = path.IndexOf(marker, 0, StringComparison.Ordinal);
            if (index >= 0)
            {
                var request = Unwrap(path.Substring(index + marker.Length).Trim());

                if (!string.IsNullOrEmpty(request))
                {
                    if (tokenProcessor != null)
                    {
                        request = tokenProcessor.Untoken(request);
                    }

                    return request.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Replace("" + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar, "" + Path.DirectorySeparatorChar);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the 'destination' part of the item specified path and applies the transformation.
        /// </summary>
        protected string GetDestinationName(string itemName, string path, TokenProcessor tokenProcessor)
        {
            if (string.IsNullOrEmpty(itemName))
                throw new ArgumentNullException("itemName");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            var destinationRequest = GetDestinationNameRequest("->", path, tokenProcessor);
            if (!string.IsNullOrEmpty(destinationRequest))
            {
                // replace item's name:
                if (IsFullPath(destinationRequest))
                    return destinationRequest;
                return Path.Combine(Path.GetDirectoryName(itemName), destinationRequest);
            }

            destinationRequest = GetDestinationNameRequest("#>", path, tokenProcessor);
            if (!string.IsNullOrEmpty(destinationRequest))
            {
                // change item's extension:
                return Path.ChangeExtension(itemName, destinationRequest);
            }

            destinationRequest = GetDestinationNameRequest("~>", path, tokenProcessor);
            if (!string.IsNullOrEmpty(destinationRequest))
            {
                // change item's folder (to full or relative path):
                if (IsFullPath(destinationRequest))
                    return Path.Combine(destinationRequest, Path.GetFileName(itemName));
                return Path.Combine(Path.GetDirectoryName(itemName), destinationRequest, Path.GetFileName(itemName));
            }

            return itemName;
        }

        private static bool IsFullPath(string path)
        {
            return !string.IsNullOrEmpty(path) && ((path.Length > 1 && path[1] == ':') || path.StartsWith("\\\\"));
        }

        private static string Unwrap(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if ((path[0] == '"' && path[path.Length - 1] == '"')
                || (path[0] == '\'' && path[path.Length - 1] == '\''))
                return path.Substring(1, path.Length - 2);

            return path;
        }

        #region Abstract Methods

        /// <summary>
        /// Method that creates new project for existing or new solution.
        /// </summary>
        internal abstract wizardResult ExecuteNewProject(DTE2 dte, IntPtr owner, NewProjectParams context, KeyValuePair<string, string>[] customParams);

        /// <summary>
        /// Method that creates new project item for existing project.
        /// </summary>
        internal abstract wizardResult ExecuteNewProjectItem(DTE2 dte, IntPtr owner, NewProjectItemParams context, KeyValuePair<string, string>[] customParams);

        #endregion
    }
}
