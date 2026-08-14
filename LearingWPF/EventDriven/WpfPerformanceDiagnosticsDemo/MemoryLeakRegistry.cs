using System;
using System.Collections.Generic;

namespace WpfPerformanceDiagnosticsDemo
{
    public static class MemoryLeakRegistry
    {
        // This static list will hold references to closed views or large objects, preventing GC from reclaiming them.
        public static List<object> LeakedObjects = new List<object>();

        public static void Clear()
        {
            LeakedObjects.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
