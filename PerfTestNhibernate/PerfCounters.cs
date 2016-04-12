using System.Collections.Generic;
using System.Diagnostics;

namespace PerfTestNhibernate
{
    public static class PerfCounters
    {
        private static readonly string instance = Process.GetCurrentProcess().ProcessName;
        private static readonly IDictionary<string, PerformanceCounter> performanceCounters = new Dictionary<string, PerformanceCounter>(); 

        public static float GetPerformanceCounterValue(string category, string counter)
        {
            string cacheKey = string.Format("{0}-{1}-{2}", instance, category, counter);

            PerformanceCounter performanceCounter;
            if (!performanceCounters.TryGetValue(cacheKey, out performanceCounter))
            {
                if (PerformanceCounterCategory.Exists(category) &&
                    PerformanceCounterCategory.CounterExists(counter, category) &&
                    PerformanceCounterCategory.InstanceExists(instance, category))
                {
                    performanceCounter = new PerformanceCounter(category, counter, instance);
                    performanceCounters[cacheKey] = performanceCounter;
                }
            }

            return performanceCounter == null ? 0 : performanceCounter.NextValue();
        }
    }
}