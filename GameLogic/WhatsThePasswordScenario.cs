using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GameLogic
{
    public class WhatsThePasswordScenario : Scenario
    {
        public WhatsThePasswordScenario(HttpClient client) : base(client, "2_whats_the_password")
        {

        }

        public override async Task Initialize()
        {
            await base.Initialize();

            NextScenario = "Scenario3";

//var Memory = new AddressableRegion
//            {
//                SizeRows = 3,
//                SizeColumns = 4,
//            };
//            Memory.InitializeEmptyMemorySpace();

            
//            Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 1 }, "NOTH");
//            Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 1 }, "ING ");
//            Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 1 }, "TO  ");

//            Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 2 }, "SEE ");
//            Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 2 }, "HERE");


//            Memory.SetDefault(new MemoryCoordinate { X = 3, Y = 2 }, "0x51");
//            Memory.EncryptionState[new MemoryCoordinate {X = 3, Y = 2}] = true;

//            Memory.SetMemoryToDefault();


            //var ManualProgram = new CpuProgram();
            //ManualProgram.AllCommands = new List<CpuCommand>();
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KB M:0,0"));

            //ManualProgram.AllCommands.Add(new AssertCpuCommand("M:0,0", "M:3,2"));

            //AddProcess("Login.exe", new Process { Memory = Memory, Source = ManualProgram,
            //    Prompt = "Please enter your pin number.",
            //    Instruction = "^ Can't remember your pin number? Try '1234'. That's what I use for my luggage."
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