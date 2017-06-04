using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AssemblyFireView
{
    class CodeChecker
    {
        private Brush infoBrush = Brushes.CornflowerBlue;
        private Brush warningBrush = Brushes.SlateGray;
        private Brush errorBrush = Brushes.IndianRed;

        public string[] validRegisters = new string[] { "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9", "r10", "r11", "r12"};
        public string[] validReferences = new string[] { "[r0]", "[r1]", "[r2]", "[r3]", "[r4]", "[r5]", "[r6]", "[r7]", "[r8]", "[r9]", "[r10]", "[r11]", "[r12]" };

        private string emptyLineMSG = "La linea se encuentra vacia o sin contenido reconocible";
        private string invalidCommandMSG = "La primera entrada de la linea no es una instruccion valida";
        private string swiNotFoundMSG = "No se encontro la instruccion de parada en el codigo";
        private string swiArgumentsMSG = "La instruccion SWI no requiere parametros extra";
        private string args3RequieredMSG = "La instruccion requiere exactamente 3 argumentos";
        private string args2RequieredMSG = "La instruccion requiere exactamente 2 argumentos";
        private string args1RequieredMSG = "La instruccion requiere exactamente 1 argumento";
        private string invalidArgumentsMSG = "Uno o mas argumentos no son validos";

        string rawRtbData;
        RichTextBox target;
        int ErrorCount = 0;
        int WarningCount = 0;
        MainWindow sourceW;
        bool foundSWI = false;


        //Constructor
        public CodeChecker(string rawRtbData, RichTextBox target, MainWindow sourceW) {
            this.rawRtbData = rawRtbData;
            this.target = target;
            this.sourceW = sourceW;
        }

        //Does the whole check of the sintax
        public void DoCheck() {
            string s = rawRtbData;
            s = s.Replace("\n", "");
            s = s.Replace(",", " ");
         
            string[] stringSeparators = new string[] { "\r" };
            string[] lines = s.Split(stringSeparators, StringSplitOptions.None); 

            //Runs through every line and dispatch a single line check
            for (int i=0; i<lines.Length-1; i++) {                
                CheckCodeLine(i * 4, lines[i]);
            }

            //Checks if swi was present on the file
            if (!this.foundSWI) {
                sourceW.AddColoredText("Linea " + ((lines.Length-2)* 4) + " > Error: " + this.swiNotFoundMSG + "\r", this.errorBrush);
                this.ErrorCount++;
            }

            //Shows ending message with the appropiate color
            Brush endingBrush = this.infoBrush;
            if (ErrorCount > 0) { endingBrush = this.errorBrush; }
            else if (WarningCount > 0) { endingBrush = this.warningBrush; }            
            sourceW.AddColoredText("Terminado. "+this.ErrorCount + " errores y " + this.WarningCount + " advertencias", endingBrush);


            //Reset values
            this.ErrorCount = 0;
            this.WarningCount = 0;
            foundSWI = false;
        }

        //Checks if an string has only spaces
        public bool isNotOnlySpace(string s) {
            s = s.Replace(" ", "");
            return s.Length > 1;
        }

        public string replaceMultiSpaces(string s) {
            RegexOptions options = RegexOptions.None;
            Regex regex = new Regex("[ ]{2,}", options);
            s = regex.Replace(s, " ");
            return s;
        }

        public void CheckCodeLine(int lineNumber, string s){
            //Checks if the line is empty
            if (string.IsNullOrWhiteSpace(s)){
                sourceW.AddColoredText("Linea " + lineNumber + " > Advertencia: " + this.emptyLineMSG + "\r", this.warningBrush);
                this.WarningCount++;
                return;
            }
            //Splits the string
            string[] sep = new string[] { " " };
            string[] args = s.Split(sep, StringSplitOptions.RemoveEmptyEntries);

            if (args.Length < 1) { return; } //Safety check
            
            
            //Checks if the first string is a valid instruction            
            string tmpArg = args[0].ToLower();
            if (!sourceW.InstructionsARM.Contains(tmpArg))
            {
                sourceW.AddColoredText("Linea " + lineNumber +  " > Error: " + this.invalidCommandMSG + "\r", this.errorBrush);
                this.ErrorCount++;
                return;
            }

            //If it is an instruction checks if its SWI
            if (tmpArg == "swi")
            {
                this.foundSWI = true;
                //If its swi, it must have no parameters beside it
                if (args.Length > 1)
                {
                    sourceW.AddColoredText("Linea " + lineNumber + " > Error: " + this.swiArgumentsMSG + "\r", this.errorBrush);
                    this.ErrorCount++;
                }
            }
            else { //If its not swi, it checks which one it is and checks arguments for every case.
                CheckInstruction(lineNumber,args);
            }
        }





        //Checks the instruction and its arguments when its not swi
        public void CheckInstruction(int lineNumber, string[] args) { //Already checked that args.lenght > 0;


            //If its ADD/SUB must have Reg Reg Reg/Num
            if (args[0].ToLower() == "add" || args[0].ToLower() == "sub")
            {
                //Check if there are exactly 3 args 
                if (args.Length != 4)
                {
                    sourceW.AddColoredText("Linea " + lineNumber + " > Error: " + this.args3RequieredMSG + "\r", this.errorBrush);
                    this.ErrorCount++;
                    return;
                }
                //Checks if arguments are valid
                if (!isRegister(args[1]) || !isRegister(args[2]) || (!isRegister(args[3]) && !isPlainNumber(args[3])))
                {
                    sourceW.AddColoredText("Linea " + lineNumber + " > Error: " + this.invalidArgumentsMSG + "\r", this.errorBrush);
                    this.ErrorCount++;
                    return;
                }
            }


            //If its MOV must have Reg Reg/Num
            else if (args[0].ToLower() == "mov")
            {
                //Checks if the number of arguments is the correct
                if (args.Length != 3) {
                    sourceW.AddColoredText("Linea " + lineNumber + " > Error: " + this.args2RequieredMSG + "\r", this.errorBrush);
                    this.ErrorCount++;
                    return;
                }
                //Checks if the arguments are valid
                if (!isRegister(args[1]) || (!isRegister(args[2]) && !isPlainNumber(args[2])))
                {
                    sourceW.AddColoredText("Linea " + lineNumber + " > Error: " + this.invalidArgumentsMSG + "\r", this.errorBrush);
                    this.ErrorCount++;
                    return;
                }
            }

            //If its LDR/STR must have Reg Ref
            else if (args[0].ToLower() == "ldr" || args[0].ToLower() == "str")
            {
                //Checks if the number of arguments is the correct
                if (args.Length != 3)
                {
                    sourceW.AddColoredText("Linea " + lineNumber + " > Error: " + this.args2RequieredMSG + "\r", this.errorBrush);
                    this.ErrorCount++;
                    return;
                }
                //Checks if the arguments are valid
                if (!isRegister(args[1]) || !isMemoryReference(args[2]))
                {
                    sourceW.AddColoredText("Linea " + lineNumber + " > Error: " + this.invalidArgumentsMSG + "\r", this.errorBrush);
                    this.ErrorCount++;
                    return;
                }
            }

            //If its CMP must have Reg Reg
            else if (args[0].ToLower() == "cmp")
            {
                //Checks if the number of arguments is the correct
                if (args.Length != 3)
                {
                    sourceW.AddColoredText("Linea " + lineNumber + " > Error: " + this.args2RequieredMSG + "\r", this.errorBrush);
                    this.ErrorCount++;
                    return;
                }
                //Checks if the arguments are valid
                if (!isRegister(args[1]) || !isRegister(args[2]))
                {
                    sourceW.AddColoredText("Linea " + lineNumber + " > Error: " + this.invalidArgumentsMSG + "\r", this.errorBrush);
                    this.ErrorCount++;
                    return;
                }
            }

            //If its B** it must have a VALID MEMORY REFERENCE NUMBER (n%4=0)
            else if (args[0].ToLower().StartsWith("b"))
            {
                //Checks if the number of arguments is the correct
                if (args.Length != 2)
                {
                    sourceW.AddColoredText("Linea " + lineNumber + " > Error: " + this.args1RequieredMSG + "\r", this.errorBrush);
                    this.ErrorCount++;
                    return;
                }
                //Checks if the arguments are valid
                if (!isPlainNumberReference(args[1]))
                {
                    sourceW.AddColoredText("Linea " + lineNumber + " > Error: " + this.invalidArgumentsMSG + "\r", this.errorBrush);
                    this.ErrorCount++;
                    return;
                }
            }
        }

        

        //Checks if an string represents a valid register
        public bool isRegister(string arg) {
            arg = arg.Replace(",", ""); //Removes , if they exists
            arg = arg.ToLower();
            return this.validRegisters.Contains(arg);            
        }

        //Check if an string represents a valid memory value
        public bool isMemoryReference(string arg) {
            arg = arg.Replace(",", ""); //Removes , if they exists
            arg = arg.ToLower();
            return this.validReferences.Contains(arg);
        }
        
        //Check if an string representes a plain number
        public bool isPlainNumber(string arg) {
            arg = arg.Replace(",", ""); //Removes ,
            //If starts with # 
            if (arg.StartsWith("#")) { //And the rest represents a number
                arg = arg.Remove(0, 1);               
                int n;
                bool isNumeric = int.TryParse(arg, out n);
                return isNumeric;
            }
            return false;
        }

        //Check if an string representes a plain number memory reference
        public bool isPlainNumberReference(string arg)
        {
            arg = arg.Replace(",", ""); //Removes ,
            //If starts with # 
            if (arg.StartsWith("#"))
            { //And the rest represents a number
                arg = arg.Remove(0, 1);              
                int n;
                bool isNumeric = int.TryParse(arg, out n);
                if (isNumeric && n % 4 == 0) {
                    return true;
                }                
            }
            return false;
        }


        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

    }
}
