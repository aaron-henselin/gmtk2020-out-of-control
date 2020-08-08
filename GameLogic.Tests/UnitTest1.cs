using gmtk2020_blazor.Models.Cpu;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GameLogic.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void CanReadToMemoryExtent()
        {
            var scenario1 = new HelloWorldScenario();
            var process = scenario1.Processes["Login.exe"];
            process.Memory = new AddressableRegion
            {
                SizeRows = 3,
                SizeColumns = 10,
            };
            process.Memory.InitializeEmptyMemorySpace();
            process.Memory.SetMemoryToDefault();

        }

        [TestMethod]
        public void PadsWhenNotDivisibleBy4()
        {
            var scenario = new DereferencingScenario();
            scenario.Initialize();

            scenario.KeyboardInput.Text = "111111110:0A";

                var process = scenario.Processes["Login.exe"];
                process.Source.RunNextStep(process,0, new CpuCommandContext{Scenario = scenario});
                process.Source.RunNextStep(process, 1, new CpuCommandContext { Scenario = scenario });

            scenario.KeyboardInput.Text = "1111";

            process.Source.RunNextStep(process, 0, new CpuCommandContext { Scenario = scenario });
            process.Source.RunNextStep(process, 1, new CpuCommandContext { Scenario = scenario });

        }

        //[TestMethod]
        //public void PadsWhenNotDivisibleBy4()
        //{
        //    Assert.AreEqual("aaron   ",KeyboardCpuCommand.Pad4("aaron"));
        //}
        //[TestMethod]
        //public void DoesNotPadWhenAlreadyDivisible()
        //{
        //    Assert.AreEqual("aaro",KeyboardCpuCommand.Pad4("aaro"));
        //}
    }
}
