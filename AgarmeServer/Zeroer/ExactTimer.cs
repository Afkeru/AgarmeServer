using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#pragma warning disable 1061,0164,169
namespace AgarmeServer.Zeroer
{
    public class ExactTimer
    {
        public double Frequency
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => frequency;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                frequency = value;
                interval = 1000.0 / Frequency;
            }
        }

        private double interval;

        public double Interval
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => interval;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                interval = value;
                frequency = 1000.0 / value;
            }
        }

        public bool Running
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => running;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value)
                {
                    Start();
                }
                else
                {
                    StopAndWait();
                }
            }
        }

        public event Action Event;

        private bool running;
        private Task task;

        private bool runningCondition;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start()
        {
            task = Task.Run(Run);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopAndWait()
        {
            StartStop();
            WaitStop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartStop()
        {
            runningCondition = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WaitStop()
        {
            task.Wait();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopImmediately()
        {
            StartStop();
            task.Dispose();
            running = false;
        }

        [DllImport("kernel32")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static extern short QueryPerformanceCounter(out long count);

        [DllImport("kernel32")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static extern short QueryPerformanceFrequency(out long frequency);

        private static readonly Func<double> GetMillisecond;

        public static double Millisecond
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetMillisecond();
            }
        }

        static ExactTimer()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                QueryPerformanceFrequency(out var frequency);
                var a = 1.0 / (double)frequency * 1000.0;

                GetMillisecond = () =>
                {
                    QueryPerformanceCounter(out var count);
                    return count * a;
                };
            }
            else
            {
                GetMillisecond = () => Environment.TickCount;
            }
        }

        private double begin, end;
        private double frequency;

        /// <summary>
        /// 阻塞运行
        /// </summary>
        public void Run()
        {
            running = runningCondition = true;
            var ticks = 0.0;
            while (runningCondition)
            {
                //将延迟的时加入时间缓冲区
                begin = GetMillisecond();
                Thread.Sleep(1);

                //缓冲区过大，强制削减
                if (ticks > 3 * interval)
                {
                    ticks = 3 * interval;
                }

                //时间缓冲区膨胀到一定程度时调用事件
                while (ticks > interval)
                {
                    //if (!runningCondition) goto End;
                    Event?.Invoke();
                    //削减缓冲区
                    ticks -= interval;
                }

                //将延迟的时加入时间缓冲区
                ticks += GetMillisecond() - begin;
            }
        End:
            running = false;
        }
    }
}
