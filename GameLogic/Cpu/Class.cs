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
            public Dictionary<string,string> Variables { get; set; } = new Dictionary<string, string>();

            public Scenario Scenario { get; set; }
        }



        public class CpuProgram
        {
            public List<CpuCommand> AllCommands { get; set; } = new List<CpuCommand>();
            public void RunNextStep(Process process, int CurrentStep, CpuCommandContext context)
            {
                
               
                process.Memory.CoolOff();
                foreach (var drive in context.Scenario.Disks)
                    drive.CoolOff();

                var command = AllCommands[CurrentStep];
                process.CpuLog.Log(CurrentStep + ": "+ command.ToString());

                command.Run(process,context);
               

            }

        }

    public abstract class CpuCommand
    {
        public abstract void Run(Process scenario, CpuCommandContext context);
    }

    public class AssertCpuCommand : CpuCommand
    {
        public Target CompareLeft { get; set; }

        public Target CompareRight { get; set; }

        public override string ToString()
        {
            return $"ASSERT {CompareLeft} {CompareRight} PRINT";
        }

        public override void Run(Process scenario, CpuCommandContext context)
        {
            

            var valueLeft = CompareLeft.ReadFromTarget(scenario,context);
            var valueRight = CompareRight.ReadFromTarget(scenario, context);
            var areEqual = string.Equals(valueLeft, valueRight);
            if (areEqual)
                context.Scenario.Printer.Append("ACCESS GRANTED|");
            else
                context.Scenario.Printer.Append("ACCESS DENIED|");
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
        public abstract void WriteToTarget(Process scenario, string input, CpuCommandContext context);
        public abstract string ReadFromTarget(Process scenario, CpuCommandContext context);

        public static Target ResolveTarget(string from)
        {
            if (from.StartsWith("M") || Char.IsDigit(from[0]))
                return MediaTarget.FromText(from);

            if (from.StartsWith("@"))
                return VariableTarget.FromText(from);

            if (from.StartsWith("KB"))
                return KeyBoardTarget.FromText(from);

            if (from.StartsWith("PRINT"))
                return PrinterTarget.FromText(from);

            if (from.StartsWith("X"))
                return DeRefTarget.FromText(from);

            if (from.StartsWith("`"))
                return LiteralTarget.FromText(from);

            throw new ArgumentException(from);
        }
    }

    public class LiteralTarget : Target
    {
        public string Value { get; set; }

        public override void WriteToTarget(Process scenario, string input, CpuCommandContext context)
        {
            throw new NotImplementedException();
        }

        public override string ReadFromTarget(Process scenario, CpuCommandContext context)
        {
            return Value;
        }

        internal static LiteralTarget FromText(string from)
        {
            return new LiteralTarget
            {
                Value = from.Substring(1)
            };

        }

        public override string ToString()
        {
            return "`" + Value;
        }
    }

    public class MediaTarget :Target
    {
        public MemoryCoordinate Coordinate { get; set; }

        public override string ReadFromTarget(Process scenario, CpuCommandContext context)
        {
            string fromValue;
            if (Coordinate.DriveId != null)
            {
                var drive = context.Scenario.Disks[Coordinate.DriveId.Value];
                fromValue = drive.Read(Coordinate);
            }
            else
            {
                var memory = scenario.Memory;
                fromValue = memory.Read(Coordinate);
            }

            return fromValue;
        }

        public override void WriteToTarget(Process scenario, string input, CpuCommandContext context)
        {
            input = Pad4(input);

            AddressableRegion region;
            if (Coordinate.DriveId != null)
            {
                region = context.Scenario.Disks[Coordinate.DriveId.Value];
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

        public override string ReadFromTarget(Process scenario, CpuCommandContext context)
        {
            var reference = ReferenceLocation.ReadFromTarget(scenario, context);
            Console.WriteLine("DeRef Raw: "+reference);

            var actualTarget = Target.ResolveTarget(reference);
            Console.WriteLine("DeRef Parsed: " + actualTarget);

            var actualValue = actualTarget.ReadFromTarget(scenario, context);
            Console.WriteLine("DeRef Actual: " + actualValue);

            return actualValue;
        }

        public override void WriteToTarget(Process scenario, string input, CpuCommandContext context)
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
        public string Number { get; set; }

        public override string ReadFromTarget(Process scenario, CpuCommandContext context)
        {
            if (!context.Variables.ContainsKey(Number))
                return string.Empty;

            return context.Variables[Number];
        }

        public override void WriteToTarget(Process scenario, string input, CpuCommandContext context)
        {
            context.Variables[Number] = input;
        }

        public override string ToString()
        {
            return "@" + Number;
        }

        internal static VariableTarget FromText(string from)
        {
            //var variableTarget = new VariableTarget();
            //var parts = from.Split(new[] {':'});
            //variableTarget.Number = parts[1];
            //return variableTarget;
            return new VariableTarget {Number = from.Substring(1)};
        }
    }

    public class KeyBoardTarget : Target
    {
        public override string ReadFromTarget(Process scenario, CpuCommandContext context)
        {
            return context.Scenario.KeyboardInput.Text ?? string.Empty;
        }

        public override void WriteToTarget(Process scenario, string input, CpuCommandContext context)
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

        public override string ReadFromTarget(Process scenario, CpuCommandContext context)
        {
            throw new NotImplementedException();
        }

        public override void WriteToTarget(Process scenario, string input, CpuCommandContext context)
        {
            context.Scenario.Printer.Append(input);
        }

        internal static PrinterTarget FromText(string from)
        {
            return new PrinterTarget();
        }
    }

    public class ExecCpuCommand : CpuCommand
    {
        public SearchConstraints SearchConstraints { get; set; }

        public override string ToString()
        {
            return $"EXEC {SearchConstraints}";
        }

        public override void Run(Process scenario, CpuCommandContext context)
        {
            var disks = context.Scenario.FindDisks(SearchConstraints);

            var coordList =
            disks.SelectMany(x => x.FilterCoordinates(SearchConstraints))
                .OrderBy(x => x.DriveId)
                .ThenBy(x => x.Y)
                .ThenBy(x => x.X);

            string fullText = string.Empty;
            foreach (var coord in coordList)
            {
                var drive = context.Scenario.Disks[coord.DriveId.Value];
                var readValue = drive.Read(coord);
                fullText += readValue;
            }

            var dynamicQuery = QueryCpuCommand.FromText(fullText);
            scenario.CpuLog.Log("~:" + dynamicQuery.ToString());
            dynamicQuery.Run(scenario,context);
        }

        public static ExecCpuCommand FromText(string text)
        {
            var parts = text.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);

            var constraints = GameLogic.SearchConstraints.ResolveTarget(parts[1]);

            return new ExecCpuCommand
            {
                SearchConstraints = constraints
            };

        }
    }

    public class QueryCpuCommand : CpuCommand
    {
        public SearchConstraints SearchConstraints { get; set; }
        public Target For { get; set; }

        public const string OutputVariableName = "@Index";

        public override string ToString()
        {
            return $"QUERY {SearchConstraints} FOR {For}";
        }

        public override void Run(Process scenario, CpuCommandContext context)
        {
            var searchValue = For.ReadFromTarget(scenario, context);

            var disks = context.Scenario.FindDisks(SearchConstraints);
            foreach (var disk in disks)
            {
                var found = disk.Find(SearchConstraints, searchValue);
                if (found == null)
                    continue;

                context.Variables["Index"] = found.Value.ToString();
                return;
            }

            
        }

        public static QueryCpuCommand FromText(string text)
        {
            var forAt = text.LastIndexOf("FOR");
            var constraintsRaw = text.Substring(6, forAt - 6).Trim();
            var forTargetRaw = text.Substring(forAt + 4).Trim();

            var constraints = GameLogic.SearchConstraints.ResolveTarget(constraintsRaw);
            var forResult = Target.ResolveTarget(forTargetRaw);

            return new QueryCpuCommand
            {
                For = forResult,
                SearchConstraints = constraints
            };
            
        }

    }

    public class ReadCpuCommand :CpuCommand
    {
        public Target From { get; set; }
        public Target To { get; set; }

        public override string ToString()
        {
            return "PUT " + From.ToString() + " " + To.ToString();
        }

        public override void Run(Process scenario, CpuCommandContext context)
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
