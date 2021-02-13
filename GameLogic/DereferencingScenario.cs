using System;
using System.Linq;
using System.Threading.Tasks;

namespace GameLogic
{
    public class DereferencingScenario : Scenario
    {
        public DereferencingScenario(ScenarioPackage package) : base(package)
        {

        }

        public async Task Initialize()
        {
            

            var Memory = new AddressableRegion
            {
                SizeRows = 3,
                SizeColumns = 4,
            };
            Memory.InitializeEmptyMemorySpace();

            //#region disk
            //var disk0 = new AddressableRegion();
            //disk0.VolumeName = "Keystore";
            //disk0.ReadOnly = true;
            //disk0.DriveId = 0;
            //disk0.SizeRows = 1;
            //disk0.SizeColumns = 4;
            //disk0.InitializeEmptyMemorySpace();
            //disk0.SetDefault(new MemoryCoordinate{DriveId = 0,X=0,Y=0},"0451");
            //disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 0,DriveId = 0}] = true;
            //disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 0, DriveId = 0 }] = true;
            //disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 0, DriveId = 0 }] = true;
            //disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 0, DriveId = 0 }] = true;
            //disk0.SetMemoryToDefault();
            //Disks.Add(disk0);
            //#endregion

            //#region disk
            //var disk1 = new AddressableRegion();
            //disk1.VolumeName = "Messages";
            //disk1.ReadOnly = true;
            //disk1.DriveId = 1;
            //disk1.SizeRows = 1;
            //disk1.SizeColumns = 4;
            //disk1.InitializeEmptyMemorySpace();
            //disk1.SetDefault(new MemoryCoordinate { DriveId = 1, X = 0, Y = 0 }, "PLEA");
            //disk1.SetDefault(new MemoryCoordinate { DriveId = 1, X = 1, Y = 0 }, "SE W");
            //disk1.SetDefault(new MemoryCoordinate { DriveId = 1, X = 2, Y = 0 }, "AIT|");
            //disk1.SetMemoryToDefault();
            //Disks.Add(disk1);

            //var disk2 = new AddressableRegion();
            //disk2.VolumeName = "Messages";
            //disk2.DriveId = 1;
            //disk2.SizeRows = 1;
            //disk2.SizeColumns = 4;
            //disk2.IsExternalDrive = true;
            //disk2.InitializeEmptyMemorySpace();
            //disk2.SetDefault(new MemoryCoordinate { DriveId = 1, X = 0, Y = 0 }, "PLEA");
            //disk2.SetDefault(new MemoryCoordinate { DriveId = 1, X = 1, Y = 0 }, "SE W");
            //disk2.SetDefault(new MemoryCoordinate { DriveId = 1, X = 2, Y = 0 }, "AIT|");
            //disk2.SetMemoryToDefault();
            //Disks.Add(disk2);
            //#endregion


            //Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 0 }, "MSG");
            //Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 0 }, "1:0A");
            //Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 0 }, "1:1A");
            //Memory.SetDefault(new MemoryCoordinate { X = 3, Y = 0 }, "1:2A");

            //Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 1 }, "NOTH");
            //Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 1 }, "ING ");
            //Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 1 }, "TO  ");

            //Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 2 }, "SEE ");
            //Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 2 }, "HERE");


            ////Memory.SetDefault(new MemoryCoordinate { X = 3, Y = 2 }, "0x51");
            ////Memory.EncryptionState[new MemoryCoordinate { X = 3, Y = 2 }] = true;

            Memory.SetMemoryToDefault();

            

            //var ManualProgram = new CpuProgram();
            //ManualProgram.AllCommands = new List<CpuCommand>();
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT XM:1A PRINT"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT XM:2A PRINT"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT XM:3A PRINT"));

            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KB M:2C"));

            //ManualProgram.AllCommands.Add(new AssertCpuCommand("M:2C", "0:0,0"));

            //AddProcess("Login.exe", new Process
            //{
            //    Memory = Memory, 
            //    Source = ManualProgram,
            //    Prompt = "Please enter your password",
            //    Instruction = "^ Can't remember your password? Try 'Password'."

            //});

        }

        public override bool IsAtWinCondition()
        {
            var winningLines = Printer.TextLines
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .FirstOrDefault(x => "ACCESS GRANTED" == x);

            if (winningLines != null)
                Console.WriteLine("Winning line: " + winningLines);
            return winningLines != null;
        }
    }
}