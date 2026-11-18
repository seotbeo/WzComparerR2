using System;
using System.Collections.Generic;

namespace WzComparerR2.MapRender
{
    /// <summary>
    /// One-shot timers advanced by the MapRender scene update on the render thread.
    /// </summary>
    public class MapRenderTimer
    {
        private readonly List<Handle> timers = new List<Handle>();
        private TimeSpan currentTime;

        /// <summary>
        /// Registers a callback for a future update. The callback receives the time past its deadline.
        /// Timers registered during a callback begin processing on the next update.
        /// </summary>
        public Handle Register(TimeSpan delay, Action<TimeSpan> callback, bool removeOnMapChange = true)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            var handle = new Handle(this.currentTime + (delay < TimeSpan.Zero ? TimeSpan.Zero : delay), callback, removeOnMapChange);
            this.timers.Add(handle);
            return handle;
        }

        public void Unregister(Handle handle)
        {
            if (handle != null && this.timers.Remove(handle))
            {
                handle.Callback = null;
            }
        }

        public void Update(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            }
            this.currentTime += elapsed;
            // A callback may register, cancel, or clear timers while this snapshot is processed.
            foreach (var handle in this.timers.ToArray())
            {
                if (handle.Callback != null && handle.Deadline <= this.currentTime)
                {
                    var callback = handle.Callback;
                    Unregister(handle);
                    callback(this.currentTime - handle.Deadline);
                }
            }
        }

        public void OnMapChange()
        {
            for (int i = this.timers.Count - 1; i >= 0; i--)
            {
                if (this.timers[i].RemoveOnMapChange)
                {
                    Unregister(this.timers[i]);
                }
            }
        }

        public void Clear()
        {
            foreach (var handle in this.timers)
            {
                handle.Callback = null;
            }
            this.timers.Clear();
        }

        public sealed class Handle
        {
            internal Handle(TimeSpan deadline, Action<TimeSpan> callback, bool removeOnMapChange)
            {
                this.Deadline = deadline;
                this.Callback = callback;
                this.RemoveOnMapChange = removeOnMapChange;
            }

            internal TimeSpan Deadline { get; }
            internal Action<TimeSpan> Callback { get; set; }
            public bool RemoveOnMapChange { get; }
            public bool IsActive => this.Callback != null;
        }
    }
}
