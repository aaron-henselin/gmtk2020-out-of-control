using gmtk2020_blazor.Models.Cpu;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GameLogic.Tests
{
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

    [TestClass]
    public class TargetTests
    {


        [TestClass]
        public class ResolveTarget
        {
            [TestMethod]
            public void ResolvesVariableTarget()
            {
                var variableTarget = (VariableTarget)Target.ResolveTarget("@A");
                Assert.AreEqual("A",variableTarget.Number);
            }

            [TestMethod]
            public void ResolvesDereferenceTarget()
            {
                var variableTarget = (DeRefTarget)Target.ResolveTarget("XM:0A");
                Assert.AreEqual("M:0A", variableTarget.ReferenceLocation.ToString());
            }
        }
    }
}
