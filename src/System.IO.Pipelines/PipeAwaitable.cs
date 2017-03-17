// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    internal struct PipeAwaitable
    {
        private static readonly Action _awaitableIsCompleted = () => { };
        private static readonly Action _awaitableIsNotCompleted = () => { };
        private static readonly Action<object> _cancelOperation = (state) => { };

        private CancelledState _cancelledState;
        private Action _state;
        private CancellationToken _cancellationToken;
        private CancellationTokenRegistration _cancellationTokenRegistration;

        public PipeAwaitable(bool completed)
        {
            _cancelledState = CancelledState.NotCancelled;
            _state = completed ? _awaitableIsCompleted : _awaitableIsNotCompleted;
        }



        public void AttachToken(CancellationToken cancellationToken, Action<object> callback, object state)
        {
            if (cancellationToken != _cancellationToken)
            {
                _cancellationTokenRegistration.Dispose();
                _cancellationToken = cancellationToken;
                if (_cancellationToken != CancellationToken.None)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    _cancellationTokenRegistration = _cancellationToken.Register(callback, state);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Action Complete()
        {
            var awaitableState = _state;
            _state = _awaitableIsCompleted;

            if (!ReferenceEquals(awaitableState, _awaitableIsCompleted) &&
                !ReferenceEquals(awaitableState, _awaitableIsNotCompleted))
            {
                return awaitableState;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            if (ReferenceEquals(_state, _awaitableIsCompleted) &&
                _cancelledState != CancelledState.CancellationRequested)
            {
                _state = _awaitableIsNotCompleted;
            }

            // Change the state from observed -> not cancelled. We only want to reset the cancelled state if it was observed
            if (_cancelledState == CancelledState.CancellationObserved)
            {
                _cancelledState = CancelledState.NotCancelled;
            }
        }

        public bool IsCompleted => ReferenceEquals(_state, _awaitableIsCompleted);

        public Action OnCompleted(Action continuation, ref PipeCompletion completion)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var awaitableState = _state;
            if (ReferenceEquals(awaitableState, _awaitableIsNotCompleted))
            {
                _state = continuation;
            }

            if (ReferenceEquals(awaitableState, _awaitableIsCompleted))
            {
                return continuation;
            }
            else if (!ReferenceEquals(awaitableState, _awaitableIsNotCompleted))
            {
                completion.TryComplete(ThrowHelper.GetInvalidOperationException(ExceptionResource.NoConcurrentOperation));

                _state = _awaitableIsCompleted;

                Task.Run(continuation);
                Task.Run(awaitableState);
            }

            return null;
        }

        public Action Cancel()
        {
            _cancelledState = CancelledState.CancellationRequested;
            return Complete();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ObserveCancelation()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (_cancelledState == CancelledState.CancellationRequested)
            {
                _cancelledState = CancelledState.CancellationObserved;
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"CancelledState: {_cancelledState}, {nameof(IsCompleted)}: {IsCompleted}";
        }

        private enum CancelledState
        {
            NotCancelled = 0,
            CancellationRequested = 1,
            CancellationObserved = 2
        }
    }
}
