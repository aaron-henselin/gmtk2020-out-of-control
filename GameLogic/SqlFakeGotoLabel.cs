using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using gmtk2020_blazor.Models.Cpu;

namespace GameLogic
{
    public class SqlFakeGotoLabel : Scenario
    {
        public SqlFakeGotoLabel(ScenarioPackage package) : base(package)
        {
        }

        public override async Task Initialize()
        {
            await  base.Initialize();

            var Memory = new AddressableRegion
            {
                SizeRows = 3,
                SizeColumns = 4,
            };
            Memory.InitializeEmptyMemorySpace();

            #region disk
            var disk0 = new AddressableRegion();
            disk0.VolumeName = "SWAP_DRIVE";
            disk0.ReadOnly = false;
            disk0.DriveId = 0;
            disk0.SizeRows = 1;
            disk0.SizeColumns = 4;
            disk0.InitializeEmptyMemorySpace();
            disk0.SetMemoryToDefault();
            Disks.Add(disk0);
            #endregion disk

            #region disk
            var disk1 = new AddressableRegion();
            disk1.VolumeName = "Keystore";
            disk1.ReadOnly = true;
            disk1.DriveId = 1;
            disk1.SizeRows = 1;
            disk1.SizeColumns = 4;
            disk1.InitializeEmptyMemorySpace();
            disk1.SetDefault(new MemoryCoordinate { DriveId = 1, X = 0, Y = 0 }, "0451");
            disk1.SetDefault(new MemoryCoordinate { DriveId = 1, X = 1, Y = 0 }, "0451");
            disk1.SetDefault(new MemoryCoordinate { DriveId = 1, X = 2, Y = 0 }, "PASS");
            disk1.SetDefault(new MemoryCoordinate { DriveId = 1, X = 3, Y = 0 }, "0451");

            disk1.EncryptionState[new MemoryCoordinate { X = 0, Y = 0, DriveId = 1 }] = true;
            disk1.EncryptionState[new MemoryCoordinate { X = 1, Y = 0, DriveId = 1 }] = true;
            disk1.EncryptionState[new MemoryCoordinate { X = 2, Y = 0, DriveId = 1 }] = false;
            disk1.EncryptionState[new MemoryCoordinate { X = 3, Y = 0, DriveId = 1 }] = true;
            disk1.SetMemoryToDefault();
            Disks.Add(disk1);
            #endregion

            #region disk

            //var disk1 = new AddressableRegion
            //{
            //    VolumeName = "SWAP_DRIVE",
            //    ReadOnly = true,
            //    DriveId = 1,
            //    SizeRows = 1,
            //    SizeColumns = 4,
            //    IsExternalDrive = true,
            //    IsMounted = false
            //};
            //disk1.InitializeEmptyMemorySpace();
            //disk1.SetDefault(new MemoryCoordinate { DriveId = 1, X = 0, Y = 0 }, "YOUR");
            //disk1.SetDefault(new MemoryCoordinate { DriveId = 1, X = 1, Y = 0 }, "DATA");
            //disk1.SetDefault(new MemoryCoordinate { DriveId = 1, X = 2, Y = 0 }, "HERE");
            //disk1.SetMemoryToDefault();
            //Disks.Add(disk1);

            #endregion

            Memory.SetMemoryToDefault();

            //
            // 


            var ManualProgram = new CpuProgram();
            ManualProgram.AllCommands = new List<CpuCommand>();
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KBD 0:0A"));
            ManualProgram.AllCommands.Add(QueryCpuCommand.FromText("QUERY *:** FOR `PASS"));
            ManualProgram.AllCommands.Add(new SeekCpuCommand{Target=new VariableTarget{Number = "Index"},Amount = new LiteralTarget{Value = "1"}});

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT @Index M:0B"));
            ManualProgram.AllCommands.Add(new AssertCpuCommand("0:0A", "XM:0B"));

            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KB M:2C"));

            //ManualProgram.AllCommands.Add(new AssertCpuCommand
            //{
            //    CompareLeft = MediaTarget.FromText("M:2C"),
            //    CompareRight = MediaTarget.FromText("0:0,0")
            //});

            AddProcess("Login.exe", new Process
            {
                Memory = Memory,
                Source = ManualProgram,
                Prompt = "Please enter your password",
                Instruction = "^ Can't remember your password? Try 'Password'."

            });

            this.Hints.Add(new Hint
            {
                Title = "What am I looking at?",
                Body = "In this scenario, some memory addresses are prefixed with X. This indicates that the value in memory at that location should be treated as a MEMORY ADDRESS to go fetch the value from."
            });

            this.Hints.Add(new Hint
            {
                Title = "The security is too tight! There's no way for me to get in and break this thing.",
                Body = "Yea, it'd be better if you just had the password."
            });

            this.Hints.Add(new Hint
            {
                Title = "The password's in encrypted memory, I can't see it.",
                Body = "Find a way to get the password OUT of encrypted memory."
            });
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