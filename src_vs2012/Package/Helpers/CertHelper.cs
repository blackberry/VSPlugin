using System;
using System.IO;
using System.Windows.Forms;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Model;
using BlackBerry.Package.Options.Dialogs;

namespace BlackBerry.Package.Helpers
{
    internal static class CertHelper
    {
        /// <summary>
        /// Helper method to let reload author. Returns 'true', when new data has been loaded and UI needs to be updated.
        /// </summary>
        public static bool ReloadAuthor(DeveloperDefinition developer)
        {
            if (developer == null)
                throw new ArgumentNullException("developer");

            string author;

            // verify if at least tried to register:
            if (!developer.HasCertificate)
            {
                MessageBoxHelper.Show("Sorry, but certificate does not exist. Please try to register first", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // try to reload info with cached password:
            if (developer.HasPassword)
            {
                author = developer.UpdateName(null);

                if (!string.IsNullOrEmpty(author))
                    return true;
            }

            // if it failed, ask for new password:
            var form = new PasswordForm();
            if (form.ShowDialog() != DialogResult.OK)
                return false;

            developer.UpdatePassword(form.Password, form.ShouldRemember);

            // try again to reload data from certificate:
            author = developer.UpdateName(null);
            VerifyAuthor(author);

            return !string.IsNullOrEmpty(author);
        }

        /// <summary>
        /// Helper class to import certificate into the DataPath location.
        /// It also triggers asking for password. Returns 'true', when all was done correctly and UI needs to be updated.
        /// </summary>
        public static bool Import(DeveloperDefinition developer)
        {
            if (developer == null)
                throw new ArgumentNullException("developer");
            if (string.IsNullOrEmpty(developer.DataPath))
                throw new ArgumentOutOfRangeException("developer");

            // navigate for new certificate:
            var form = DialogHelper.OpenCertFile(developer.DataPath);

            if (form.ShowDialog() == DialogResult.OK && File.Exists(form.FileName))
            {
                // will need to move the file? - ask for confirmation, if one with the same name exists:
                var srcPath = form.FileName;
                var fileName = Path.GetFileName(srcPath);
                var folderName = Path.GetDirectoryName(srcPath);

                if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(folderName))
                    return false;

                // copy the file:
                if (string.Compare(developer.DataPath, folderName, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    var destPath = Path.Combine(developer.DataPath, fileName);
                    if (File.Exists(destPath))
                    {
                        var result = MessageBoxHelper.Show("File \"" + fileName + "\" already exists in certificate storage folder.\r\nDo you want to overwrite it?",
                                                           null, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

                        if (result == DialogResult.Cancel)
                            return false;
                        if (result == DialogResult.No)
                        {
                            // generate new name:
                            fileName = "author-" + DateTime.Now.ToString("yyyy-MM-dd") + ".p12";
                            destPath = Path.Combine(developer.DataPath, fileName);
                        }
                    }

                    try
                    {
                        File.Copy(srcPath, destPath, true);
                    }
                    catch (Exception ex)
                    {
                        TraceLog.WriteException(ex, "Unable to copy certificate file \"{0}\"", srcPath);
                        MessageBoxHelper.Show(ex.Message, "Certificate file error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }

                developer.UpdateCertificate(fileName);

                // ask for password several times:
                while (true)
                {
                    var passForm = new PasswordForm();

                    if (passForm.ShowDialog() == DialogResult.OK)
                    {
                        // load info from new certificate:
                        developer.UpdateName(passForm.Password);

                        // succeeded - yes?
                        if (developer.HasName)
                        {
                            developer.UpdatePassword(passForm.Password, passForm.ShouldRemember);
                            return true;
                        }

                        // no - display error and ask again:
                        VerifyAuthor(developer.Name);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return false;
        }

        private static void VerifyAuthor(string author)
        {
            if (string.IsNullOrEmpty(author))
            {
                MessageBoxHelper.Show("Unable to load info about author", "Invalid password or certificate file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
