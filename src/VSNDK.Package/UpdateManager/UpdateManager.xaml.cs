using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RIM.VSNDK_Package.UpdateManager.Model;
using Microsoft.VisualStudio.PlatformUI;

namespace RIM.VSNDK_Package.UpdateManager
{
    /// <summary>
    /// Interaction logic for UpdateManager.xaml
    /// </summary>
    public partial class UpdateManager : Window
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public UpdateManager()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Close button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Install button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Install_Click(object sender, RoutedEventArgs e)
        {
            UpdateManagerData data = gridMain.DataContext as UpdateManagerData;
            if (data != null)
            {
                data.InstallAPI(((APITargetClass)APITargets.SelectedItem).TargetVersion);
            }
        }

        /// <summary>
        /// Uninstall button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            UpdateManagerData data = gridMain.DataContext as UpdateManagerData;
            if (data != null)
            {
                data.UninstallAPI(((APITargetClass)lbAvailable.SelectedItem).TargetVersion);
            }
        }

        /// <summary>
        /// Update 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UpdateManagerData data = gridMain.DataContext as UpdateManagerData;
            if (data != null)
            {
                data.UpdateAPI(((APITargetClass)lbAvailable.SelectedItem).TargetVersion, ((APITargetClass)lbAvailable.SelectedItem).LatestVersion);
            }
        }
    }
}
