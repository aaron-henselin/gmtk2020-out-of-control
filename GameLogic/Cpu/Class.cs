using GameLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace gmtk2020_blazor.Models.Cpu
{
            
        public class CpuCommandContext
        {
            public Dictionary<int,string> Variables { get; set; } = new Dictionary<int, string>();
        }



        public class CpuProgram
        {
            public List<CpuCommand> AllCommands { get; set; } = new List<CpuCommand>();
            public void RunNextStep(GameLogic.Scenario scenario, int CurrentStep, CpuCommandContext context)
            {
                
               
                scenario.Memory.CoolOff();
                foreach (var drive in scenario.Disks)
                    drive.CoolOff();

                var command = AllCommands[CurrentStep];
                scenario.CpuLog.Log(CurrentStep + ": "+ command.ToString());

                command.Run(scenario,context);
               

            }
        }

    public abstract class CpuCommand
    {
        public abstract void Run(Scenario scenario, CpuCommandContext context);
    }

    public class AssertCpuCommand : CpuCommand
    {
        public Target CompareLeft { get; set; }

        public Target CompareRight { get; set; }

        public override string ToString()
        {
            return $"ASSERT {CompareLeft} {CompareRight} PRINT";
        }

        public override void Run(Scenario scenario, CpuCommandContext context)
        {
            

            var valueLeft = CompareLeft.ReadFromTarget(scenario,context);
            var valueRight = CompareRight.ReadFromTarget(scenario, context);
            var areEqual = string.Equals(valueLeft, valueRight);
            if (areEqual)
                scenario.Printer.Append("ACCESS GRANTED|");
            else
                scenario.Printer.Append("ACCESS DENIED|");
        }
    }

    //public class PrintCpuCommand : CpuCommand
    //{
    //    public MemoryCoordinate From { get; set; }

    //    public override string ToString()
    //    {
    //        return $"PUT {From} PRINT";
    //    }

    //    public override void Run(Scenario scenario)
    //    {
    //        AddressableRegion region;
    //        if (From.DriveId != null)
    //        {
    //            region = scenario.Disks[From.DriveId.Value];
    //        }
    //        else
    //        {
    //            region = scenario.Memory;
    //        }

    //        var toPrint = region.Read(From);
    //        scenario.Printer.Append(toPrint);
    //    }
    //}

    public abstract class Target
    {
        public abstract void WriteToTarget(Scenario scenario, string input, CpuCommandContext context);
        public abstract string ReadFromTarget(Scenario scenario, CpuCommandContext context);

        public static Target ResolveTarget(string from)
        {
            if (from.StartsWith("M") || Char.IsDigit(from[0]))
                return MediaTarget.FromText(from);

            if (from.StartsWith("V"))
                return VariableTarget.FromText(from);

            if (from.StartsWith("KB"))
                return KeyBoardTarget.FromText(from);

            if (from.StartsWith("PRINT"))
                return PrinterTarget.FromText(from);

            if (from.StartsWith("X"))
                return DeRefTarget.FromText(from);

            throw new ArgumentException(from);
        }
    }

    public class MediaTarget :Target
    {
        public MemoryCoordinate Coordinate { get; set; }

        public override string ReadFromTarget(Scenario scenario, CpuCommandContext context)
        {
            string fromValue;
            if (Coordinate.DriveId != null)
            {
                var drive = scenario.Disks[Coordinate.DriveId.Value];
                fromValue = drive.Read(Coordinate);
            }
            else
            {
                var memory = scenario.Memory;
                fromValue = memory.Read(Coordinate);
            }

            return fromValue;
        }

        public override void WriteToTarget(Scenario scenario, string input, CpuCommandContext context)
        {
            input = Pad4(input);

            AddressableRegion region;
            if (Coordinate.DriveId != null)
            {
                region = scenario.Disks[Coordinate.DriveId.Value];
            }
            else
            {
                region = scenario.Memory;
            }

            var chunks = Split(input, 4).ToList();
            MemoryCoordinate? lastPut = null;
            for (int i = 0; i < chunks.Count; i++)
            {

                if (lastPut == null)
                {
                    region.Write(Coordinate, chunks[i]);
                    lastPut = Coordinate;
                }
                else
                {
                    var put = region.NextAddress(lastPut.Value);
                    region.Write(put, chunks[i]);
                    lastPut = put;
                }

            }
        }

        static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        public static string Pad4(string input)
        {
            var isLength4 = input.Length % 4 == 0;
            if (!isLength4)
            {
                var newLength = ((input.Length / 4) + 1) * 4;
                return input.PadRight(newLength, ' ');
            }

            return input;
        }

        public override string ToString()
        {
            return Coordinate.ToString();
        }

        internal static MediaTarget FromText(string from)
        {
            var mediaTarget = new MediaTarget();
            mediaTarget.Coordinate = MemoryCoordinate.FromText(from);
            return mediaTarget;
        }
    }

    public class DeRefTarget : Target
    {
        public Target ReferenceLocation { get; set; }

        public override string ReadFromTarget(Scenario scenario, CpuCommandContext context)
        {
            var reference = ReferenceLocation.ReadFromTarget(scenario, context);
            Console.WriteLine("DeRef Raw: "+reference);

            var actualTarget = Target.ResolveTarget(reference);
            Console.WriteLine("DeRef Parsed: " + actualTarget);

            var actualValue = actualTarget.ReadFromTarget(scenario, context);
            Console.WriteLine("DeRef Actual: " + actualValue);

            return actualValue;
        }

        public override void WriteToTarget(Scenario scenario, string input, CpuCommandContext context)
        {
            var reference = ReferenceLocation.ReadFromTarget(scenario, context);
            var actualTarget = Target.ResolveTarget(reference);
            actualTarget.WriteToTarget(scenario, input,context);
        }

        internal static DeRefTarget FromText(string from)
        {
            return new DeRefTarget
            {
                ReferenceLocation = Target.ResolveTarget(from.Substring(1))
            };

        }

        public override string ToString()
        {
            return "X" + ReferenceLocation.ToString();
        }
    }

    public class VariableTarget :Target
    {
        public int Number { get; set; }

        public override string ReadFromTarget(Scenario scenario, CpuCommandContext context)
        {
            if (!context.Variables.ContainsKey(Number))
                return string.Empty;

            return context.Variables[Number];
        }

        public override void WriteToTarget(Scenario scenario, string input, CpuCommandContext context)
        {
            if (Number > 9999)
                Number = 0;

            context.Variables[Number] = input;
        }

        public override string ToString()
        {
            return "V:" + Number;
        }

        internal static VariableTarget FromText(string from)
        {
            var variableTarget = new VariableTarget();
            var parts = from.Split(new[] {':'});
            variableTarget.Number = Convert.ToInt32(parts[1]);
            return variableTarget;
        }
    }

    public class KeyBoardTarget : Target
    {
        public override string ReadFromTarget(Scenario scenario, CpuCommandContext context)
        {
            return scenario.KeyboardInput.Text ?? string.Empty;
        }

        public override void WriteToTarget(Scenario scenario, string input, CpuCommandContext context)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "KB";
        }

        internal static KeyBoardTarget FromText(string from)
        {
            return new KeyBoardTarget();
        }
    }

    public class PrinterTarget : Target
    {
        public override string ToString()
        {
            return "PRINT";
        }

        public override string ReadFromTarget(Scenario scenario, CpuCommandContext context)
        {
            throw new NotImplementedException();
        }

        public override void WriteToTarget(Scenario scenario, string input, CpuCommandContext context)
        {
            scenario.Printer.Append(input);
        }

        internal static PrinterTarget FromText(string from)
        {
            return new PrinterTarget();
        }
    }

    //public class KeyboardCpuCommand : CpuCommand
    //{
    //    public Target To { get; set; }

    //    public override string ToString()
    //    {
    //        return "PUT KB " + To.ToString();
    //    }


    //    public override void Run(Scenario scenario)
    //    {
    //        Console.WriteLine(scenario.KeyboardInput.Text);
    //        To.WriteToTarget(scenario,scenario.KeyboardInput.Text);

    //    }
    //}


    public class ReadCpuCommand :CpuCommand
    {
        public Target From { get; set; }
        public Target To { get; set; }

        public override string ToString()
        {
            return "PUT " + From.ToString() + " " + To.ToString();
        }

        public override void Run(Scenario scenario, CpuCommandContext context)
        {
            var value = From.ReadFromTarget(scenario,context);
            To.WriteToTarget(scenario,value,context);
        }

        public static ReadCpuCommand FromText(string text)
        {
           

            var substring = text.Substring(4);
            var parts = substring.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries);
            var from = Target.ResolveTarget(parts[0]);
            var to = Target.ResolveTarget(parts[1]);

            return new ReadCpuCommand
            {
                From = from,
                To = to
            };
        }


    }

}
