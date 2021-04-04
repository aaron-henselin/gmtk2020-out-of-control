using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

        [Inject]
        public HttpClient HttpClient { get; set; }

        [Inject]
        public ScenarioPackageDownloader PackageDownloader { get; set; }


        public static EventHandler<EventArgs> StepRan;

        public EventHandler<EventArgs> RunningStatusChanged;

        public EventHandler<EventArgs> WinConditionRaised;

        public EventHandler<EventArgs> ViewportChanged;


        //public bool ManualProgramRunning { get; set; }

        private string _viewportProcessName;

        public string ViewportProcessName
        {
            get
            {
                return _viewportProcessName;
            }
            set
            {
                _viewportProcessName = value;
                this.ViewportChanged?.Invoke(this,new EventArgs());
            }
        }

        public string ScenarioName { get; set; }

        public Process ViewportProcess
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ViewportProcessName))
                    return null;

                return Scenario.Processes[ViewportProcessName];
            }
        }

        public ProcessState ViewportProcessState
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ViewportProcessName))
                    return null;

                if (!Timer.ProcessStates.ContainsKey(ViewportProcessName))
                    return null;

                return Timer.ProcessStates[ViewportProcessName];
            }
        }

        public void Launch(string keyboardInput)
        {
            Console.WriteLine("Launching with keyboard input "+keyboardInput);
            Scenario.KeyboardInput.Text = keyboardInput;

            if (Timer.ProcessStates.ContainsKey(ViewportProcessName))
                Timer.ProcessStates.Remove(ViewportProcessName);

            Timer.ProcessStates.Add(ViewportProcessName, new ProcessState
            {
                CpuCycle=0,
                CommandContext = new CpuCommandContext
                {
                    Scenario = Scenario
                }
            });

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
            foreach (var processName in Scenario.Processes.Keys)
            {
                var process = Scenario.Processes[processName];

                if (Timer.ProcessStates.ContainsKey(processName))
                {
                    var processState = Timer.ProcessStates[processName];
                        processState.CpuCycle++;

                    bool exceptional = false;
                    try
                    {
                        process.Source.RunNextStep(process, processState.CpuCycle - 1, processState.CommandContext);
                    }
                    catch (Exception e)
                    {
                        exceptional = true;
                        Console.Write(e);
                    }
                  

                    var ended = exceptional || processState.CpuCycle == process.Source.AllCommands.Count;
                    if (ended)
                    {
                        var isWinCondition = Scenario.IsAtWinCondition();
                        if (isWinCondition)
                            WinConditionRaised?.Invoke(this, new EventArgs());

                        if (process.IsBackgroundProcess && !exceptional)
                            processState.CpuCycle = 0;
                        else
                        {
                            Timer.ProcessStates.Remove(processName);
                            RunningStatusChanged?.Invoke(this, new EventArgs());
                        }
                        
                        if (exceptional)
                            process.CpuLog.Log("<SEGFAULT>");
                    }



                }

            }


            //if (ManualProgramRunning)
            //{
                
            //    Timer.ForegroundCpuCycle++;

            //    bool exceptional = false;
            //    try
            //    {
            //        Scenario.ManualProgram.RunNextStep(Scenario, Timer.ForegroundCpuCycle - 1, Timer.ForegroundCpuContext);
            //    }
            //    catch (Exception e)
            //    {
            //        exceptional = true;
            //        Timer.ForegroundCpuCycle = 0;
            //    }

            //    var ended = exceptional || Timer.ForegroundCpuCycle == Scenario.ManualProgram.AllCommands.Count;
            //    if (ended)
            //    {
            //        ManualProgramRunning = false;
            //        //Timer.ForegroundCpuCycle = 0;

            //        var isWinCondition = Scenario.IsAtWinCondition();
            //        RunningStatusChanged?.Invoke(this, new EventArgs());
            //        if (isWinCondition)
            //            WinConditionRaised?.Invoke(this, new EventArgs());

            //        if (exceptional)
            //            Scenario.CpuLog.Log("<SEGFAULT>");
            //    }
            //}

            StepRan?.Invoke(new object(), new EventArgs());
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();


            StartTimer();

        }

        protected override async Task OnParametersSetAsync()
        {
            SetDefaultViewport();

            Timer.ClearProcessStates();
            var processNames = Scenario.Processes.Where(x => x.Value.IsBackgroundProcess).Select(x => x.Key);
            foreach (var backgroundProcessName in processNames)
            {
                if (!Timer.ProcessStates.ContainsKey(backgroundProcessName))
                    Timer.ProcessStates.Add(backgroundProcessName, new ProcessState
                    {
                        CpuCycle = 0,
                        CommandContext = new CpuCommandContext
                        {
                            Scenario = Scenario
                        }
                    });
            }



        }

        private void SetDefaultViewport()
        {
            string defaultViewport = Scenario.Processes.FirstOrDefault(x => !x.Value.IsBackgroundProcess).Key;
            ViewportProcessName = defaultViewport;
        }

        //public CpuProgram CpuProgram { get; set; }

        public Scenario Scenario { get; set; }// = new HelloWorldScenario();

    }



}