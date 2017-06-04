using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for BlockEditor.xaml
    /// </summary>
    public partial class BlockEditor
    {
        private int blockNum4;

        public BlockEditor()
        {
            InitializeComponent(); //Remember to prevent the paste action
            DataObject.AddPastingHandler(this.BlockValueBox, BlockValuePaste);
        }

        public void SetValues(MainWindow owner, int blockNum4, string blockValue) {
            this.Owner = owner;
            this.blockNum4 = blockNum4;
            this.txtTitle.Text = "Modificar bloque de memoria N°"+blockNum4;
            this.BlockValueBox.Text = blockValue;
            this.BlockValueBox.SelectAll();
            this.BlockValueBox.Focus();
        }

        public void BlockValuePaste(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();
        }


        public void CloseButton(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ModifyBlockValue(object sender, RoutedEventArgs e) {
            if (((MainWindow)this.Owner).currentSimulation != null) {
                string tmpValue = this.BlockValueBox.Text;
                tmpValue = tmpValue.Replace(" ", "");
                int newValue = Int32.Parse(tmpValue);
                ((MainWindow)this.Owner).currentSimulation.SetCustomMemoryValue(newValue, this.blockNum4);
            }
            this.Close();
        }

        private void ClearCurrentBlock(object sender, RoutedEventArgs e) {
            if (((MainWindow)this.Owner).currentSimulation != null)
            {
                ((MainWindow)this.Owner).currentSimulation.ResetValueAtMemory(this.blockNum4);
            }
            this.Close();
        }

        private void ClearAllCustomBlocks(object sender, RoutedEventArgs e) {
            if (((MainWindow)this.Owner).currentSimulation != null)
            {
                ((MainWindow)this.Owner).currentSimulation.ResetAllCustomValues();
            }
            this.Close();
        }

    }
}
