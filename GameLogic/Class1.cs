using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
                return new MemoryCoordinate {X = xInt , Y =yInt};
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


        internal MemoryCoordinate NextAddress(MemoryCoordinate to)
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

    public abstract class Scenario
    {
        public string NextScenario { get; set; }
        public abstract void Initialize();
        public abstract bool IsAtWinCondition();

        public CpuLog CpuLog { get; set; } = new CpuLog();


        public CpuProgram BackgroundProgram { get; set; }


        public CpuProgram ManualProgram { get; set; }

        public AddressableRegion Memory { get; set; }
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
            Memory = new AddressableRegion
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


            this.ManualProgram = new CpuProgram();
            this.ManualProgram.AllCommands = new List<CpuCommand>();

            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0A PRINT"));
            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:1A PRINT"));
            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:2A PRINT"));

            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0B PRINT"));
            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:1B PRINT"));
            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:2B PRINT"));

            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KB M:0C"));



            this.ManualProgram.AllCommands.Add(new AssertCpuCommand
            {
                CompareLeft = MediaTarget.FromText("M:0C"),
                CompareRight = MediaTarget.FromText("0:0,0")
            });

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

    public class DereferencingScenario : Scenario
    {
        public override void Initialize()
        {
            Memory = new AddressableRegion
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


            this.ManualProgram = new CpuProgram();
            this.ManualProgram.AllCommands = new List<CpuCommand>();
            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT XM:1A PRINT"));
            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT XM:2A PRINT"));
            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT XM:3A PRINT"));

            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KB M:2C"));

            this.ManualProgram.AllCommands.Add(new AssertCpuCommand
            {
                CompareLeft = MediaTarget.FromText("M:2C"),
                CompareRight = MediaTarget.FromText("0:0,0")
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

    public class CameraWipeOutScenario : Scenario
    {
        public override void Initialize()
        {
            Memory = new AddressableRegion
            {
                SizeRows = 3,
                SizeColumns = 4,
            };
            Memory.InitializeEmptyMemorySpace();


            Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 1 }, "CAME");
            Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 1 }, "RA__");
            Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 1 }, "TOOL");

            Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 2 }, "(C) ");
            Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 2 }, "1995");


            //Memory.SetDefault(new MemoryCoordinate { X = 3, Y = 2 }, "0x51");
            //Memory.EncryptionState[new MemoryCoordinate { X = 3, Y = 2 }] = true;

            Memory.SetMemoryToDefault();


            //this.ManualProgram = new CpuProgram();
            //this.ManualProgram.AllCommands = new List<CpuCommand>();
            //this.ManualProgram.AllCommands.Add(new KeyboardCpuCommand
            //{
            //    To = new MemoryCoordinate { X = 0, Y = 0 }
            //});
            //this.ManualProgram.AllCommands.Add(new ReadCpuCommand
            //{
            //    CompareLeft = new MemoryCoordinate { X = 0, Y = 0 },
            //    CompareRight = new MemoryCoordinate { X = 3, Y = 2 }
            //});



        }

        public override bool IsAtWinCondition()
        {
            return false;
        }
    }

    public class WhatsThePasswordScenario : Scenario
    {
        public override void Initialize()
        {
            NextScenario = "Scenario3";

            Memory = new AddressableRegion
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


            this.ManualProgram = new CpuProgram();
            this.ManualProgram.AllCommands = new List<CpuCommand>();
            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KB M:0,0"));

            this.ManualProgram.AllCommands.Add(new AssertCpuCommand
            {
                CompareLeft = MediaTarget.FromText("M:0,0"),
                CompareRight = MediaTarget.FromText("M:3,2")
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

            Memory = new AddressableRegion
            {
                SizeRows = 3,
                SizeColumns = 4,
            };
            Memory.InitializeEmptyMemorySpace();

            Memory.SetDefault(new MemoryCoordinate { X = 0, Y = 1 }, "HELL");
            Memory.SetDefault(new MemoryCoordinate { X = 1, Y = 1 }, "O, WO");
            Memory.SetDefault(new MemoryCoordinate { X = 2, Y = 1 }, "RLD|");

            Memory.SetMemoryToDefault();

            this.ManualProgram = new CpuProgram();
            this.ManualProgram.AllCommands = new List<CpuCommand>();
            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT KB M:0,0"));
            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:0,1 PRINT"));
            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:1,1 PRINT"));
            this.ManualProgram.AllCommands.Add(ReadCpuCommand.FromText("PUT M:2,1 PRINT"));

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
