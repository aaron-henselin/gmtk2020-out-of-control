using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameLogic;
using gmtk2020_blazor.Helpers;
using gmtk2020_blazor.Models.Cpu;
using Microsoft.AspNetCore.Components;

namespace gmtk2020_blazor
{
    public class IndexBase : LayoutComponentBase
    {
        [Inject]
        public BlazorTimer Timer { get; set; }

        public static EventHandler<EventArgs> StepRan;

        public EventHandler<EventArgs> RunningStatusChanged;

        public EventHandler<EventArgs> WinConditionRaised;

        public bool ManualProgramRunning { get; set; }

        public void Launch(string keyboardInput)
        {
            Console.WriteLine("Launching with keyboard input "+keyboardInput);
            Scenario.KeyboardInput.Text = keyboardInput;

            Timer.ForegroundCpuContext = new CpuCommandContext();
            Timer.ForegroundCpuCycle = 0;
            ManualProgramRunning = true;
            RunningStatusChanged?.Invoke(this,new EventArgs());
        }

        private void StartTimer()
        {
            Timer.SetTimer(500);
            Timer.OnElapsed += SimulationTick;
            
            Console.WriteLine("Timer Started.");
        }

        private void SimulationTick()
        {

            if (Scenario.BackgroundProgram != null)
            {

                Timer.BackgroundCpuCycle++;
                Scenario.BackgroundProgram.RunNextStep(Scenario, Timer.BackgroundCpuCycle-1, Timer.BackgroundCpuContext);
                

                var ended = Timer.BackgroundCpuCycle == Scenario.BackgroundProgram.AllCommands.Count;
                if (ended)
                    Timer.BackgroundCpuCycle = 0;
            }

            if (ManualProgramRunning)
            {
                
                Timer.ForegroundCpuCycle++;

                bool exceptional = false;
                try
                {
                    Scenario.ManualProgram.RunNextStep(Scenario, Timer.ForegroundCpuCycle - 1, Timer.ForegroundCpuContext);
                }
                catch (Exception e)
                {
                    exceptional = true;
                    Timer.ForegroundCpuCycle = 0;
                }

                var ended = exceptional || Timer.ForegroundCpuCycle == Scenario.ManualProgram.AllCommands.Count;
                if (ended)
                {
                    ManualProgramRunning = false;
                    //Timer.ForegroundCpuCycle = 0;

                    var isWinCondition = Scenario.IsAtWinCondition();
                    RunningStatusChanged?.Invoke(this, new EventArgs());
                    if (isWinCondition)
                        WinConditionRaised?.Invoke(this, new EventArgs());

                    if (exceptional)
                        Scenario.CpuLog.Log("<SEGFAULT>");
                }
            }

            StepRan?.Invoke(new object(), new EventArgs());
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();



        }

        protected override void OnParametersSet()
        {
            Scenario.Initialize();
            StartTimer();
        }

        //public CpuProgram CpuProgram { get; set; }

        public Scenario Scenario { get; set; }// = new HelloWorldScenario();

    }



}