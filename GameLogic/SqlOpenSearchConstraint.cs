﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using gmtk2020_blazor.Models.Cpu;

namespace GameLogic
{
    public class SqlOpenSearchConstraint : Scenario
    {
        public SqlOpenSearchConstraint(ScenarioPackage package) : base(package)
        {
        }

        public async Task Initialize()
        {
            //await base.Initialize();

            var Memory = new AddressableRegion
            {
                SizeRows = 3,
                SizeColumns = 4,
            };
            Memory.InitializeEmptyMemorySpace();



            #region disk
            var disk0 = new AddressableRegion();
            disk0.VolumeName = "Keystore";
            disk0.ReadOnly = true;
            disk0.DriveId = 0;
            disk0.SizeRows = 4;
            disk0.SizeColumns = 4;
            disk0.InitializeEmptyMemorySpace();
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 0 }, "~451");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 0, DriveId = 0 }] = true;

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 0 }, "~451");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 1, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 1, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 1, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 1, DriveId = 0 }] = true;

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 0 }, "~451");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 2, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 2, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 2, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 2, DriveId = 0 }] = true;

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 3 }, "/*YOU");
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 1, Y = 3 }, "R KE");
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 2, Y = 3 }, "Y HE");
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 3, Y = 3 }, "RE*/");

            disk0.SetMemoryToDefault();
            Disks.Add(disk0);
            #endregion


            Memory.SetMemoryToDefault();

            var ManualProgram = new CpuProgram();
            ManualProgram.AllCommands = new List<CpuCommand>();

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KBD M:0A"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0A @SEARCH_TEXT"));
            ManualProgram.AllCommands.Add(QueryCpuCommand.FromText("QUERY 0:3* FOR @SEARCH_TEXT"));

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT @Index M:0B"));
            ManualProgram.AllCommands.Add(new AssertCpuCommand("M:0A", "XM:0B"));


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