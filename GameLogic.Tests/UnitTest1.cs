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
            scenario1.Memory = new AddressableRegion
            {
                SizeRows = 3,
                SizeColumns = 10,
            };
            scenario1.Memory.InitializeEmptyMemorySpace();
            scenario1.Memory.SetMemoryToDefault();


            var address = scenario1.Memory.Current[new MemoryCoordinate {X = scenario1.Memory.SizeColumns-1, Y = scenario1.Memory.SizeRows - 1 }];

        }

        [TestMethod]
        public void PadsWhenNotDivisibleBy4()
        {
            var scenario = new DereferencingScenario();
            scenario.Initialize();

            scenario.KeyboardInput.Text = "111111110:0A";

            scenario.ManualProgram.RunNextStep(scenario,0, new CpuCommandContext());
            scenario.ManualProgram.RunNextStep(scenario, 1, new CpuCommandContext());

            scenario.KeyboardInput.Text = "1111";

            scenario.ManualProgram.RunNextStep(scenario, 0, new CpuCommandContext());
            scenario.ManualProgram.RunNextStep(scenario, 1, new CpuCommandContext());

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
