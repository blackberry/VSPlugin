using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RIM.VSNDK_Package.Signing
{
    public partial class Browser : Form
    {
        SigningDialog signingDialog;

        public Browser(SigningDialog sd)
        {
            signingDialog = sd;
            InitializeComponent();
        }
    }
}
