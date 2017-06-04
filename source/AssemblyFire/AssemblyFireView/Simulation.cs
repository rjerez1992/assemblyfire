using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace AssemblyFireView
{
    public class Simulation
    {
        private string errorDuringExecution = "Error durante la ejecucion";
        private string errorProtectedBlockMSG = "Se ha intentado acceder a un bloque protegido durante la ejecucion de una instruccion";
        private string executionOverMSG = "La ejecucion del ciclo actual ya ha terminado";
        private string errorEndOfMemoryMSG = "Se ha alcanzado el final de la memoria";
        private string errorOutOfMemoryMSG = "No hay memoria suficiente para cargar el programa";
        private string errorInstructionInvalid = "Se ha intentado ejecutar una instruccion invalida";
        private string errorInvalidParameters = "Se ha intentado ejecutar una instruccion con parametros invalidos";
        private string infoExecution = "Informacion de ejecucion";
        private string successMSG = "La ejecucion ha terminado con exito";
        private string successResetMSG = "Se han reiniciado los valores de la ejecucion actual";
        private string errorDuringEdit = "Error de modificacion";
        private string errorModifyBlock = "Ha ocurrido un error al intentar modificar un bloque de memoria";

        public string[] validRegisters = new string[] { "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9", "r10", "r11", "r12" };
        public string[] validReferences = new string[] { "[r0]", "[r1]", "[r2]", "[r3]", "[r4]", "[r5]", "[r6]", "[r7]", "[r8]", "[r9]", "[r10]", "[r11]", "[r12]" };

        public Brush currentColorUsed = Brushes.Firebrick;
        public Brush currentPositionColor = Brushes.CornflowerBlue;

        private MainWindow target;
        private bool CicleEnded;
        private Dictionary<int, int> BlockCustomValues; //First is block number and must be on 4* and second is value.
        private List<string> Memory;
        private string[] programm; //TODO:Lines recieved on the programm must have empty lines
        private List<int> Registers;
        private int PC;
        private int ZeroFlag;
        private int NegFlag;
        private string IR;
        private int stepCounter;

        public Simulation(MainWindow target, string[] programm) {
            this.target = target;
            this.CicleEnded = false;
            this.programm = programm.Take(programm.Count() - 1).ToArray(); //Programm cant have more than 256 lines (remember to check when editing memory block)

            this.Memory = new List<string>();
            this.BlockCustomValues = new Dictionary<int, int>();
            this.Registers = new List<int>();


            //Fills memory array
            for (int i = 0; i < target.memorySize; i++)
            {
                this.Memory.Add("0");
            }

            //Fills registers array
            for (int i = 0; i < 16; i++)
            {
                this.Registers.Add(0);
            }
        }

        //Initializes everything.
        public void Initialize() {
            this.CicleEnded = false; //Restarts the cicle
            this.stepCounter = 0;

            this.currentColorUsed = Brushes.DimGray;

            //Sets all memory on zero
            for (int i = 0; i < target.memorySize; i++)
            {
                this.SetMemoryValueBypass(i * 4, "0");
            }

            this.currentColorUsed = Brushes.Firebrick;
            //Reloads the programm
            this.ReloadProgramm(); 

            this.currentColorUsed = Brushes.DimGray;
            //Sets all registers on zero
            for (int i = 0; i < 13; i++)
            {
                this.SetRegisterValue(i, 0);
            }
            //And sets the register 15
            this.SetRegisterValue(15, 0);

            this.currentColorUsed = Brushes.Firebrick;
            //Re-sets the customs blocks on memory
            foreach (KeyValuePair<int, int> entry in this.BlockCustomValues)
            {
                this.SetMemoryValue(entry.Key, "" + entry.Value);
            }

            this.currentColorUsed = Brushes.DimGray;
            //Sets standard starting values
            this.SetPC(0);
            //this.SetIR(); //Reads the pc value and loads that instruction
            this.SetIREmpty(); //Starts empty
            this.SetNegFlag(0);
            this.SetZeroFlag(0);
            this.currentColorUsed = Brushes.Firebrick;

            //Paints the first memory position
            TextBox newTxb = (TextBox)this.target.MemoryDisplayGrid.FindName("memBlock0"); //Graphic
            newTxb.BorderBrush = this.currentPositionColor;

        }

        public void DoStep() {
            if (this.CicleEnded) {//Checks if we still on the run
                this.target.ShowInfoDialog(this.errorDuringExecution, this.executionOverMSG);
                return;
            }
            stepCounter++; //Check wheter to put it in here or before the last instruction.

            this.SetIR(); //Fetchs the instruction from the memory

            int startingPCValue = this.PC; //Saves the value for later coloring
            this.SetPC(this.PC + 4); //Sets the pc to its new value
                                    
            //Runs the instruction
            if (!this.RunInstruction()){//Stops if instruction was not successful.     
                this.CicleEnded = true;
                return;
            }

            //If pc value/4 > memoryLimit-1, then we stop the cycle
            if (this.PC / 4 > this.target.memorySize - 1) //Reached end of memory
            {
                this.target.ShowInfoDialog(this.errorDuringExecution, this.errorEndOfMemoryMSG);
                this.CicleEnded = true;
            }
            else {//Paints the current value of the pc
                //Paints the last one
                TextBox tmpTxb = (TextBox)this.target.MemoryDisplayGrid.FindName("memBlock" + startingPCValue); //Graphic
                tmpTxb.BorderBrush = this.currentColorUsed;

                //Paints the next one
                TextBox newTxb = (TextBox)this.target.MemoryDisplayGrid.FindName("memBlock" + this.PC); //Graphic
                newTxb.BorderBrush = this.currentPositionColor;
            }
        }


        //Easiest thing ever.
        public void DoAll() {
            while (!CicleEnded) {
                DoStep();
            }
        }

        public void Reset() {
            //if (!target.keepValuesAtReset) {
            //    this.BlockCustomValues.Clear(); //Removes all values
            //}
            this.Initialize(); //Its the same
            this.target.ShowInfoDialog(this.infoExecution, this.successResetMSG); //Mensaje
        }

        private void HiddenReset() {
            this.Initialize(); //Its the same
        }

        public void DoStepBack() {
            int lastStepCount = this.stepCounter - 1;

            //Hard reset without message
            if (!target.keepValuesAtReset)
            {
                this.BlockCustomValues.Clear(); //Removes all values
            }
            this.Initialize(); //Its the same

            for (int i = 0; i < lastStepCount; i++) {
                this.DoStep();
            }
        }

        public void SetCustomMemoryValue(int value, int memBlock) {
            //Checks if value is correct pls and smaller than memory
            if (memBlock % 4 != 0 || IsProtectedBlock(memBlock)) {
                this.target.ShowInfoDialog(this.errorDuringEdit, this.errorModifyBlock);
                return;
            }
            if (this.BlockCustomValues.ContainsKey(memBlock)) { //If already contains the key changes the value
                this.BlockCustomValues[memBlock] = value;
            }
            else
            {
                this.BlockCustomValues.Add(memBlock, value); //If not it adds the value and the key
            }
            
            this.HiddenReset(); //May change it for a subReset();
        }

        public void ResetValueAtMemory(int memBlock) {
            if (memBlock % 4 != 0 || IsProtectedBlock(memBlock))
            {
                this.target.ShowInfoDialog(this.errorDuringEdit, this.errorModifyBlock);
                return;
            }
            this.BlockCustomValues.Remove(memBlock);
            this.HiddenReset();
        }

        public void ResetAllCustomValues() {
            this.BlockCustomValues.Clear();
            this.HiddenReset();
        }

        //Block must be (4*) and value must be an string
        private void SetMemoryValue(int block4, string value) {
            //Checks thats not a protected one
            if (this.IsProtectedBlock(block4)){//Must display an error
                this.target.ShowInfoDialog(this.errorDuringExecution, this.errorProtectedBlockMSG);
                this.CicleEnded = true; //Ends the cicle.
                return;
            } 
            this.Memory[block4 / 4] = value; //Code
            TextBox tmpTxb = (TextBox)this.target.MemoryDisplayGrid.FindName("memBlock" + block4); //Graphic
            tmpTxb.BorderBrush = this.currentColorUsed;
            tmpTxb.Text = value.ToUpper() + "";
        }

        //THIS IS TO LOAD THE PROGRAMM, BYPASS PROTECTED BLOCKS PROTECTION
        private void SetMemoryValueBypass(int block4, string value)
        { 
            this.Memory[block4 / 4] = value; //Code
            TextBox tmpTxb = (TextBox)this.target.MemoryDisplayGrid.FindName("memBlock" + block4); //Graphic
            tmpTxb.Text = value.ToUpper() + "";
            tmpTxb.BorderBrush = this.currentColorUsed;
        }


        //Sets a register value
        private void SetRegisterValue(int register, int value) { //Value must be 0-12 / 15
            this.Registers[register] = value; //Code
            TextBox tmpTxb = (TextBox)this.target.MemoryDisplayGrid.FindName("RunBoxR"+register); //Graphic
            tmpTxb.Text = value + "";
            if (currentColorUsed == Brushes.Firebrick)
            {
                tmpTxb.BorderBrush = Brushes.CornflowerBlue;
            }
            else {
                tmpTxb.BorderBrush = this.currentColorUsed;
            }
            
        } 

        private void SetPC(int value) {//Must set register 15 as well.          
            this.PC = value; //Class
            this.SetRegisterValue(15, value); //Register
            //Graphic
            this.target.RunBoxPC.Text = value + "";
            this.target.RunBoxPC.BorderBrush = Brushes.CornflowerBlue;                        
        } 

        private void SetIR() {
            this.IR = this.Memory[this.PC / 4].ToUpper(); ; //Code**
            this.target.RunBoxIR.Text = this.IR;
            this.target.RunBoxIR.BorderBrush = this.currentColorUsed;
        }

        private void SetIREmpty()
        {
            this.IR = "";
            this.target.RunBoxIR.Text = "";
            this.target.RunBoxIR.BorderBrush = this.currentColorUsed;
        }

        private void SetNegFlag(int value) {
            this.NegFlag = value;
            this.target.RunBoxNF.Text = value + "";
            this.target.RunBoxNF.BorderBrush = this.currentColorUsed;
        }

        private void SetZeroFlag(int value) {
            this.ZeroFlag = value;
            this.target.RunBoxZF.Text = value + "";
            this.target.RunBoxZF.BorderBrush = this.currentColorUsed;
        }

        private void ReloadProgramm() {
            if (this.programm.Length >= this.target.memorySize) {
                this.CicleEnded = true;
                this.target.ShowInfoDialog(this.errorDuringExecution, this.errorOutOfMemoryMSG);
                return; //If not the program my crash.
            }
            for (int i = 0; i < this.programm.Length; i++) {
                //this.Memory[i] = this.programm[i];
                this.SetMemoryValueBypass(i * 4, this.programm[i]);
            }
        }

        public bool IsProtectedBlock(int value4) {
            if (value4 <= (this.programm.Length - 1) * 4) {
                return true;
            }
            return false;
        }
        
        private bool RunInstruction() { //Runs the instruction on the IR, if error return value of success
            string tmpIR = this.IR.Replace(",", ""); //Removes commads
            bool validArgs = true; //Starts as true;

            //Splits the string
            string[] sep = new string[] { " " };
            string[] args = tmpIR.Split(sep, StringSplitOptions.RemoveEmptyEntries);

            if (args.Length < 1) {
                this.target.ShowInfoDialog(this.errorDuringExecution, this.errorInstructionInvalid);
                this.CicleEnded = true;
                return false;
            }

            //Checks if the first string is a valid instruction            
            string tmpArg = args[0].ToLower();

            //If instruction doesnt exist
            if (!target.InstructionsARM.Contains(tmpArg))
            {
                this.CicleEnded = true;
                this.target.ShowInfoDialog(this.errorDuringExecution, this.errorInstructionInvalid);
                return false;
            }

            if (tmpArg == "swi") {//Ends the execution
                this.CicleEnded = true;
                this.target.ShowInfoDialog(this.infoExecution, this.successMSG);
                return true;
            }

            //If its ADD/SUB must have Reg Reg Reg/Num
            if (args[0].ToLower() == "add" || args[0].ToLower() == "sub")
            {
                //Check if there are exactly 3 args 
                if (args.Length != 4)
                {
                    validArgs = false;
                }
                //Checks if arguments are valid
                if (!isRegister(args[1]) || !isRegister(args[2]) || (!isRegister(args[3]) && !isPlainNumber(args[3])))
                {
                    validArgs = false;
                }
                if (validArgs) {
                    if (args[0].ToLower() == "add")
                    {
                        return ADD(args);
                    }
                    else {
                        return SUB(args);
                    }
                }
            }



            //If its MOV must have Reg Reg/Num
            else if (args[0].ToLower() == "mov")
            {
                //Checks if the number of arguments is the correct
                if (args.Length != 3)
                {
                    validArgs = false;
                }
                //Checks if the arguments are valid
                if (!isRegister(args[1]) || (!isRegister(args[2]) && !isPlainNumber(args[2])))
                {
                    validArgs = false;
                }
                if (validArgs) {
                    return MOV(args);
                }
            }

            //If its LDR/STR must have Reg Ref
            else if (args[0].ToLower() == "ldr" || args[0].ToLower() == "str")
            {
                //Checks if the number of arguments is the correct
                if (args.Length != 3)
                {
                    validArgs = false;
                }
                //Checks if the arguments are valid
                if (!isRegister(args[1]) || !isMemoryReference(args[2]))
                {
                    validArgs = false;
                }
                if (validArgs) {
                    if (args[0].ToLower() == "ldr")
                    {
                        return LDR(args);
                    }
                    else {
                        return STR(args);
                    }
                }
            }


            //If its CMP must have Reg Reg
            else if (args[0].ToLower() == "cmp")
            {
                //Checks if the number of arguments is the correct
                if (args.Length != 3)
                {
                    validArgs = false;
                }
                //Checks if the arguments are valid
                if (!isRegister(args[1]) || !isRegister(args[2]))
                {
                    validArgs = false;
                }
                if (validArgs) {
                    return CMP(args);
                }
            }


            //If its B** it must have a VALID MEMORY REFERENCE NUMBER (n%4=0)
            else if (args[0].ToLower().StartsWith("b"))
            {
                //Checks if the number of arguments is the correct
                if (args.Length != 2)
                {
                    validArgs = false;
                }
                //Checks if the arguments are valid
                if (!isPlainNumberReference(args[1]))
                {
                    validArgs = false;
                }
                if (validArgs) {
                    return BXX(args);
                }
            }

            //Throws error message and returns false
            this.target.ShowInfoDialog(this.errorDuringExecution, this.errorInvalidParameters);
            return false; 
        }
       

        private bool ADD(string[] args) {
            int regNum = this.getRegisterNumber(args[1]);
            int refA = this.getRegisterNumber(args[2]);
            int sumA = this.Registers[refA];
            int sumB = 0;
           
            if (this.isRegister(args[3]))
            {
                int refB = this.getRegisterNumber(args[3]);
                sumB = this.Registers[refB];
            }
            else {
                sumB = this.getRawValueNumber(args[3]);                
            }
            this.SetRegisterValue(regNum, sumA + sumB);
            return true;
        }



        private bool SUB(string[] args) {
            int regNum = this.getRegisterNumber(args[1]);
            int refA = this.getRegisterNumber(args[2]);
            int sumA = this.Registers[refA];
            int sumB = 0;

            if (this.isRegister(args[3]))
            {
                int refB = this.getRegisterNumber(args[3]);
                sumB = this.Registers[refB];
            }
            else
            {
                sumB = this.getRawValueNumber(args[3]);
            }
            this.SetRegisterValue(regNum, sumA - sumB); ///CHECK SUB DIRECTIOn
            return true;
        }



        private bool MOV(string[] args) {
            int regNum = this.getRegisterNumber(args[1]);
            int newValue = 0;

            if (this.isRegister(args[2]))
            {
                int refB = this.getRegisterNumber(args[2]);
                newValue = this.Registers[refB];
            }
            else
            {
                newValue = this.getRawValueNumber(args[2]);
            }
            this.SetRegisterValue(regNum, newValue);
            return true;
        }

        private bool LDR(string[] args){//Must check if trying to load a protected block
            int regNum = this.getRegisterNumber(args[1]);
            int memRef = this.GetReferenceValueNumber(args[2]);
            memRef = this.Registers[memRef];
            if (IsProtectedBlock(memRef)) { return false; } /////////////////////////Should show an error message

            int memVal;
            bool isNumeric = int.TryParse(this.Memory[memRef/4], out memVal);

            this.SetRegisterValue(regNum, memVal);
            return true;
        } 

        private bool STR(string[] args){ //Must check if trying to load a protected block
            int regNum = this.getRegisterNumber(args[1]);
            int memRef = this.GetReferenceValueNumber(args[2]);
            memRef = this.Registers[memRef];
            if (IsProtectedBlock(memRef)) { return false; } /////////////////////////Should show an error message

            string strValue = ""+this.Registers[regNum];

            this.SetMemoryValue(memRef, strValue);
           
            return true;

        } 

        private bool CMP(string[] args) {
            int regNumA = this.getRegisterNumber(args[1]);
            int regNumB = this.getRegisterNumber(args[2]);
            int regValA = this.Registers[regNumA];
            int regValB = this.Registers[regNumB];

            int result = regValA - regValB;

            if (result < 0)
            {
                this.SetNegFlag(1); //Is negative
            } else {
                this.SetNegFlag(0);
            }

            if (result == 0)
            {
                this.SetZeroFlag(1); //Is zero
            }
            else {
                this.SetZeroFlag(0);
            }
            return true;
        }

        private bool BXX(string[] args){ //Internally checks which type of jump it is.
            int jumpTarget = this.getValueNumber(args[1]); //Gets the jump target
            string inst = args[0].ToLower();
            //if its a b always jumps
            if (inst == "b")
            {
                this.SetPC(jumpTarget);
            }

            //Jumps if flag zero is 1
            else if (inst == "beq")
            {
                if (this.ZeroFlag == 1)
                {
                    this.SetPC(jumpTarget);
                }
            }

            //Jumps if flag zero is 0
            else if (inst == "bne")
            {
                if (this.ZeroFlag == 0)
                {
                    this.SetPC(jumpTarget);
                }
            }

            //jumps n y z = 0
            else if (inst == "bgt")
            {
                if (this.ZeroFlag == 0 && this.NegFlag == 0)
                {
                    this.SetPC(jumpTarget);
                }
            }

            //jumps if n = 1
            else if (inst == "blt")
            {
                if (this.NegFlag == 1)
                {
                    this.SetPC(jumpTarget);
                }
            }

            //Jumps if n=0
            else if (inst == "bge")
            {
                if (this.NegFlag == 0)
                {
                    this.SetPC(jumpTarget);
                }
            }

            //Jumps if n = 1 or z = 1
            else if (inst == "ble") {
                if (this.NegFlag == 1 || this.ZeroFlag == 1) {
                    this.SetPC(jumpTarget);
                }
            }

            return true;
        } 

        ///EXTRA CHECKS
        //Checks if an string represents a valid register
        public bool isRegister(string arg)
        {
            arg = arg.Replace(",", ""); //Removes , if they exists
            arg = arg.ToLower();
            return this.validRegisters.Contains(arg);
        }

        //Check if an string represents a valid memory value
        public bool isMemoryReference(string arg)
        {
            arg = arg.Replace(",", ""); //Removes , if they exists
            arg = arg.ToLower();
            return this.validReferences.Contains(arg);
        }

        //Check if an string representes a plain number
        public bool isPlainNumber(string arg)
        {
            arg = arg.Replace(",", ""); //Removes ,
            //If starts with # 
            if (arg.StartsWith("#"))
            { //And the rest represents a number
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
                if (isNumeric && n % 4 == 0 && n < this.target.memorySize*4)
                {
                    return true;
                }
            }
            return false;
        }


        //Gets the register number from a string
        private int getRegisterNumber(string s) {
            s = s.ToLower();
            s = s.Replace(",", "");
            s = s.Replace("r", "");
            int n;
            bool isNumeric = int.TryParse(s, out n);
            if (isNumeric) {
                return n;
            }
            return -1;
        }

        //Gets memory value from string
        private int getValueNumber(string s) {
            s = s.Replace(",", ""); //Removes ,
            //If starts with # 
            if (s.StartsWith("#"))
            { //And the rest represents a number
                s = s.Remove(0, 1);
                int n;
                bool isNumeric = int.TryParse(s, out n);
                if (isNumeric && n % 4 == 0 && n < this.target.memorySize * 4)
                {
                    return n;
                }
            }
            return -1;
        }

        private int getRawValueNumber(string s) {
            s = s.Replace(",", ""); //Removes ,
            //If starts with # 
            if (s.StartsWith("#"))
            { //And the rest represents a number
                s = s.Remove(0, 1);
                int n;
                bool isNumeric = int.TryParse(s, out n);
                if (isNumeric)
                {
                    return n;
                }
            }
            return -1;
        }

        private int GetReferenceValueNumber(string s) {
            s = s.ToLower();
            s = s.Replace(",", ""); //Removes ,
            s = s.Replace("[", ""); //Removes ]
            s = s.Replace("]", ""); //Removes [
            s = s.Replace("r", ""); //Removes r
            int n;
            bool isNumeric = int.TryParse(s, out n);
            if (isNumeric)
            {
                return n;
            }
            return -1;
        }

    }
}
