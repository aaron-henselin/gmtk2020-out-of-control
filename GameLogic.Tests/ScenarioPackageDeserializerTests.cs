using System;
using System.Threading.Tasks;
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
}
