using System;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Security.Cryptography;

namespace BlackBerry.NativeCore.Helpers
{
    /// <summary>
    /// Class providing common helper functions.
    /// </summary>
    public static class GlobalHelper
    {
        /// <summary>
        /// Checks if user is online. Needs to be online for UpdateManager to work.
        /// </summary>
        public static bool IsOnline
        {
            get
            {
                try
                {
                    Dns.GetHostEntry("downloads.blackberry.com");
                    return true;
                }
                catch (SocketException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Encrypts a given password and returns the encrypted data as a base64 string.
        /// </summary>
        /// <param name="text">An unencrypted string that needs to be secured.</param>
        /// <returns>A base64 encoded string that represents the encrypted binary data.
        /// </returns>
        /// <remarks>This solution is not really secure as we are keeping strings in memory. If runtime protection is essential, <see cref="SecureString"/> should be used.</remarks>
        /// <exception cref="ArgumentNullException">If <paramref name="text"/> is a null reference.</exception>
        public static string Encrypt(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            var data = Encoding.Unicode.GetBytes(text);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine);

            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// Decrypts a given string.
        /// </summary>
        /// <param name="cipher">A base64 encoded string that was created through the <see cref="Encrypt(string)"/> extension method.</param>
        /// <returns>The decrypted string.</returns>
        /// <remarks>Keep in mind that the decrypted string remains in memory and makes your application vulnerable per se. If runtime protection is essential, <see cref="SecureString"/> should be used.</remarks>
        /// <exception cref="ArgumentNullException">If <paramref name="cipher"/> is a null reference.</exception>
        public static string Decrypt(string cipher)
        {
            if (cipher == null)
                throw new ArgumentNullException("cipher");

            var data = Convert.FromBase64String(cipher);
            var decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);

            return Encoding.Unicode.GetString(decrypted);
        }
    }
}
