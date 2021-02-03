using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GameLogic
{
    public class LateralThinkingScenario : Scenario
    {
        public LateralThinkingScenario(ScenarioPackage package) : base(package)
        {
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            NextScenario = "Scenario4";

            var process = new Process
            {
                Prompt = "Please enter your 4 character pin number to manage the camera feed.",
                Instruction = "^ Note: pin number 'HOTDOG' has been disabled due to it not being 4 characters, or a number."
            };

            var Memory = new AddressableRegion
            {
                SizeRows = 3,
                SizeColumns = 4,
            };
            Memory.InitializeEmptyMemorySpace();

            #region disk
            var disk0 = new AddressableRegion();
            disk0.VolumeName = "Keystore";
            disk0.DriveId = 0;
            disk0.SizeRows = 1;
            disk0.SizeColumns = 4;
            disk0.InitializeEmptyMemorySpace();
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 0 }, "0x1x");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 0, DriveId = 0 }] = true;
            disk0.SetMemoryToDefault();
            Disks.Add(disk0);
            #endregion

            //Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 0}, "VALI");
            //Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 0 }, "DATI");
            //Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 0 }, "NG  ");

            //Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 1 }, "PASS");
            //Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 1 }, "WORD");
            //Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 1 }, "|");

            //Memory.SetDefault(new MemoryCoordinate { X = 3, Y = 2 }, "0x51");
            //Memory.EncryptionState[new MemoryCoordinate { X = 3, Y = 2 }] = true;

            Memory.SetMemoryToDefault();
            process.Memory = Memory;

            
            //var ManualProgram = new CpuProgram();
            //ManualProgram.AllCommands = new List<CpuCommand>();

            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0A PRINT"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:1A PRINT"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:2A PRINT"));

            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0B PRINT"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:1B PRINT"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:2B PRINT"));

            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KB M:0C"));



            //ManualProgram.AllCommands.Add(new AssertCpuCommand("M:0C", "0:0,0"));

            process.Source = ManualProgram;
            this.AddProcess("Login.exe",process);


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