using System;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace ZBase.UnityScreenNavigator.Foundation.AssetLoaders
{
    public readonly struct AssetLoadHandleId : IEquatable<AssetLoadHandleId>
    {
        private readonly uint _value;

        public AssetLoadHandleId(uint value)
        {
            _value = value;
        }

        public bool Equals(AssetLoadHandleId other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            if (obj is AssetLoadHandleId other)
            {
                return _value == other._value;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public static implicit operator AssetLoadHandleId(uint value)
        {
            return new AssetLoadHandleId(value);
        }
    }

    public sealed class AssetLoadHandle<T>
        where T : Object
    {
        private Func<float> _percentCompleteFunc;

        public AssetLoadHandle(AssetLoadHandleId id)
        {
            Id = id;
        }

        public AssetLoadHandleId Id { get; }

        public bool IsDone => Status != AssetLoadStatus.None;

        public AssetLoadStatus Status { get; private set; }

        public float PercentComplete => _percentCompleteFunc.Invoke();

        public Exception OperationException { get; private set; }

        public T Result { get; private set; }

        public UniTask<T> Task { get; private set; }

        public void SetStatus(AssetLoadStatus status)
        {
            Status = status;
        }

        public void SetResult(T result)
        {
            Result = result;
        }

        public void SetPercentCompleteFunc(Func<float> percentComplete)
        {
            _percentCompleteFunc = percentComplete;
        }
        
        public void SetTask(UniTask<T> task)
        {
            Task = task;
        }

        public void SetOperationException(Exception ex)
        {
            OperationException = ex;
        }
    }
}