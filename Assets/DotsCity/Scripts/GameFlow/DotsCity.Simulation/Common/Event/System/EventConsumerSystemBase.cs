using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Spirit604.DotsCity.Events
{
    public abstract partial class EventConsumerSystemBase : SimpleSystemBase
    {
        protected List<NativeStream> _triggerStreams;

        protected List<int> _forEachCounts;

        private bool registered;
        private JobHandle TriggerJobHandle;

        public void RegisterTriggerDependency(JobHandle triggerJobHandle)
        {
            if (!registered)
            {
                registered = true;
                TriggerJobHandle = triggerJobHandle;
            }
            else
            {
                TriggerJobHandle = JobHandle.CombineDependencies(triggerJobHandle, TriggerJobHandle);
            }
        }

        public NativeStream.Writer CreateConsumerWriter(int foreachCount)
        {
            var _effectStream = new NativeStream(foreachCount, Allocator.TempJob);
            _triggerStreams.Add(_effectStream);
            _forEachCounts.Add(foreachCount);

            return _effectStream.AsWriter();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            _triggerStreams = new List<NativeStream>();
            _forEachCounts = new List<int>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var stream in _triggerStreams)
            {
                if (stream.IsCreated)
                {
                    stream.Dispose(Dependency);
                }
            }
        }

        protected override void OnUpdate()
        {
            Dependency = JobHandle.CombineDependencies(Dependency, TriggerJobHandle);

            #region Make sure there is something to do

            // If the producer did not actually write anything to the stream, the native stream will
            // not be flaged as created. In that case we don't need to do anything and we can remove
            // the stream from the list of stream to process Not doing the IsCreated check actually
            // result in a non authrorized access to the memory and crashes Unity.
            for (int i = _triggerStreams.Count - 1; i >= 0; i--)
            {
                if (!_triggerStreams[i].IsCreated)
                {
                    _triggerStreams.RemoveAt(i);
                    _forEachCounts.RemoveAt(i);
                }
            }

            // if there are no stream left to process, do nothing
            if (_triggerStreams.Count == 0) { return; }

            #endregion Make sure there is something to do

            Consume();

            Clear();
        }

        protected abstract void Consume();

        private void Clear()
        {
            for (int i = 0; i < _triggerStreams.Count; i++)
            {
                _triggerStreams[i].Dispose(Dependency);
            }

            _triggerStreams.Clear();
            registered = false;
            TriggerJobHandle = default;
        }
    }
}
