using System;
using System.Collections.Generic;
using System.Linq;

namespace XPortal
{
    internal class QueuedAction
    {
        private static readonly Dictionary<Guid, QueuedAction> queuedActions;

        static QueuedAction()
        {
            queuedActions = new Dictionary<Guid, QueuedAction>();
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
            var newAction = new QueuedAction(action, delay);
            queuedActions.Add(Guid.NewGuid(), newAction);
        }

        private static void Trigger()
        {
            var readyActions = queuedActions.Where(kvp => kvp.Value.Delay == 0).ToList();
            foreach (var readyAction in readyActions)
            {
                queuedActions.Remove(readyAction.Key);
                readyAction.Value.Action.Invoke(false);
            }
        }

        private static void Countdown()
        {
            foreach (var queuedAction in queuedActions.Keys.ToList())
            {
                queuedActions[queuedAction].Delay--;
            }
        }

        private static void Cleanup()
        {
            var processedActions = queuedActions.Where(kvp => kvp.Value.Delay < 0).ToList();
            foreach (var processedAction in processedActions)
            {
                Log.Warning($"Cleaned up stale action {processedAction.Value.Action.Method.Name}");
                queuedActions.Remove(processedAction.Key);
            }
        }

        private Action<bool> Action;
        private int Delay;

        private QueuedAction(Action<bool> action, int delay)
        {
            Action = action;
            Delay = delay;
        }
    }
}
