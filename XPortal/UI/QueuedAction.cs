using System;

namespace XPortal.UI
{
    internal static class QueuedAction
    {
        private static Action<bool> queuedAction;
        private static int queueDelay = -1;

        public static void Update()
        {
            if (queueDelay == 0)
            {
                Trigger();
            }
            else if (queueDelay > 0)
            {
                queueDelay--;
            }
        }

        public static void Queue(Action<bool> action, int delay = 2)
        {
            if (queuedAction != null)
            {
                throw new InvalidOperationException("Cannot queue two actions at the same time");
            }
            queueDelay = delay;
            queuedAction = action;
        }

        private static void Trigger()
        {
            if (queuedAction != null)
            {
                queueDelay = -1;
                queuedAction.Invoke(false);
                queuedAction = null;
            }
        }
    }
}
