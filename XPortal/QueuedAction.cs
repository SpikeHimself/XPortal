using System;
using System.Collections.Generic;
using System.Linq;

namespace XPortal
{
    internal static class QueuedAction
    {
        private static readonly Dictionary<Action<bool>, int> queuedActions;

        static QueuedAction()
        {
            queuedActions = new Dictionary<Action<bool>, int>();
        }

        public static void Update()
        {
            if (queuedActions.Count > 0)
            {
                Trigger();
                Countdown();
                Cleanup();
            }
        }

        public static void Queue(Action<bool> action, int delay = 2)
        {
            queuedActions.Add(action, delay);
        }

        private static void Trigger()
        {
            var readyActions = queuedActions.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key).ToList();
            foreach (var readyAction in readyActions)
            {
                readyAction.Invoke(false);
                queuedActions.Remove(readyAction);
            }
        }

        private static void Countdown()
        {
            foreach (var queuedAction in queuedActions.Keys.ToList())
            {
                queuedActions[queuedAction]--;
            }
        }

        private static void Cleanup()
        {
            var processedActions = queuedActions.Where(kvp => kvp.Value < 0).Select(kvp => kvp.Key);
            foreach (var processedAction in processedActions.ToList())
            {
                Log.Warning($"Cleaned up stale action {processedAction.Method.Name}");
                queuedActions.Remove(processedAction);
            }
        }
    }
}
