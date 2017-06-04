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
using System.Windows.Navigation;
using System.Windows.Shapes;
using FontAwesome.WPF;
using System.Text.RegularExpressions;
using System.Timers;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Animation;
using System.Threading;

namespace AssemblyFireView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        int LastRowCount = 0;
        int RowVariation = 4;
        public string[] InstructionsARM = new string[] { "add", "sub", "mov", "ldr", "str", "swi", "cmp", "beq", "bne", "bgt", "blt", "bge", "ble", "b" };
        public string[] validRegisters = new string[] { "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9", "r10", "r11", "r12" };
        public string[] validReferences = new string[] { "[r0]", "[r1]", "[r2]", "[r3]", "[r4]", "[r5]", "[r6]", "[r7]", "[r8]", "[r9]", "[r10]", "[r11]", "[r12]" };

        string AppVersion = "Build 0.1a "; //Modify on infoMSG too

        string OpenedFilePath = null;
        string OpenedFileName = null;
        bool ChangesSaved = true;

        public int memorySize = 256; //How many memory blocks ///////

        public Simulation currentSimulation = null;

        public bool keepValuesAtReset = true;

        bool isCheckModeOn = false;
        bool isRunModeOn = false;

        string cannonTitle = "Assemblyfire";

        string message_SaveTitle = "Guardar archivo";
        string message_SaveBeforeClose = "¡Hay cambios en el archivo que no han sido guardados!\n¿Deseas guardar el archivo actual antes de cerrar el programa?";
        string message_SaveBeforeOpen = "¡Hay cambios en el archivo que no han sido guardados!\n¿Deseas guardar el archivo actual antes de abrir un nuevo archivo?";
        string message_SaveBeforeNew = "¡Hay cambios en el archivo que no han sido guardados!\n¿Deseas guardar el archivo actual antes de crear un nuevo archivo?";

        string infoTitle = "Acerca de";
        string infoMSG = "Desarrollado por Reinaldo Jerez @ 2017.\n\nBuild 0.1a (.NET 4.5.2)";

        string errorMSG = "Error";
        string editProtectedBlock = "No es imposible editar bloques protegidos";

        public MainWindow()
        {
            InitializeComponent();
            Icon = ImageAwesome.CreateImageSource(FontAwesomeIcon.Fire, Brushes.White);            
            LineCountBox.AppendText("0\r");
            DataObject.AddPastingHandler(CodeEditBox, CodeBoxPasteFilter);
            tbVersion.Text = AppVersion;
            CodeEditBox.Focus();
            this.Title = cannonTitle + " - " + "Nuevo";
            OpenedFileName = "Nuevo";
            this.SimulationPanel.Visibility = Visibility.Collapsed;
            //this.btnStopRun.Visibility = Visibility.Collapsed;
            this.CheckPanel.Visibility = Visibility.Collapsed;
            this.runToolBar.Visibility = Visibility.Collapsed;
            this.CheckOutputBox.CaretBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            this.iniMemoryBlocks(); //Initializes memory blocks (changes requieres re-open)     

            //TODO:Must fix it
            this.PanelCodeBox.Visibility = Visibility.Visible;
            this.PanelLineCounter.Visibility = Visibility.Visible;
        }

      
        /* Handles the pasting event for the CodeEditbox */
        public void CodeBoxPasteFilter(object sender, DataObjectPastingEventArgs e){
            var isText = e.SourceDataObject.GetDataPresent(DataFormats.OemText, true);  //Checkea el tipo de dato

            if (!isText) { e.CancelCommand(); } //Comprobamos que sea texto

            //Obtenemos el texto y lo pasamos a string para eliminar cualquier formato
            string s = (string) e.SourceDataObject.GetData(DataFormats.StringFormat);
            s = s.Replace("\r\n", "\n");
            s = s.Replace("\r", "\n");

            string[] stringSeparators = new string[] { "\n" };
            string[] lines = s.Split(stringSeparators, StringSplitOptions.None);
            string newString = "";
            foreach (string sl in lines) {
                //Console.WriteLine(">" + sl + "<");
                newString += sl + "\r";
            }

            //Borramos la seleccion si es que existe alguna
            this.CodeEditBox.Selection.Text = "";
 
            //Agregamos el texto en la posicion del cursor
            CodeEditBox.CaretPosition.InsertTextInRun(newString);
            //UpdateKeyWordsHighlight(); //Updates the highligh of words/////////////////////////////////////////////////////////
            this.alternativeHighligh();
            e.CancelCommand(); //Cancelamos el pegado normal
        }

        /* Handles events when a key is released */
        private void CodeEditBox_KeyUp(object sender, KeyEventArgs e) {          
            if (e.Key == Key.Space || e.Key == Key.Enter || e.Key == Key.Return)
            {

                //UpdateKeyWordsHighlight(); //Checks for new words and the correct highlight.   
                this.alternativeHighligh();
            }
        }

        /* Handles events when text changes */
        private void CodeEditBox_TextChanged(object sender, EventArgs e)
        {
            if (ChangesSaved)
            {
                ChangesSaved = false;
                this.Title += "*";
            }
            UpdateLineCounter();               
        }

        /* Updates the line counter complement */
        private void UpdateLineCounter() {
            string s = new TextRange(CodeEditBox.Document.ContentStart, CodeEditBox.Document.ContentEnd).Text;
            int count = s.Count(f => f == '\r')-1;           

            while (count < LastRowCount) {          //Quitamos el ultimo numero

                string UpdatedLineCount =   new TextRange(LineCountBox.Document.ContentStart, 
                                            LineCountBox.Document.ContentEnd).Text.Replace(LastRowCount*RowVariation + "\r", "");               
                LastRowCount -= 1;
                LineCountBox.Document.Blocks.Clear();
                LineCountBox.AppendText(UpdatedLineCount);
            } 
            while (count > LastRowCount) {          //Agregamos un numero            
                LastRowCount += 1;
                LineCountBox.AppendText(LastRowCount*RowVariation + "\r");                 
            }
            if (count == -1) { //adds the first zero when no text is on the edit box
                LastRowCount += 1;
                LineCountBox.AppendText(LastRowCount * RowVariation + "\r");
            }
        } 

        /* Syncronizes both the code box and the line counter box */
        private void CodeEditorBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            LineCountBox.ScrollToVerticalOffset(CodeEditBox.VerticalOffset);
        }
        
        /* Updates the keywords highlight */
        void UpdateKeyWordsHighlight()
        {

            IEnumerable<TextRange> wordRanges = GetAllWordRanges(CodeEditBox.Document);

            foreach (TextRange wordRange in wordRanges)
            {                
                //Console.WriteLine(">" + wordRange.Text + "<");
                if (isCodeReference(wordRange.Text))
                {
                    wordRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DodgerBlue); //Memory reference
                }
                else if (isCodeRegister(wordRange.Text))
                {
                    wordRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.IndianRed); //Code register
                }
                else if (isCodeInstruction(wordRange.Text))
                {
                    wordRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Orange); //Code instruction
                }
                else
                {
                    wordRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White); //Any other caracters
                }
            }
        }



        /* Get all words ranges in the document */
        public IEnumerable<TextRange> GetAllWordRanges(FlowDocument document)
        {
            bool cchecked = false;
             string pattern = @"add|sub|mov|ldr|str|swi|cmp|beq|bne|bgt|blt|bge|ble|b\b|\[r0\]|\[r10\]|\[r11\]|\[r12\]|\[r1\]|\[r2\]|\[r3\]|\[r4\]|\[r5\]|\[r6\]|\[r7\]|\[r8\]|\[r9\]|r0|r10|r11|r12|r1|r2|r3|r4|r5|r6|r7|r8|r9";
            TextPointer pointer = document.ContentStart;
            while (pointer != null)
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = pointer.GetTextInRun(LogicalDirection.Forward);
                    Console.WriteLine("====================================="+textRun+"========================================");
                    MatchCollection matches = Regex.Matches(textRun.ToLower(), pattern, RegexOptions.Compiled);
                    foreach (Match match in matches)
                    {
                        int startIndex = match.Index;
                        int length = match.Length;
                        TextPointer start = pointer.GetPositionAtOffset(startIndex);
                        TextPointer end = start.GetPositionAtOffset(length);
                        Console.WriteLine("Matches: " + match + " | Start: " + startIndex+" | End: "+(startIndex+length));
                        yield return new TextRange(start, end);
                    }
                    cchecked = true;
                }

                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
                if (cchecked) { pointer = null; }
            }
        }

        /* Another way to highlight syntax */
        public void alternativeHighligh() {
            string pattern = @"add|sub|mov|ldr|str|swi|cmp|beq|bne|bgt|blt|bge|ble|b\b|\[r0\]|\[r10\]|\[r11\]|\[r12\]|\[r1\]|\[r2\]|\[r3\]|\[r4\]|\[r5\]|\[r6\]|\[r7\]|\[r8\]|\[r9\]|r0|r10|r11|r12|r1|r2|r3|r4|r5|r6|r7|r8|r9";
            
            TextPointer pointer = this.CodeEditBox.Document.ContentStart;

            while (pointer != null)
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = pointer.GetTextInRun(LogicalDirection.Forward);
                   
                    MatchCollection matches = Regex.Matches(textRun.ToLower(), pattern, RegexOptions.Compiled);
                    foreach (Match match in matches)
                    {
                        int startIndex = match.Index;
                        int length = match.Length;
                        TextPointer start = pointer.GetPositionAtOffset(startIndex);
                        TextPointer end = start.GetPositionAtOffset(length);
                        TextRange txtRg = new TextRange(start, end);
                        if (isCodeReference(txtRg.Text))
                        {
                            txtRg.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DodgerBlue); //Memory reference
                        }
                        else if (isCodeRegister(txtRg.Text))
                        {
                            txtRg.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.IndianRed); //Code register
                        }
                        else if (isCodeInstruction(txtRg.Text))
                        {
                            txtRg.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Orange); //Code instruction
                        }
                                            
                    }
                   
                }
                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
               
            }


            pattern = @"[^\s]+";
            pointer = this.CodeEditBox.Document.ContentStart;
            while (pointer != null)
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = pointer.GetTextInRun(LogicalDirection.Forward);
                    MatchCollection matches = Regex.Matches(textRun.ToLower(), pattern, RegexOptions.ExplicitCapture);
                    foreach (Match match in matches)
                    {
                        int startIndex = match.Index;
                        int length = match.Length;
                        TextPointer start = pointer.GetPositionAtOffset(startIndex);
                        TextPointer end = start.GetPositionAtOffset(length);
                        TextRange txtRg = new TextRange(start, end);
                        if (!isCodeReference(txtRg.Text.Replace(",", "")) && !isCodeInstruction(txtRg.Text) && !isCodeRegister(txtRg.Text.Replace(",","")))
                        {
                            txtRg.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White); //Memory reference
                        }
                       
                        
                    }
                  
                }
                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
                
            }

        }

        /* Checks whether a string is an instruction or not */
        public bool isCodeInstruction(string s) {
            s = s.ToLower();
            return InstructionsARM.Contains(s);
        }

        public bool isCodeRegister(string s) {
            s = s.Replace(",", ""); //Removes , if they exists
            s = s.ToLower();
            return this.validRegisters.Contains(s);
        }

        public bool isCodeReference(string s) {
            s = s.Replace(",", ""); //Removes , if they exists
            s = s.ToLower();
            return this.validReferences.Contains(s);
        }


        public void OpenFileButton(object sender, RoutedEventArgs e) {
            this.CodeEditBox.Focus();
            if (!ChangesSaved)
            {
                MessageBoxResult mbr = DialogSaveData(message_SaveTitle, message_SaveBeforeOpen);
                if (mbr == MessageBoxResult.Cancel) { return; }
                else if (mbr == MessageBoxResult.Yes)
                {
                    SaveFileButton(null, null);
                    if (!ChangesSaved)
                    { //Checks if changes were saved
                        return;
                    }
                }                
            }


            if (isCheckModeOn || isRunModeOn) {return;} //Only allows loading files when editing mode is on
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;            
            if (openFileDialog.ShowDialog() == true)               
                RTBLoadNewText(openFileDialog.FileName, openFileDialog.SafeFileName, System.IO.File.ReadAllText(openFileDialog.FileName));
            
        }

        public void RTBLoadNewText(string fullpath, string name, string text) {
            OpenedFilePath = fullpath;
            OpenedFileName = name;
            CodeEditBox.Document.Blocks.Clear();
            //text = text.Replace("\n", "");
            CodeEditBox.AppendText(text);
            alternativeHighligh();
            this.Title = cannonTitle + " - " + name;
            ChangesSaved = true;
        }

        public void NewFileButton(object sender, RoutedEventArgs e) {
            this.CodeEditBox.Focus();
            if (!ChangesSaved)
            {
                MessageBoxResult mbr = DialogSaveData(message_SaveTitle, message_SaveBeforeNew);
                if (mbr == MessageBoxResult.Cancel) { return; }
                else if (mbr == MessageBoxResult.Yes) {
                    SaveFileButton(null, null);
                    if (!ChangesSaved) { //Checks if changes were saved
                        return;
                    }
                }
            }
                        
            OpenedFilePath = null;
            OpenedFileName = "Nuevo";            
            CodeEditBox.Document.Blocks.Clear();
            this.Title = cannonTitle + " - " + "Nuevo";
            ChangesSaved = true;
            //this.CodeEditBox.AppendText("");
            this.UpdateLineCounter();
        }

        public void SaveFileButton(object sender, RoutedEventArgs e) {
            this.CodeEditBox.Focus();
            string tmpFilePath = "";
            if (OpenedFilePath == null) { //Si la ruta del archivo no esta especificada
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.FileName = OpenedFileName; // Default file name
                dlg.DefaultExt = ".txt"; // Default file extension
                Nullable<bool> result = dlg.ShowDialog();
                if (result == true) {
                    tmpFilePath = dlg.FileName;
                    OpenedFilePath = tmpFilePath;
                    OpenedFileName = dlg.SafeFileName;                    
                }
            }
            else {
                tmpFilePath = OpenedFilePath;
            }

            if (OpenedFilePath != "" && OpenedFilePath != null)
            {
                StreamWriter writer = new StreamWriter(tmpFilePath);
                string codeText = new TextRange(CodeEditBox.Document.ContentStart, CodeEditBox.Document.ContentEnd).Text;
                writer.Write(codeText);
                writer.Dispose();
                writer.Close();

                ChangesSaved = true;
                this.Title = cannonTitle + " - " + OpenedFileName; //Si es que cambia el archivo                
            }       
        }

        public MessageBoxResult DialogSaveData(string xtitle, string msg) {
            ConfirmDialog cd = new ConfirmDialog();
            cd.Owner = this;
            cd.SetDialogMessage(msg);
            cd.SetDialogTitle(xtitle);
            cd.ShowDialog();
            return cd.result;           
        }



        public void WindowClosingEvent(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ChangesSaved)
            {
                MessageBoxResult mbr = DialogSaveData(message_SaveTitle, message_SaveBeforeClose);
                if (mbr == MessageBoxResult.Cancel) { e.Cancel = true; }
                else if (mbr == MessageBoxResult.Yes)
                {
                    SaveFileButton(null, null);
                    if (!ChangesSaved)
                    { //Checks if changes were saved
                        e.Cancel = true;
                    }
                }
            }
        }

        //RUN SIMULATION
        public void RunSimulationButton(object sender, RoutedEventArgs e) {
            //Hides check panel whether its open or not and fixes the button status.
            this.CheckPanel.Visibility = Visibility.Collapsed;   
            this.btnCheck.Visibility = Visibility.Visible;
            this.btnReCheck.Visibility = Visibility.Collapsed;

            //Shows the simulation panel and hides the other one
            this.PanelCodeBox.Visibility = Visibility.Collapsed; //
            this.PanelLineCounter.Visibility = Visibility.Collapsed; //  
            this.SimulationPanel.Visibility = Visibility.Visible;

            this.toolBar.Visibility = Visibility.Collapsed;
            this.runToolBar.Visibility = Visibility.Visible;


            string s = new TextRange(CodeEditBox.Document.ContentStart, CodeEditBox.Document.ContentEnd).Text;
            s = s.Replace("\n", "");
            s = s.Replace(",", " ");

            string[] stringSeparators = new string[] { "\r" };
            string[] lines = s.Split(stringSeparators, StringSplitOptions.None);

            //Starts a new simulation
            this.currentSimulation = new Simulation(this, lines);
            //Initializes the simulation
            this.currentSimulation.Initialize();

            isRunModeOn = true;
            AreEnabledComponentsOnRun(false);

            txtBtnRun.Text = " Terminar Ejecucion";
            icoBtnRun.Foreground = Brushes.Crimson;

            //Changes the bottom bar color and text
            bottomStatusBar.Background = Brushes.Firebrick;
            this.BorderBrush = Brushes.Firebrick; 
            this.bottomWindowStatus.Text = "Simulacion";            
        }
    

        ///STOP SIMULATION
        public void StopSimulationButton(object sender, RoutedEventArgs e) {
            this.SimulationPanel.Visibility = Visibility.Collapsed;
            this.PanelCodeBox.Visibility = Visibility.Visible; //
            this.PanelLineCounter.Visibility = Visibility.Visible; //  


            this.toolBar.Visibility = Visibility.Visible;
            this.runToolBar.Visibility = Visibility.Collapsed;


            //Erases the current run
            this.currentSimulation = null;


            isRunModeOn = false;
            AreEnabledComponentsOnRun(true);
            txtBtnRun.Text = " Ejecutar";
            icoBtnRun.Foreground = Brushes.Green;
            bottomStatusBar.Background = (Brush)new BrushConverter().ConvertFrom("#FF007ACC");
            this.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#FF007ACC");
            this.bottomWindowStatus.Text = "Editor de codigo";



        }

        public void AreEnabledComponentsOnRun(bool status) {
            btnCheck.IsEnabled = status;
            btnNew.IsEnabled = status;
            btnOpen.IsEnabled = status;
            btnSave.IsEnabled = status;
            btnRun.IsEnabled = status;
            CodeEditBox.IsReadOnly = !status;
        }

        public void ButtonCheckCode(object sender, RoutedEventArgs e) {
            this.btnCheck.Visibility = Visibility.Collapsed;
            this.btnReCheck.Visibility = Visibility.Visible;


            this.CheckOutputBox.Document.Blocks.Clear(); //Limpia los bloques
            string codeText = new TextRange(CodeEditBox.Document.ContentStart, CodeEditBox.Document.ContentEnd).Text;
            CodeChecker cc = new CodeChecker(codeText, this.CheckOutputBox, this);
            this.CheckPanel.Visibility = Visibility.Visible;
            this.CheckOutputBox.Focus();
            cc.DoCheck(); //Do the checking            
            TextPointer caretPos = this.CheckOutputBox.CaretPosition;
            caretPos = caretPos.DocumentEnd;
            this.CheckOutputBox.CaretPosition = caretPos;
            this.CheckOutputBox.ScrollToEnd();
        }

        public void ButtonHideCheckResult(object sender, RoutedEventArgs e) {
            //Restores the button icon and color
            this.btnCheck.Visibility = Visibility.Visible;
            this.btnReCheck.Visibility = Visibility.Collapsed;

            this.CheckPanel.Visibility = Visibility.Collapsed;
        }

        public void AddColoredText(string s, Brush b) {
            TextRange textRange = new TextRange(CheckOutputBox.Document.ContentEnd, CheckOutputBox.Document.ContentEnd);
            textRange.Text = s;
            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, b);
        }

        private void StepBackSimulation(object sender, RoutedEventArgs e)
        {
            if (this.currentSimulation != null) {
                this.currentSimulation.DoStepBack();
            }
        }

        //Sets the memory blocks in the grid
        public void iniMemoryBlocks() {
            for (int i = 0; i < this.memorySize ; i++) //MemorySize
            {
                this.MemoryDisplayGrid.RowDefinitions.Add(new RowDefinition());
                //Mem post
                TextBlock memPos = new TextBlock();
                memPos.Text = "" + i * 4;
                memPos.FontWeight = FontWeights.Bold;
                memPos.Foreground = Brushes.LightGray;
                memPos.SetValue(Grid.ColumnProperty, 0);
                memPos.SetValue(Grid.RowProperty, i);
                memPos.HorizontalAlignment = HorizontalAlignment.Right;
                memPos.VerticalAlignment = VerticalAlignment.Center;
                memPos.Margin = new Thickness(0, 0, 15, 10);
                                
                //Mem value
                string name = "memBlock" + i*4;
                TextBox memValue = new TextBox();
                                          
                memValue.Text = name;
                memValue.SetValue(Grid.ColumnProperty, 1);
                memValue.SetValue(Grid.RowProperty, i);
                memValue.MaxLines = 1;
                memValue.IsReadOnly = true;
                memValue.MaxHeight = 26;
                memValue.BorderThickness = new Thickness(4, 0, 0, 0);
                memValue.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF363535"));
                memValue.BorderBrush = Brushes.DarkGray;//Brushes.Firebrick;
                memValue.Margin = new Thickness(0, 0, 0, 10);
                memValue.Padding = new Thickness(5, 0, 0, 0);
                memValue.Tag = "" + (i * 4);
                memValue.MouseDoubleClick += MemBlockShowMenu;
                                              
                this.MemoryDisplayGrid.Children.Add(memPos);
                this.MemoryDisplayGrid.Children.Add(memValue);
                this.RegisterName(name, memValue);
            }
        }
             

        public void ResetSimulation(object sender, RoutedEventArgs e) {
            if (this.currentSimulation!=null)
                this.currentSimulation.Reset();
        }

        public void StepSimulation(object sender, RoutedEventArgs e) {
            if (this.currentSimulation != null) {
                this.currentSimulation.DoStep();
            }
        }

        public void AllSimulation(object sender, RoutedEventArgs e) {
            if (this.currentSimulation != null) {
                this.currentSimulation.DoAll();
            }
        }

        public void ShowInfoDialog(string title, string msg) {
            InfoDialog id = new InfoDialog();
            id.SetDialogValues(title, msg);
            id.Owner = this;
            id.ShowDialog();
        }

        public void ButtonShowInfo(object sender, RoutedEventArgs e)
        {
            this.ShowInfoDialog(this.infoTitle, this.infoMSG);
        }

        public void ButtonShowCheatsheet(object sender, RoutedEventArgs e) {
            Cheatsheet cs = new Cheatsheet();
            cs.Owner = this;
            cs.ShowDialog();
        }
        
        //Menu to edit memory blocks values
        public void MemBlockShowMenu(object sender, RoutedEventArgs e) {
            if (this.currentSimulation == null) { return; } //If simulation exists
            string tagValue = (string) (((TextBox)sender).Tag);
            string blockValue = ((TextBox)sender).Text;
            int tagNumValue = Int32.Parse(tagValue);
            if (this.currentSimulation.IsProtectedBlock(tagNumValue)) { //And if its not a protected block
                this.ShowInfoDialog(this.errorMSG, this.editProtectedBlock);
                return;
            }
            //Show edit dialog.
            BlockEditor be = new BlockEditor();
            be.SetValues(this, tagNumValue, blockValue);
            be.ShowDialog();
        }

    }
}
