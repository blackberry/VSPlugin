using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RIM.VSNDK_Package
{
    static class GlobalFunctions
    {
        /// <summary>
        /// function to check if user is online.  
        /// Needs to be online for UpdateManager to work.
        /// </summary>
        /// <returns></returns>
        public static bool isOnline()
        {
            try
            {
                System.Net.IPHostEntry ipHostEntry = System.Net.Dns.GetHostEntry("downloads.blackberry.com");
                return true;
            }
            catch (System.Net.Sockets.SocketException)
            {
                return false;
            }
        }
    }
}
