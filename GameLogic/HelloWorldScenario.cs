using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GameLogic
{
    public class HelloWorldScenario : Scenario
    {
        
        public HelloWorldScenario(ScenarioPackage package) : base(package)
        {

        }

        public async Task Initialize()
        {
            NextScenario = "Scenario2";

            //await base.Initialize();

            //var Memory = new AddressableRegion
            //{
            //    SizeRows = 3,
            //    SizeColumns = 4,
            //};
            //Memory.InitializeEmptyMemorySpace();

            //Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 1 }, "HELL");
            //Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 1 }, "O, WO");
            //Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 1 }, "RLD|");

            //Memory.SetMemoryToDefault();

            //var ManualProgram = new CpuProgram();
            //ManualProgram.AllCommands = new List<CpuCommand>();
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KB M:0,0"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0,1 PRINT"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:1,1 PRINT"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:2,1 PRINT"));

            //this.Hints.Add(new Hint
            //{
            //    Title = "What am I looking at?",
            //    Body = "This program places your number input into memory using the PUT instruction. It then PUTs 'hello world', which is stored at a later address, to your display output. You cannot modify the CPU instructions, so you must find another way to alter the control flow of the program.",
            //    InterfaceHelpLink = true

            //});

            //this.Hints.Add(new Hint
            //{
            //    Title = "BUG REPORT 001",
            //    Body = "Did you try entering 11? Doesn't seem like it's actually restricting numbers to 1-10."
            //});

            //this.Hints.Add(new Hint
            //{
            //    Title = "BUG REPORT 002",
            //    Body = "Addendum to report #1, it looks like the field is not restricted to numbers. I was able to enter the word 'HOTDOG'"
            //});

            //AddProcess("hello_word.exe", new Process
            //{
            //    Memory = Memory, 
            //    Source = ManualProgram,
            //    Prompt = "What's your favorite number between 1 and 10?",
            //    Instruction = "^ Please answer truthfully. This program knows when you're lying."

            //});

        }

        public override bool IsAtWinCondition()
        {
            var winningLines = Printer.TextLines
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .FirstOrDefault(x => "HELLO  WORLD" != x);

            if (winningLines != null)
                Console.WriteLine("Winning line: "+winningLines);
            return winningLines != null;
        }
    }
}