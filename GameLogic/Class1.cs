﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
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
        public AddressableRegion(int rows, int columns,int? driveId)
        {
            SizeRows = rows;
            SizeColumns = columns;
            DriveId = driveId;
            InitializeEmptyMemorySpace();
        }

        public AddressableRegion(){}

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
            foreach (var searchCoordinate in coordinates)
            {
                if (TestSearchCoordinate(searchCoordinate, paddedValues))
                    return searchCoordinate;
            }

            return null;
        }

        private bool TestSearchCoordinate(MemoryCoordinate searchCoordinate, List<string> paddedValues)
        {
            bool matched = true;

            var compareCoordinate = searchCoordinate;
            int matchLength = 0;
            foreach (var paddedValue in paddedValues)
            {
                var compareValue = Read(compareCoordinate);
                compareValue = compareValue.PadRight(4, ' ');
                var areEqual = string.Equals(compareValue, paddedValue, StringComparison.OrdinalIgnoreCase);
                if (!areEqual)
                {
                    if (matchLength > 0)
                    {
                        Console.WriteLine(
                            $"Multi-cell match failed at {compareCoordinate} on comparison '{compareValue}' to '{paddedValue}', out of: {string.Join("/", paddedValues)}");
                    }

                    matched = false;
                    break;
                }
                matchLength++;
                compareCoordinate = this.NextAddress(compareCoordinate);
            }

            return matched;
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

    public class Section
    {
        public string Name { get; set; }
        public List<string> Lines { get; set; } = new List<string>();
    }

    public class WinCondition
    {
        public string Description { get; set; }

        public List<WinConditionMatch> Matches { get; set; } = new List<WinConditionMatch>();
    }

    public class WinConditionMatch
    {
        public string MatchLine { get; set; }
        public bool NegateMatch { get; set; }
    }

    public static class ScenarioPackageDeserializer
    {
        public static ScenarioPackage Deserialize(string programsTxt,string descriptionTxt,string disksTxt)
        {


            var package = new ScenarioPackage();

            DeserializeProgramsFile(programsTxt, package);

            if (descriptionTxt != null)
            {
                DeserializeDescriptionFile(descriptionTxt, package);
            }

            if (disksTxt != null)
            {
                DeserializeDisksFile(disksTxt, package);
            }

            return package;

        }

        private static void DeserializeProgramsFile(string programsTxt, ScenarioPackage package)
        {
            var groups = FileReader.ReadArray(programsTxt);
            foreach (var group in groups)
            {
                var sections = FileReader.ReadSections(@group);


                var metadataSection = sections["metadata"];
                var programMetadata = FileReader.ReadDictionary(metadataSection.Lines);
                var programName = programMetadata["name"];
                var prompt = programMetadata["prompt"];
                var instruction = programMetadata["instruction"];

                var sourceSection = sections["source"];
                IReadOnlyCollection<CpuCommand> commands;
                try
                {
                    commands = FileReader.ReadCpuCommands(sourceSection.Lines);
                }
                catch (Exception e)
                {
                    Console.WriteLine("program contents was: ");
                    foreach (var line in sourceSection.Lines)
                        Console.WriteLine(line);

                    throw;
                }


                var memorySection = sections["memory"];
                var memory = FileReader.ReadAddressableRegion(memorySection.Lines,null);

                package.Processes.Add(new ScenarioProcess
                {
                    Name = programName,
                    Process = new Process
                    {
                        Memory = memory,
                        Prompt = prompt,
                        Instruction = instruction,
                        Source = new CpuProgram
                        {
                            AllCommands = commands.ToList()
                        }
                    }
                });
            }
        }

        private static void DeserializeDisksFile(string disksTxt, ScenarioPackage package)
        {
            int i = 0;
            var driveArray = FileReader.ReadArray(disksTxt).ToList();
            foreach (var driveDefinition in driveArray)
            {
                var sections = FileReader.ReadSections(driveDefinition);
                var contentsSection = sections["contents"];
                var metadataSection = sections["metadata"];

                var metadata = FileReader.ReadDictionary(metadataSection);

                var memorySetup = FileReader.ReadAddressableRegion(contentsSection,i++);
                memorySetup.ReadOnly = metadata["readonly"] == "true";
                memorySetup.VolumeName = metadata["name"];

                package.Drives.Add(memorySetup);
            }
            
        }

        private static void DeserializeDescriptionFile(string descriptionTxt, ScenarioPackage package)
        {
            var descriptionSections = FileReader.ReadSections(descriptionTxt);
            var headerSection = descriptionSections["header"];
            var descriptionSection = descriptionSections["description"];
            package.Title = headerSection.Lines.FirstOrDefault();
            package.DescriptionLines = descriptionSection.Lines;

            var winConditionSection = descriptionSections["wincondition"];
            var winConditionDictionary = FileReader.ReadDictionary(winConditionSection);

            package.WinCondition = new WinCondition
            {
                Description = winConditionDictionary["description"],
            };

            var test = winConditionDictionary["test"];
            var conditions = test.Split(new[] {"||"}, StringSplitOptions.None);
            foreach (var condition in conditions)
            {
                package.WinCondition.Matches.Add(new WinConditionMatch
                {
                    NegateMatch = condition[0] == '!',
                    MatchLine = condition.Substring(1).Trim()
                });
            }
        }
    }

    public static class FileReader
    {
        

        public static IReadOnlyCollection<CpuCommand> ReadCpuCommands(IReadOnlyCollection<string> strs)
        {
            var commands = new List<CpuCommand>();
            foreach (var str in strs)
                commands.Add(CpuCommandFactory.Build(str));
            return commands;
        }

        public static Dictionary<string, Section> ReadSections(string str)
        {
            string[] lines = GetLines(str);
            return ReadSections(lines);
        }

        public static Dictionary<string,Section> ReadSections(IReadOnlyCollection<string> lines)
        {
            List<Section> groups = new List<Section> { };
            foreach (var line in lines)
            {
                var isSectionName = line.StartsWith("=") && line.EndsWith("=");
                if (isSectionName)
                {
                    var name = line.Substring(1, line.Length - 2);
                    groups.Add(new Section{Name = name});
                }
                else
                    groups[groups.Count - 1].Lines.Add(line);

            }

            var duplicates=groups.ToLookup(x => x.Name).Where(x => x.Count() > 1);
            if (duplicates.Any())
            {
                throw new InvalidOperationException("Section '" + duplicates.First().Key + "' appears more than once.");
            }

            return groups.ToDictionary(x => x.Name,StringComparer.OrdinalIgnoreCase);
        }

        static string[] GetLines(string str)
        {
            string[] lines = str.Split(
                    new[] { Environment.NewLine },
                    StringSplitOptions.None
                )
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToArray();

            return lines;
        }

        public static IEnumerable<IReadOnlyCollection<string>> ReadArray(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return new IReadOnlyCollection<string>[0];

            string[] lines = GetLines(str);
            List<List<string>> groups = new List<List<string>> { new List<string>() };
            foreach (var line in lines)
            {
                if ("--" == line)
                    groups.Add(new List<string>());
                else
                    groups[groups.Count - 1].Add(line);
            }

            return groups;
        }

        public static KeyValuePair<string, string> ReadKvp(string line)
        {
            var isEmpty = string.IsNullOrWhiteSpace(line);
            if (isEmpty)
                throw new ArgumentException("Argument cannot be empty");

            var index = line.IndexOf(':');
            if (index == -1)
                throw new ArgumentException($"'{line}' does not contain ':'");

            var left = line.Substring(0, index);
            var right = line.Substring(index + 1);
            return new KeyValuePair<string, string>(left, right);
        }

        static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        public static IDictionary<string, string> ReadDictionary(Section section)
        {
            return ReadDictionary(section.Lines);
        }

        public static IDictionary<string, string> ReadDictionary(IEnumerable<string> lines)
        {
            return lines.Select(ReadKvp)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value,StringComparer.OrdinalIgnoreCase);
        }

        public static AddressableRegion ReadAddressableRegion(Section section,int? driveId)
        {
            return ReadAddressableRegion(section.Lines,driveId);
        }

        public static AddressableRegion ReadAddressableRegion(IReadOnlyList<string> lines, int? driveId)
        {

            var kvp = ReadKvp(lines[0]);
            var size = kvp.Key
                .Split(new[] { 'x' })
                .Select(x => Convert.ToInt32(x))
                .ToList();

            var region = new AddressableRegion(size[0], size[1],driveId);
            
            
            var multiline = string.IsNullOrWhiteSpace(kvp.Value);
            if (!multiline)
            {
                var chunks = Split(kvp.Value, 4);
                var writeCoord = region.AllCoordinates.First();

                foreach (var chunk in chunks)
                {
                    region.SetDefault(writeCoord, chunk);
                    writeCoord = region.NextAddress(writeCoord);
                }
            }
            else
            {
                var writeCoord = region.AllCoordinates.First();

                var remainingLines = lines.Skip(1).Take(region.SizeRows);
                foreach (var remainingLine in remainingLines)
                {
                    var chunks = remainingLine.Split(new[] {','}, StringSplitOptions.None);

                    foreach (var chunk in chunks)
                    {
                        var encrypted = chunk.Length > 0 && chunk[0] == '#';
                        string text = encrypted ? chunk.Substring(1) : chunk;

                        region.SetDefault(writeCoord, text);
                        region.EncryptionState[writeCoord] = encrypted;
                        writeCoord = region.NextAddress(writeCoord);
                    }

                }
            }

            return region;
        }

        public static IReadOnlyCollection<Hint> ReadHints(string hints_txt)
        {
            List<Hint> hints = new List<Hint>();
            var groups = ReadArray(hints_txt);
            foreach (var group in groups)
            {
                var dict = FileReader.ReadDictionary(group);
                var hint = new Hint
                {
                    Body = dict["body"],
                    Title = dict["title"],
                   // InterfaceHelpLink = "true" == dict["help_link"],
                };
                hints.Add(hint);
            }
            return hints;
        }
    }

    public class ScenarioPackage
    {
        public List<ScenarioProcess> Processes { get; set; } = new List<ScenarioProcess>();
        public List<Hint> Hints { get; set; } = new List<Hint>();
        public string Title { get; internal set; }
        public List<string> DescriptionLines { get; internal set; }
        public WinCondition WinCondition { get; set; } = new WinCondition();
        public List<AddressableRegion> Drives { get; set; } = new List<AddressableRegion>();
    }

    public class ScenarioProcess 
    {
        public string Name { get; set; }
        public Process Process { get; set; }
    }

    public class ScenarioPackageDownloader
    {
        private readonly HttpClient _httpClient;

        public ScenarioPackageDownloader(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ScenarioPackage> Download(string scenarioName)
        {

            var programsTxt = await _httpClient.GetStringAsync($"lib/scenarios/{scenarioName}/programs.txt");
            var descriptionTxt = await _httpClient.GetStringAsync($"lib/scenarios/{scenarioName}/description.txt");
            var drivesTxt = await _httpClient.GetStringAsync($"lib/scenarios/{scenarioName}/disks.txt");

            //var hints_txt = await _httpClient.GetStringAsync($"/{scenarioName}/hints.txt");
            //package.Hints = FileReader.ReadHints(hints_txt).ToList();

            return ScenarioPackageDeserializer.Deserialize(programsTxt, descriptionTxt, drivesTxt);
        }

    }

    public abstract class Scenario
    {
        
        
        public Scenario(ScenarioPackage scenarioPackage)
        {
            Title = scenarioPackage.Title;
            DescriptionLines = scenarioPackage.DescriptionLines;

            foreach (var process in scenarioPackage.Processes)
            {
                Console.WriteLine("Attaching " + process.Name);

                var p = process.Process;
                p.Memory.SetMemoryToDefault();
                this.Processes.Add(process.Name, p);
            }

            this.WinCondition = scenarioPackage.WinCondition;

            foreach (var drive in scenarioPackage.Drives)
            {
                Console.WriteLine($"Attaching drive {drive.DriveId}");
                Console.WriteLine($"Coord range {drive.AllCoordinates.First()} ->  {drive.AllCoordinates.Last()}");

                drive.SetMemoryToDefault();
                this.Disks.Add(drive);
            }
            
        }

        public List<string> DescriptionLines { get; set; }

        public string Title { get; set; }

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


        public virtual bool IsAtWinCondition()
        {
            var allMatches = this.WinCondition.Matches;
            foreach (var match in allMatches)
            {

                var isWin1 = Printer.TextLines
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Any(x => !match.NegateMatch && string.Equals(match.MatchLine.Trim(), x.Trim(), StringComparison.OrdinalIgnoreCase));
                if (isWin1)
                    return true;

                var isWin2 = Printer.TextLines
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Any(x => match.NegateMatch && !string.Equals(match.MatchLine.Trim(), x.Trim(), StringComparison.OrdinalIgnoreCase));

                if (isWin2)
                    return true;
            }

            return false;
        }

        public Dictionary<string,Process> Processes { get; set; }= new Dictionary<string, Process>();

        public void AddProcess(string name,Process process)
        {
            Processes.Add(name,process);
        }


        public List<AddressableRegion> Disks { get; set; } = new List<AddressableRegion>();

        public KeyboardInput KeyboardInput { get; set; } = new KeyboardInput();

        public PrinterOutput Printer { get; set; } = new PrinterOutput();

        public List<Hint> Hints { get; set; } = new List<Hint>();
        public WinCondition WinCondition { get; private set; }
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

    public class SqlScenario3 : Scenario
    {
        public SqlScenario3(ScenarioPackage package) : base(package)
        {
        }

        public async Task Initialize()
        {
            //await base.Initialize();

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
}
