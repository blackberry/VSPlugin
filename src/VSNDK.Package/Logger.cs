using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RIM.VSNDK_Package
{
    class Logger
    {
        private System.IO.StreamWriter file;

        public Logger()
        {
            
        }

        public void LogToFile(string targetPath, string line)
        {
            file = new System.IO.StreamWriter(targetPath, true);
            file.WriteLine(line);
            file.Close();
        }
    }
}
