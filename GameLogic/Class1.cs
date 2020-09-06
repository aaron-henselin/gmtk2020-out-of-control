using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using gmtk2020_blazor.Models.Cpu;

namespace GameLogic
{


    public struct MemoryCoordinate
    {
        public int? DriveId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public override string ToString()
        {
            var displayY = Convert.ToChar((int) 'A' + Y);

            if (DriveId == null)
                return "M:"+X + "" + displayY;
            else
            {
                return DriveId + ":" + X + "" + displayY;
            }
        }



        public static MemoryCoordinate FromText(string from)
        {
            try
            {
                from = from.Replace(",", "");
                from = from.Replace(" ", "");
                from = from.Replace(":", "");


                //var parts = from.Split(new []{':',',',' '},StringSplitOptions.RemoveEmptyEntries);

                var yInt = 0;
                var yChar = from[2];
                if (char.IsLetter(yChar))
                {
                    yInt = (int)yChar - 'A';
                }
                else
                {
                    yInt = Convert.ToInt32(yChar.ToString());
                }

                var xInt = 0;
                xInt = Convert.ToInt32(from[1].ToString());

                if (from[0] == 'M')
                {
                    return new MemoryCoordinate { X = xInt, Y = yInt };
                }
                else
                {
                    var driveInt = Convert.ToInt32(from[0].ToString());

                    return new MemoryCoordinate
                    {
                        X = xInt,
                        Y = yInt,
                        DriveId = driveInt
                    };
                }

                throw new ArgumentException();
            }
            catch (Exception e)
            {

                throw new ArgumentException("Unable to parse memorycoordinate " + from,e);
            }
            
        }
    }

    public struct MemoryContents
    {
        public string Value { get; set; }
        public MemoryAccessState AccessState { get; set; }
    }

    public enum MemoryAccessState
    {
        Cold, Read,Write
    }

    public class AddressRegionEventArgs : EventArgs
    {
        public MemoryCoordinate? Coordinate { get; set; }
    }

    public class AddressableRegion
    {
        public bool IsExternalDrive { get; set; }
        public bool IsMounted { get; set; }

        public event EventHandler<AddressRegionEventArgs> RegionUpdated;
        public int? DriveId { get; set; }
        public string VolumeName { get; set; }
        public bool ReadOnly { get; set; }
        public int SizeRows { get; set; }
        public int SizeColumns { get; set; }
        public Dictionary<MemoryCoordinate, string> Default { get; set; } = new Dictionary<MemoryCoordinate, string>();
        public Dictionary<MemoryCoordinate, MemoryContents> Current { get; set; } = new Dictionary<MemoryCoordinate, MemoryContents>();
        public Dictionary<MemoryCoordinate, bool> EncryptionState { get; set; } = new Dictionary<MemoryCoordinate, bool>();

        public IEnumerable<MemoryCoordinate> AllCoordinates
        {
            get
            {
                for (int y = 0; y < SizeRows; y++)
                    for (int x = 0; x < SizeColumns; x++)
                        yield return new MemoryCoordinate { X = x, Y = y,DriveId = DriveId };
            }
        }

        public string Read(MemoryCoordinate coord)
        {
            var currentValue = this.Current[coord].Value;
            this.Current[coord] = new MemoryContents
            {
                Value = currentValue,
                AccessState = MemoryAccessState.Read,
            };
            
            RegionUpdated?.Invoke(this, new AddressRegionEventArgs{Coordinate = coord});
            return currentValue;

        }

        public void Write(MemoryCoordinate coord, string value)
        {
            if (ReadOnly)
                throw new ArgumentException("Readonly.");

            this.Current[coord] = new MemoryContents
            {
                AccessState = MemoryAccessState.Write,
                Value = value,
            };

            RegionUpdated?.Invoke(this,new AddressRegionEventArgs { Coordinate = coord });
        }


        public void SetDefault(MemoryCoordinate coord, string contents)
        {
            this.Default[coord] = contents;
        }


        public MemoryCoordinate NextAddress(MemoryCoordinate to)
        {
            MemoryCoordinate next;
            if (to.X == SizeColumns - 1)
            {
                next = new MemoryCoordinate {DriveId = to.DriveId, X = 0, Y = to.Y + 1};
            }
            else
            {
                next = new MemoryCoordinate { DriveId = to.DriveId, X = to.X+1, Y = to.Y };
            }

            if (next.Y >= SizeRows)
                next = new MemoryCoordinate
                {
                    DriveId = next.DriveId,
                    X = 0,
                    Y = 0,
                };

            return next;
        }

        public IDictionary<MemoryCoordinate,string> ToDictionary()
        {
            return this.Current.ToDictionary(x => x.Key, x => x.Value.Value);
        }

        public void SetMemoryToDefault()
        {
            foreach (var coord in AllCoordinates)
                Current[coord] = new MemoryContents {Value= Default[coord]};



            RegionUpdated?.Invoke(this, new AddressRegionEventArgs());
        }

        public void InitializeEmptyMemorySpace()
        {
            foreach (var coord in AllCoordinates)
                Default[coord] = string.Empty;

            foreach (var coord in AllCoordinates)
                EncryptionState[coord] = false;

            RegionUpdated?.Invoke(this, new AddressRegionEventArgs());
        }

        public void CoolOff()
        {
            foreach (var coord in AllCoordinates)
            {
                var contents = Current[coord];
                Current[coord] = new MemoryContents
                {
                    AccessState = MemoryAccessState.Cold,
                    Value = contents.Value,
                };
            }
            RegionUpdated?.Invoke(this, new AddressRegionEventArgs());
        }


        public IEnumerable<MemoryCoordinate> FilterCoordinates(SearchConstraints constraints)
        {
            var allCoordinates = this.AllCoordinates.ToList();
            foreach (var coordinate in allCoordinates)
            {
                var matchCol = !constraints.ColConstraint.HasValue || constraints.ColConstraint.Value == coordinate.X;
                var matchRow= !constraints.RowConstraint.HasValue || constraints.RowConstraint.Value == coordinate.Y;
                if (!matchRow || !matchCol)
                    continue;

                yield return coordinate;
            }
        }

        public MemoryCoordinate? Find(SearchConstraints constraints, IReadOnlyCollection<string> value)
        {
            var paddedValues = value.Select(x =>
            {
                if (x.Length < 4)
                {
                    return x.PadRight(4, ' ');
                }
                else
                    return x;

            }).ToList();



            var coordinates = FilterCoordinates(constraints);
            foreach (var coordinate in coordinates)
            {
                bool matched=true;
                var offset = coordinate;
                foreach (var paddedValue in paddedValues)
                {
                    var valueAtAddress = Read(offset);
                    var areEqual = string.Equals(valueAtAddress,paddedValue,StringComparison.OrdinalIgnoreCase);
                    if (!areEqual)
                    {
                        matched = false;
                        break;
                    }

                    offset = this.NextAddress(coordinate);
                }

                if (matched)
                    return coordinate;

            }

            return null;
        }
    }

    public class SearchConstraints
    {
        public int? DriveConstraint { get; set; }
        public int? RowConstraint { get; set; }
        public int? ColConstraint { get; set; }

        public static SearchConstraints ResolveTarget(string from)
        {
            var expression = new SearchConstraints();

            from = from.Replace(",", "");
            from = from.Replace(" ", "");
            from = from.Replace(":", "");

            var driveConstraintRaw = from[0];
            if ('*' != driveConstraintRaw)
                expression.DriveConstraint = Int32.Parse(driveConstraintRaw.ToString());

            var colConstraintRaw = from[1];
            if ('*' != colConstraintRaw)
                expression.ColConstraint = Int32.Parse(colConstraintRaw.ToString());

            var rowConstraintRaw = from[2];
            if ('*' != rowConstraintRaw)
            {
                int yInt;
                if (char.IsLetter(rowConstraintRaw))
                {
                    yInt = (int)rowConstraintRaw - 'A';
                }
                else
                {
                    yInt = Convert.ToInt32(rowConstraintRaw.ToString());
                }

                expression.RowConstraint = yInt;
            }

            return expression;
        }

        public override string ToString()
        {
            return (DriveConstraint?.ToString() ?? "*") + ":" + (RowConstraint?.ToString() ?? "*") +
                   (ColConstraint?.ToString() ?? "*");
        }
    }

    public class CpuLog
    {
        public event EventHandler<EventArgs> LogChanged; 
        public List<string> entries = new List<string>();
        public void Log(string command)
        {
            while (entries.Count > 10)
                entries.RemoveAt(0);

            entries.Add(command);

            LogChanged?.Invoke(this,new EventArgs());
        }
    }

    public class Hint
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public bool InterfaceHelpLink { get; set; }
    }

    public class Process
    {
        public CpuLog CpuLog { get; set; } = new CpuLog();
        public bool IsBackgroundProcess { get; set; } = false;
        public CpuProgram Source { get; set; }
        public AddressableRegion Memory { get; set; }

        public string Prompt { get; set; }
        public string Instruction { get; set; }
    }

    public abstract class Scenario
    {
        public IReadOnlyCollection<AddressableRegion> FindDisks(SearchConstraints SearchConstraints)
        {
            List<AddressableRegion> disks = new List<AddressableRegion>();
            for (int i = 0; i < Disks.Count; i++)
            {
                var invalidConstraint = SearchConstraints.DriveConstraint != null && SearchConstraints.DriveConstraint != i;
                if (invalidConstraint)
                    continue;

                var region = Disks[i];
                if (region.IsExternalDrive && !region.IsMounted)
                    continue;

                disks.Add(region);

                //var found = region.Find(SearchConstraints, searchValue);
                //if (found == null)
                //    continue;

                //context.Variables["Index"] = found.Value.ToString();
                //return;
            }

            return disks;
        }

        public string NextScenario { get; set; }
        public abstract void Initialize();

        public abstract bool IsAtWinCondition();

        public Dictionary<string,Process> Processes { get; set; }= new Dictionary<string, Process>();

        public void AddProcess(string name,Process process)
        {
            Processes.Add(name,process);
        }


        public List<AddressableRegion> Disks { get; set; } = new List<AddressableRegion>();

        public KeyboardInput KeyboardInput { get; set; } = new KeyboardInput();

        public PrinterOutput Printer { get; set; } = new PrinterOutput();

        public List<Hint> Hints { get; set; } = new List<Hint>();
    }

    public class KeyboardInput
    {
        public string Text { get; set; }
    }

    public class PrinterOutput
    {
        public event EventHandler<EventArgs> PrinterOutputChanged; 

        public List<string> TextLines { get; set; } = new List<string>();

        public void Append(string text)
        {
            Console.WriteLine("PRINTER: "+text);

            if (TextLines.Count == 0)
                NewLine();

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '|')
                    NewLine();
                else
                    TextLines[0] += text[i];
            }

        }


        public void NewLine()
        {
            while (TextLines.Count > 9)
                TextLines.RemoveAt(9);

            TextLines.Insert(0,string.Empty);
            PrinterOutputChanged?.Invoke(this, new EventArgs());
        }

    }

    public class LateralThinkingScenario : Scenario
    {
        public override void Initialize()
        {
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

            Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 0}, "VALI");
            Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 0 }, "DATI");
            Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 0 }, "NG  ");

            Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 1 }, "PASS");
            Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 1 }, "WORD");
            Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 1 }, "|");

            //Memory.SetDefault(new MemoryCoordinate { X = 3, Y = 2 }, "0x51");
            //Memory.EncryptionState[new MemoryCoordinate { X = 3, Y = 2 }] = true;

            Memory.SetMemoryToDefault();
            process.Memory = Memory;

            
            var ManualProgram = new CpuProgram();
            ManualProgram.AllCommands = new List<CpuCommand>();

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0A PRINT"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:1A PRINT"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:2A PRINT"));

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0B PRINT"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:1B PRINT"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:2B PRINT"));

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KB M:0C"));



            ManualProgram.AllCommands.Add(new AssertCpuCommand("M:0C", "0:0,0"));

            process.Source = ManualProgram;
            this.AddProcess("Login.exe",process);

            this.Hints.Add(new Hint
            {
                Title = "I can't modify the memory address I want to!",
                Body = "If you can't go backwards, try going forwards."
            });

            this.Hints.Add(new Hint
            {
                Title = "That didn't help me because even if I could get to the memory, there's no password in memory to read or write to.",
                Body = "Read the win criteria again."
            });

            this.Hints.Add(new Hint
            {
                Title = "Stop with the mind-games already.",
                Body = "Your goal is not to pass an ASSERT. Your goal is to get the text 'ACCESS GRANTED' to output to the display."
            });

            this.Hints.Add(new Hint
            {
                Title = "How do I do that?",
                Body = "The program prints out a helpful progress message to let you know that it's checking your password. Take advantage of it."
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

    public class SqlFakeGotoLabel : Scenario
    {
        public override void Initialize()
        {
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
    public class SqlOpenSearchConstraint : Scenario
    {
        public override void Initialize()
        {
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

    public class SqlReplaceEntireQuery : Scenario
    {
        public override void Initialize()
        {
            var Memory = new AddressableRegion
            {
                SizeRows = 2,
                SizeColumns = 4,
            };
            Memory.InitializeEmptyMemorySpace();



            #region disk
            var disk0 = new AddressableRegion();
            disk0.VolumeName = "Keystore";
            disk0.ReadOnly = true;
            disk0.DriveId = 0;
            disk0.SizeRows = 3;
            disk0.SizeColumns = 4;
            disk0.InitializeEmptyMemorySpace();
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 0 }, "~451");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 0, DriveId = 0 }] = true;

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 1 }, "~451");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 1, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 1, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 1, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 1, DriveId = 0 }] = true;

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 2 }, "~451");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 2, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 2, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 2, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 2, DriveId = 0 }] = true;

            disk0.SetMemoryToDefault();
            Disks.Add(disk0);
            #endregion


            var disk1 = new AddressableRegion
            {
                VolumeName = "QUERY_BUILDER",
                ReadOnly = false,
                DriveId = 1,
                SizeRows = 2,
                SizeColumns = 4
            };
            disk1.InitializeEmptyMemorySpace();
            disk1.SetDefault(MemoryCoordinate.FromText("1:0A"),"QUER" );
            disk1.SetDefault(MemoryCoordinate.FromText("1:1A"), "Y ");
            disk1.SetDefault(MemoryCoordinate.FromText("1:2A"), "0:3*");
            disk1.SetDefault(MemoryCoordinate.FromText("1:3A"), " FOR");
            disk1.SetDefault(MemoryCoordinate.FromText("1:0B"), "   `");
            
            disk1.SetMemoryToDefault();
            Disks.Add(disk1);



            Memory.SetMemoryToDefault();

            var ManualProgram = new CpuProgram();
            
            ManualProgram.AllCommands = new List<CpuCommand>();
            
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KBD 1:1B"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT `QUER 1:0A "));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT `Y 1:1A  "));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT @StartLiteral 1:0B "));

            ManualProgram.AllCommands.Add(ExecCpuCommand.FromText("EXEC 1:**"));

            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0A @SEARCH_TEXT"));
            //ManualProgram.AllCommands.Add(QueryCpuCommand.FromText("QUERY 0:3* FOR @SEARCH_TEXT"));

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT @Index M:0B"));
            ManualProgram.AllCommands.Add(new AssertCpuCommand("1:1B", "XM:0B"));



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

    public class SqlScenario3 : Scenario
    {
        public override void Initialize()
        {
            var Memory = new AddressableRegion
            {
                SizeRows = 2,
                SizeColumns = 4,
            };
            Memory.InitializeEmptyMemorySpace();



            #region disk
            var disk0 = new AddressableRegion();
            disk0.VolumeName = "Keystore";
            disk0.ReadOnly = true;
            disk0.DriveId = 0;
            disk0.SizeRows = 3;
            disk0.SizeColumns = 4;
            disk0.InitializeEmptyMemorySpace();
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 0 }, "~451");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 0, DriveId = 0 }] = false;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 0, DriveId = 0 }] = true;

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 0 }, "~451");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 1, DriveId = 0 }] = false;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 1, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 1, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 1, DriveId = 0 }] = true;

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 0 }, "~451");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 2, DriveId = 0 }] = false;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 2, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 2, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 2, DriveId = 0 }] = true;

            disk0.SetMemoryToDefault();
            Disks.Add(disk0);
            #endregion


            var disk1 = new AddressableRegion
            {
                VolumeName = "QUERY_BUILDER",
                ReadOnly = false,
                DriveId = 1,
                SizeRows = 2,
                SizeColumns = 4
            };
            disk1.InitializeEmptyMemorySpace();
            disk1.SetDefault(MemoryCoordinate.FromText("1:0A"), "QUER");
            disk1.SetDefault(MemoryCoordinate.FromText("1:1A"), "Y ");
            disk1.SetDefault(MemoryCoordinate.FromText("1:2A"), "0:3*");
            disk1.SetDefault(MemoryCoordinate.FromText("1:3A"), " FOR");
            disk1.SetDefault(MemoryCoordinate.FromText("1:0B"), "   `");

            disk1.SetMemoryToDefault();
            Disks.Add(disk1);



            Memory.SetMemoryToDefault();

            var ManualProgram = new CpuProgram();
            ManualProgram.AllCommands = new List<CpuCommand>();

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KBD 1:1B"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT `QUER 1:0A "));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT `Y 1:1A  "));

            ManualProgram.AllCommands.Add(ExecCpuCommand.FromText("EXEC 1:**"));

            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0A @SEARCH_TEXT"));
            //ManualProgram.AllCommands.Add(QueryCpuCommand.FromText("QUERY 0:3* FOR @SEARCH_TEXT"));

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

    ////what if we put a second instruction in the query, separated by |
    //public class SqlInstructionPacking : Scenario
    //{

    //}

    //public class SqlChangeEscapeCharacterScenario : Scenario
    //{

    //}

    public class ExploitNextAddressCheck : Scenario
    {
        public override void Initialize()
        {
            var Memory = new AddressableRegion
            {
                SizeRows = 2,
                SizeColumns = 4,
            };
            Memory.InitializeEmptyMemorySpace();



            #region disk
            var disk0 = new AddressableRegion();
            disk0.VolumeName = "Keystore";
            disk0.ReadOnly = true;
            disk0.DriveId = 0;
            disk0.SizeRows = 5;
            disk0.SizeColumns = 3;
            disk0.InitializeEmptyMemorySpace();

            //disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 0 }, "USER");
            //disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 1, Y = 0 }, "PASS");
            //disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 2, Y = 0 }, "XXXX");

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 0 }, "USR1");
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 1, Y = 0 }, "0451");
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 2, Y = 0 }, "HIGH");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 0, DriveId = 0 }] = false;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 0, DriveId = 0 }] = true;

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 1 }, "USR2");
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 1, Y = 1 }, "0451");
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 2, Y = 1 }, "HIGH");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 1, DriveId = 0 }] = false;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 1, DriveId = 0 }] = true;

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 2 }, "USR3");
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 1, Y = 2 }, "0451");
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 2, Y = 2 }, "LOW ");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 2, DriveId = 0 }] = false;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 2, DriveId = 0 }] = true;

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 3 }, "USR4");
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 1, Y = 3 }, "0451");
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 2, Y = 3 }, "LOW ");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 3, DriveId = 0 }] = false;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 3, DriveId = 0 }] = true;

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 4 }, "USR5");
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 1, Y = 4 }, "0451");
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 2, Y = 4 }, "LOW ");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 4, DriveId = 0 }] = false;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 4, DriveId = 0 }] = true;

            disk0.SetMemoryToDefault();
            Disks.Add(disk0);
            #endregion


            var disk1 = new AddressableRegion
            {
                VolumeName = "SWAP_DRIVE",
                ReadOnly = false,
                DriveId = 1,
                SizeRows = 2,
                SizeColumns = 4
            };
            disk1.InitializeEmptyMemorySpace();
            disk1.SetDefault(MemoryCoordinate.FromText("1:0A"), "ACCE");
            disk1.SetDefault(MemoryCoordinate.FromText("1:1A"), "SS G");
            disk1.SetDefault(MemoryCoordinate.FromText("1:2A"), "RANT");
            disk1.SetDefault(MemoryCoordinate.FromText("1:3A"), "ED  ");
            disk1.SetDefault(MemoryCoordinate.FromText("1:0B"), "----");
            disk1.SetDefault(MemoryCoordinate.FromText("1:1B"), "    ");
            disk1.SetDefault(MemoryCoordinate.FromText("1:2B"), "SEC:");
            disk1.SetDefault(MemoryCoordinate.FromText("1:3B"), "----");

            disk1.SetMemoryToDefault();
            Disks.Add(disk1);


            var disk2 = new AddressableRegion
            {
                VolumeName = "QUERY_BUILDER",
                ReadOnly = false,
                DriveId = 2,
                SizeRows = 2,
                SizeColumns = 4
            };
            disk2.InitializeEmptyMemorySpace();
            disk2.SetDefault(MemoryCoordinate.FromText("2:0A"), "QUER");
            disk2.SetDefault(MemoryCoordinate.FromText("2:1A"), "Y ");
            disk2.SetDefault(MemoryCoordinate.FromText("2:2A"), "0:**");
            disk2.SetDefault(MemoryCoordinate.FromText("2:3A"), " FOR");
            disk2.SetDefault(MemoryCoordinate.FromText("2:0B"), " @USR");
            disk2.SetDefault(MemoryCoordinate.FromText("2:1B"), "/@PWD");

            disk2.SetMemoryToDefault();
            Disks.Add(disk2);

            Memory.SetMemoryToDefault();

            var ManualProgram = new CpuProgram();
            ManualProgram.AllCommands = new List<CpuCommand>();

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KBD M:0A"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0A @USR"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:1A @PWD"));

            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0A 2:1B"));

            //ManualProgram.AllCommands.Add(ExecCpuCommand.FromText("EXEC 2:**"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT @Index 1:1A"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT X1:1A @U"));

            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:1A 2:1B"));
            ManualProgram.AllCommands.Add(ExecCpuCommand.FromText("EXEC 2:**"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT @Index 1:0B"));
            ManualProgram.AllCommands.Add(new SeekCpuCommand()
            {
                Amount = new LiteralTarget{Value="2"},
                Target = new VariableTarget{Number = "Index"}
            });

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT @Index 1:3B"));

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT X1:0B 1:0B"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT X1:3B 1:3B"));
            ManualProgram.AllCommands.Add(new DumpCommand()
            {
                From = new SearchConstraints
                {
                    DriveConstraint = 1
                },
                Target = new PrinterTarget()
            });
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT `| PRINT"));

            //ManualProgram.AllCommands.Add(new SeekCpuCommand
            //    {
            //        Amount = Target.ResolveTarget("1:0A"),
            //        Target = Target.ResolveTarget("1:1A")
            //    }
            //);

            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT X1:1A @P"));

            //ManualProgram.AllCommands.Add(new AssertCpuCommand
            //{
            //    Assertions = new List<Assertion>
            //    {
            //        new Assertion {CompareRight = Target.ResolveTarget("@U"), CompareLeft = Target.ResolveTarget("M:0A")},
            //       new Assertion {CompareRight = Target.ResolveTarget("@P"), CompareLeft = Target.ResolveTarget("M:1A")}
            //    }

            //});

            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT `0 1:0A"));

            //ManualProgram.AllCommands.Add(new SeekCpuCommand
            //    {
            //        Amount = new LiteralTarget { Value = "1" },
            //        Target = Target.ResolveTarget("1:0A")
            //    }
            //);


            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT 2:1A 1:1A"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT 2:2A 1:2A"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT 2:3A 1:3A"));
            ////ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT 2:0B 1:0B"));

            //ManualProgram.AllCommands.Add(ExecCpuCommand.FromText("EXEC 1:**"));

            ////ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0A @SEARCH_TEXT"));
            ////ManualProgram.AllCommands.Add(QueryCpuCommand.FromText("QUERY 0:3* FOR @SEARCH_TEXT"));

            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT @Index M:0B"));




            AddProcess("Login.exe", new Process
            {
                Memory = Memory,
                Source = ManualProgram,
                Prompt = "Please enter your password",
                Instruction = "^ Can't remember your password? Try 'Password'."

            });


        }

        public override bool IsAtWinCondition()
        {
            var winningLines = Printer.TextLines
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .FirstOrDefault(x => "ACCESS GRANTED USR2 SEC:HIGH" == x || "ACCESS GRANTED USR1 SEC:HIGH" == x);


            if (winningLines != null)
                Console.WriteLine("Winning line: " + winningLines);
            return winningLines != null;
        }
    }


    public class SqlForUsingMemoryAddress : Scenario
    {
        public override void Initialize()
        {
            var Memory = new AddressableRegion
            {
                SizeRows = 2,
                SizeColumns = 4,
            };
            Memory.InitializeEmptyMemorySpace();



            #region disk
            var disk0 = new AddressableRegion();
            disk0.VolumeName = "Keystore";
            disk0.ReadOnly = true;
            disk0.DriveId = 0;
            disk0.SizeRows = 3;
            disk0.SizeColumns = 4;
            disk0.InitializeEmptyMemorySpace();
            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 0 }, "~451");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 0, DriveId = 0 }] = true;

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 1 }, "~451");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 1, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 1, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 1, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 1, DriveId = 0 }] = true;

            disk0.SetDefault(new MemoryCoordinate { DriveId = 0, X = 0, Y = 2 }, "~451");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 2, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 2, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 2, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 2, DriveId = 0 }] = true;

            disk0.SetMemoryToDefault();
            Disks.Add(disk0);
            #endregion


            var disk1 = new AddressableRegion
            {
                VolumeName = "QUERY_BUILDER",
                ReadOnly = false,
                DriveId = 1,
                SizeRows = 2,
                SizeColumns = 4
            };
            disk1.InitializeEmptyMemorySpace();
            disk1.SetDefault(MemoryCoordinate.FromText("1:0A"), "QUER");
            disk1.SetDefault(MemoryCoordinate.FromText("1:1A"), "Y ");
            disk1.SetDefault(MemoryCoordinate.FromText("1:2A"), "0:3*");
            disk1.SetDefault(MemoryCoordinate.FromText("1:3A"), " FOR");
            disk1.SetDefault(MemoryCoordinate.FromText("1:0B"), "   `");

            disk1.SetMemoryToDefault();
            Disks.Add(disk1);


            var disk2 = new AddressableRegion
            {
                VolumeName = "QUERY_TEMPLATE",
                ReadOnly = false,
                DriveId = 2,
                SizeRows = 2,
                SizeColumns = 4
            };
            disk2.InitializeEmptyMemorySpace();
            disk2.SetDefault(MemoryCoordinate.FromText("2:0A"), "QUER");
            disk2.SetDefault(MemoryCoordinate.FromText("2:1A"), "Y ");
            disk2.SetDefault(MemoryCoordinate.FromText("2:2A"), "0:3*");
            disk2.SetDefault(MemoryCoordinate.FromText("2:3A"), " FOR");
            disk2.SetDefault(MemoryCoordinate.FromText("2:0B"), "   `");

            disk2.SetMemoryToDefault();
            Disks.Add(disk2);

            Memory.SetMemoryToDefault();

            var ManualProgram = new CpuProgram();
            ManualProgram.AllCommands = new List<CpuCommand>();

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KBD 1:1B"));

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT 2:0A 1:0A"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT 2:1A 1:1A"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT 2:2A 1:2A"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT 2:3A 1:3A"));
            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT 2:0B 1:0B"));

            ManualProgram.AllCommands.Add(ExecCpuCommand.FromText("EXEC 1:**"));

            //ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0A @SEARCH_TEXT"));
            //ManualProgram.AllCommands.Add(QueryCpuCommand.FromText("QUERY 0:3* FOR @SEARCH_TEXT"));

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



    public class DereferencingScenario : Scenario
    {
        public override void Initialize()
        {
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
            disk0.SizeRows = 1;
            disk0.SizeColumns = 4;
            disk0.InitializeEmptyMemorySpace();
            disk0.SetDefault(new MemoryCoordinate{DriveId = 0,X=0,Y=0},"0451");
            disk0.EncryptionState[new MemoryCoordinate { X = 0, Y = 0,DriveId = 0}] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 1, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 2, Y = 0, DriveId = 0 }] = true;
            disk0.EncryptionState[new MemoryCoordinate { X = 3, Y = 0, DriveId = 0 }] = true;
            disk0.SetMemoryToDefault();
            Disks.Add(disk0);
            #endregion

            #region disk
            var disk1 = new AddressableRegion();
            disk1.VolumeName = "Messages";
            disk1.ReadOnly = true;
            disk1.DriveId = 1;
            disk1.SizeRows = 1;
            disk1.SizeColumns = 4;
            disk1.InitializeEmptyMemorySpace();
            disk1.SetDefault(new MemoryCoordinate { DriveId = 1, X = 0, Y = 0 }, "PLEA");
            disk1.SetDefault(new MemoryCoordinate { DriveId = 1, X = 1, Y = 0 }, "SE W");
            disk1.SetDefault(new MemoryCoordinate { DriveId = 1, X = 2, Y = 0 }, "AIT|");
            disk1.SetMemoryToDefault();
            Disks.Add(disk1);

            var disk2 = new AddressableRegion();
            disk2.VolumeName = "Messages";
            disk2.DriveId = 1;
            disk2.SizeRows = 1;
            disk2.SizeColumns = 4;
            disk2.IsExternalDrive = true;
            disk2.InitializeEmptyMemorySpace();
            disk2.SetDefault(new MemoryCoordinate { DriveId = 1, X = 0, Y = 0 }, "PLEA");
            disk2.SetDefault(new MemoryCoordinate { DriveId = 1, X = 1, Y = 0 }, "SE W");
            disk2.SetDefault(new MemoryCoordinate { DriveId = 1, X = 2, Y = 0 }, "AIT|");
            disk2.SetMemoryToDefault();
            Disks.Add(disk2);
            #endregion


            Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 0 }, "MSG");
            Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 0 }, "1:0A");
            Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 0 }, "1:1A");
            Memory.SetDefault(new MemoryCoordinate { X = 3, Y = 0 }, "1:2A");

            Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 1 }, "NOTH");
            Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 1 }, "ING ");
            Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 1 }, "TO  ");

            Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 2 }, "SEE ");
            Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 2 }, "HERE");


            //Memory.SetDefault(new MemoryCoordinate { X = 3, Y = 2 }, "0x51");
            //Memory.EncryptionState[new MemoryCoordinate { X = 3, Y = 2 }] = true;

            Memory.SetMemoryToDefault();

            

            var ManualProgram = new CpuProgram();
            ManualProgram.AllCommands = new List<CpuCommand>();
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT XM:1A PRINT"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT XM:2A PRINT"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT XM:3A PRINT"));

            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KB M:2C"));

            ManualProgram.AllCommands.Add(new AssertCpuCommand("M:2C", "0:0,0"));

            AddProcess("Login.exe", new Process
            {
                Memory = Memory, Source = ManualProgram,
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


    public class WhatsThePasswordScenario : Scenario
    {
        public override void Initialize()
        {
            NextScenario = "Scenario3";

var Memory = new AddressableRegion
            {
                SizeRows = 3,
                SizeColumns = 4,
            };
            Memory.InitializeEmptyMemorySpace();

            
            Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 1 }, "NOTH");
            Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 1 }, "ING ");
            Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 1 }, "TO  ");

            Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 2 }, "SEE ");
            Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 2 }, "HERE");


            Memory.SetDefault(new MemoryCoordinate { X = 3, Y = 2 }, "0x51");
            Memory.EncryptionState[new MemoryCoordinate {X = 3, Y = 2}] = true;

            Memory.SetMemoryToDefault();


            var ManualProgram = new CpuProgram();
            ManualProgram.AllCommands = new List<CpuCommand>();
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KB M:0,0"));

            ManualProgram.AllCommands.Add(new AssertCpuCommand("M:0,0", "M:3,2"));

            AddProcess("Login.exe", new Process { Memory = Memory, Source = ManualProgram,
                Prompt = "Please enter your pin number.",
                Instruction = "^ Can't remember your pin number? Try '1234'. That's what I use for my luggage."
            });

            this.Hints.Add(new Hint
            {
                Title = "What am I looking at?",
                Body = "See those red asterisks? That tells you that the memory is *encrypted*. The 'ASSERT' CPU instruction is comparing the encrypted data to your entered data."
            });

            this.Hints.Add(new Hint
            {
                Title = "How can I decrypt the contents of the memory?",
                Body = "You can't. Your goal is to get the 'ASSERT' to pass, not to figure out what the actual password is."
            });

            this.Hints.Add(new Hint
            {
                Title = "How can I get the ASSERT to match if I don't know what the actual password is?",
                Body = "You'd need to have control over both values being compared."
            });

            this.Hints.Add(new Hint
            {
                Title = "How do I get control over both values being compared.",
                Body = "You already have half of it by entering a 4 digit password. You need to overrun the contents of the buffer such that the value in memory coordinate M:0A matches the value in memory coordinate M:3C to pass the command 'ASSERT M:0A M:3C PRINT'"
            });

            this.Hints.Add(new Hint
            {
                Title = "Just spoil it already",
                Body = "Enter a 48 character string where the first four characters match the last characters."
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

    public class HelloWorldScenario : Scenario
    {
        public override void Initialize()
        {
            NextScenario = "Scenario2";

            var Memory = new AddressableRegion
            {
                SizeRows = 3,
                SizeColumns = 4,
            };
            Memory.InitializeEmptyMemorySpace();

            Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 1 }, "HELL");
            Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 1 }, "O, WO");
            Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 1 }, "RLD|");

            Memory.SetMemoryToDefault();

            var ManualProgram = new CpuProgram();
            ManualProgram.AllCommands = new List<CpuCommand>();
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KB M:0,0"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0,1 PRINT"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:1,1 PRINT"));
            ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:2,1 PRINT"));

            this.Hints.Add(new Hint
            {
                Title = "What am I looking at?",
                Body = "This program places your number input into memory using the PUT instruction. It then PUTs 'hello world', which is stored at a later address, to your display output. You cannot modify the CPU instructions, so you must find another way to alter the control flow of the program.",
                InterfaceHelpLink = true

            });

            this.Hints.Add(new Hint
            {
                Title = "BUG REPORT 001",
                Body = "Did you try entering 11? Doesn't seem like it's actually restricting numbers to 1-10."
            });

            this.Hints.Add(new Hint
            {
                Title = "BUG REPORT 002",
                Body = "Addendum to report #1, it looks like the field is not restricted to numbers. I was able to enter the word 'HOTDOG'"
            });

            AddProcess("hello_word.exe", new Process
            {
                Memory = Memory, 
                Source = ManualProgram,
                Prompt = "What's your favorite number between 1 and 10?",
                Instruction = "^ Please answer truthfully. This program knows when you're lying."

            });

        }

        public override bool IsAtWinCondition()
        {
            var winningLines = Printer.TextLines
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .FirstOrDefault(x => "HELLO, WORLD" != x);

            if (winningLines != null)
            Console.WriteLine("Winning line: "+winningLines);
            return winningLines != null;
        }
    }
}
