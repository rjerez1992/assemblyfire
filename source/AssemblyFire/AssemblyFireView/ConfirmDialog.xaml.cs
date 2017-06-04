using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AssemblyFireView
{
    /// <summary>
    /// Interaction logic for ConfirmDialog.xaml
    /// </summary>
    public partial class ConfirmDialog
    {
        public string FakeTitle;
        public string Message;
        public MessageBoxResult result = MessageBoxResult.None;

        public ConfirmDialog()
        {
            InitializeComponent();
            
        }

        private void CancelClick(object sender, RoutedEventArgs e) //Cancel
        {
            this.result = MessageBoxResult.Cancel;
            this.Close();
        }


        private void NoClick(object sender, RoutedEventArgs e) //No
        {
            this.result = MessageBoxResult.No;
            this.Close();
        }


        private void YesClick(object sender, RoutedEventArgs e) //Yes
        {
            this.result = MessageBoxResult.Yes;
            this.Close();
        }

        public void SetDialogTitle(string s) {
            this.Title = s;
            this.txtTitle.Text = s;
        }

        public void SetDialogMessage(string s)
        {
            this.txtMessage.Text = s;
        }
    }
}
