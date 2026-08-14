using System;

namespace WpfPerformanceDiagnosticsDemo
{
    public static class GlobalEventPublisher
    {
        public static event EventHandler DataUpdated;

        public static void RaiseDataUpdated()
        {
            DataUpdated?.Invoke(null, EventArgs.Empty);
        }

        public static int GetSubscriberCount()
        {
            if (DataUpdated == null) return 0;
            return DataUpdated.GetInvocationList().Length;
        }

        public static void ClearSubscribers()
        {
            DataUpdated = null;
        }
    }
}
