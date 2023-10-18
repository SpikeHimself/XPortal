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

        public static void Queue(Action<bool, object> action, int delay = 2, object state = null)
        {
            var newAction = new QueuedAction(action, delay, state);
            queuedActions.Add(Guid.NewGuid(), newAction);
        }

        private static void Trigger()
        {
            var readyActions = queuedActions.Where(kvp => kvp.Value.Delay == 0).ToList();
            foreach (var readyAction in readyActions)
            {
                queuedActions.Remove(readyAction.Key);
                readyAction.Value.Action.Invoke(false, readyAction.Value.State);
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

        private readonly Action<bool, object> Action;
        private int Delay;
        private object State;

        private QueuedAction(Action<bool, object> action, int delay, object state = null)
        {
            Action = action;
            Delay = delay;
            State = state;
        }
    }
}
