using System;
using System.Linq;
using System.Threading.Tasks;
using gmtk2020_blazor.Models.Cpu;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GameLogic.Tests
{
    public class ScenarioPackageDeserializerTests
    {
        [TestClass]
        public class Deserialize
        {
            private string testFile = @"
                =metadata=
                prompt:What's your favorite number between 1 and 10?
                instruction:^ Please answer truthfully. This program knows when you're lying.
                name:hello_world.exe

                =memory=
                3x4:HELLO, WORLD|

                =source=
                PUT KB M:0,0
                PUT M:0,1 PRINT
                PUT M:1,1 PRINT
                PUT M:2B PRINT
                PUT KB M:0C
                TEST M:0C 0:0,0
                ";

            private string drivesFile = @"
                =metadata=
                name:Keystore
                readonly:false

                =contents=
                1x4:
                #0x1x,#   ,#   ,#    
                ";

            [TestMethod]
            public void Succeeds()
            {
                var package = ScenarioPackageDeserializer.Deserialize(testFile,null,drivesFile);
                var scenario = new HelloWorldScenario(package);
            }
        }
    }

    [TestClass]
    public class FileReaderTests
    {
        [TestClass]
        public class ReadSections
        {
            [TestMethod]
            public void FindsAllSections()
            { 
                var testFile = 
                @"

                =metadata=
                prompt:What's your favorite number between 1 and 10?

                =memory=
                3x4:HELLO, WORLD|

                =source=
                PUT KB M:0,0

                ";
                
                var sections = FileReader.ReadSections(testFile);
                Assert.IsTrue(sections.ContainsKey("metadata"));
                Assert.IsTrue(sections.ContainsKey("memory"));
                Assert.IsTrue(sections.ContainsKey("source"));
            }



        }

        [TestClass]
        public class ReadDictionaryTests
        {
            [TestMethod]
            public void ParsesKeysAndValues()
            {
                var testFile = @"
                =dictionary=
                key1:value1
                key2:value2
                key3:value3
                ";
                var sections = FileReader.ReadSections(testFile);
                var dict = FileReader.ReadDictionary(sections["dictionary"]);
                Assert.AreEqual("value1",dict["key1"]);
                Assert.AreEqual("value2", dict["key2"]);
                Assert.AreEqual("value3", dict["key3"]);
            }
        }

        [TestClass]
        public class ReadArrayTests
        {
            [TestMethod]
            public void HasCorrectNumberOfElements()
            {
                var testFile = @"
                key1:value1
                --
                key2:value2
                --
                key3:value3
                ";
           
                var arr = FileReader.ReadArray(testFile);
                Assert.AreEqual(3,arr.Count());
            }
        }

        [TestClass]
        public class ReadAddressableRegionTests
        {
            [TestMethod]
            public void ReadsGridSize()
            {
                var testFile = new[] {"3x4:test"};

                var arr = FileReader.ReadAddressableRegion(testFile,null);
                Assert.AreEqual(3,arr.SizeRows);
                Assert.AreEqual(4, arr.SizeColumns);
            }

            [TestMethod]
            public void ReadsSingleLineContents()
            {
                var testFile = new[] { "3x4:test" };

                var arr = FileReader.ReadAddressableRegion(testFile,null);
                var defaultValue = arr.Default[MemoryCoordinate.FromText("M:0A")];
                Assert.AreEqual("test",defaultValue);
            }

            [TestMethod]
            public void ReadsMutiLineContents()
            {
                var testFile = new[]
                {
                    "3x4:",
                    "test,test,test,test",
                    "test,test,test,test",
                    "test,test,test,test"
                };

                var arr = FileReader.ReadAddressableRegion(testFile,null);
                var defaultValue = arr.Default[MemoryCoordinate.FromText("M:3C")];
                Assert.AreEqual("test", defaultValue);
            }
        }
        //[TestMethod]
        //public async Task Sc6()
        //{
        //    var scenario1 = new SqlReplaceEntireQuery();
        //    //await scenario1.Initialize();
        //    scenario1.KeyboardInput = new KeyboardInput {Text = "test"};
        //    var loginExe = scenario1.Processes["Login.exe"];
        //    var context = new CpuCommandContext {Scenario = scenario1};
        //    loginExe.Source.RunNextStep(loginExe,0, context);
        //    loginExe.Source.RunNextStep(loginExe, 1, context);
        //    loginExe.Source.RunNextStep(loginExe, 2, context);
        //    loginExe.Source.RunNextStep(loginExe, 3, context);

        //}

        //[TestMethod]
        //public void CanReadToMemoryExtent()
        //{
        //    var scenario1 = new HelloWorldScenario();
        //    var process = scenario1.Processes["Login.exe"];
        //    process.Memory = new AddressableRegion
        //    {
        //        SizeRows = 3,
        //        SizeColumns = 10,
        //    };
        //    process.Memory.InitializeEmptyMemorySpace();
        //    process.Memory.SetMemoryToDefault();

        //}

        //[TestMethod]
        //public async Task PadsWhenNotDivisibleBy4()
        //{
        //    var scenario = new DereferencingScenario();
        //    //await scenario.Initialize();

        //    scenario.KeyboardInput.Text = "111111110:0A";

        //        var process = scenario.Processes["Login.exe"];
        //        process.Source.RunNextStep(process,0, new CpuCommandContext{Scenario = scenario});
        //        process.Source.RunNextStep(process, 1, new CpuCommandContext { Scenario = scenario });

        //    scenario.KeyboardInput.Text = "1111";

        //    process.Source.RunNextStep(process, 0, new CpuCommandContext { Scenario = scenario });
        //    process.Source.RunNextStep(process, 1, new CpuCommandContext { Scenario = scenario });

        //}
    }

    [TestClass]
    public class CpuCommandFactoryTests
    {
        [TestMethod]
        public void ReadsCommand()
        {
            var isReadCommand = CpuCommandFactory.Build("PUT KB M:0,0") is ReadCpuCommand readCpuCommand;
            Assert.IsTrue(isReadCommand);
        }
    }
}
