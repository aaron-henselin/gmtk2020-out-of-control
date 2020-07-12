using gmtk2020_blazor.Models.Cpu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace gmtk2020_blazor.Helpers
{

    public class BlazorTimer
    {
        private Timer _timer;

        public void SetTimer(double interval)
        {
            _timer = new Timer(interval);
            _timer.AutoReset = true;
            _timer.Elapsed += NotifyTimerElapsed;
            _timer.Enabled = true;
        }

        public int BackgroundCpuCycle { get; set; }
        public int ForegroundCpuCycle { get; set; }
        public CpuCommandContext ForegroundCpuContext { get; internal set; } = new CpuCommandContext();
        public CpuCommandContext BackgroundCpuContext { get; internal set; } = new CpuCommandContext();

        public event Action OnElapsed;

        private void NotifyTimerElapsed(Object source, ElapsedEventArgs e)
        {
            OnElapsed?.Invoke();
        }
    }
}
