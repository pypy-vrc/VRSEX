using System.Diagnostics;
using System.Threading;

namespace VRSEX
{
    public static class PerformanceMonitor
    {
        public static float CpuUsage { get; private set; }

        private static AutoResetEvent m_Event;
        private static Thread m_Thread;

        private static void Run()
        {
            PerformanceCounter cpuCounter = null;
            try
            {
                cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total", true);
            }
            catch
            {
            }
            try
            {
                if (cpuCounter == null)
                {
                    cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                }
            }
            catch
            {
            }
            do
            {
                if (cpuCounter != null)
                {
                    CpuUsage = cpuCounter.NextValue();
                }
            }
            while (!m_Event.WaitOne(1000));
        }

        public static void Start()
        {
            m_Event = new AutoResetEvent(false);
            m_Thread = new Thread(Run);
            m_Thread.Start();
        }

        public static void Stop()
        {
            m_Event.Set();
            m_Thread.Join(1000);
            m_Thread.Abort();
            m_Event.Close();
        }
    }
}