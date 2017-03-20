using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class PipeCancellationTokenTests
    {
        private readonly IPipe _pipe;
        private readonly PipeFactory _pipeFactory;

        public PipeCancellationTokenTests()
        {
            _pipeFactory = new PipeFactory();
            _pipe = _pipeFactory.Create(new PipeOptions()
            {
                MaximumSizeHigh = 65,
                MaximumSizeLow = 6
            });
        }

        public void Dispose()
        {
            _pipe.Writer.Complete();
            _pipe.Reader.Complete();
            _pipeFactory.Dispose();
        }

        [Fact]
        public void GetResultThrowsIfReadAsyncCancelledAfterOnCompleted()
        {
            var onCompletedCalled = false;
            var cancellationTokenSource = new CancellationTokenSource();

            var awaiter = _pipe.Reader.ReadAsync(cancellationTokenSource.Token);
            var awaiterIsCompleted = awaiter.IsCompleted;
            awaiter.OnCompleted(() =>
            {
                onCompletedCalled = true;
                Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
            });

            cancellationTokenSource.Cancel();

            Assert.False(awaiterIsCompleted);
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void GetResultThrowsIfReadAsyncCancelledBeforeOnCompleted()
        {
            var onCompletedCalled = false;
            var cancellationTokenSource = new CancellationTokenSource();

            var awaiter = _pipe.Reader.ReadAsync(cancellationTokenSource.Token);
            var awaiterIsCompleted = awaiter.IsCompleted;

            cancellationTokenSource.Cancel();

            awaiter.OnCompleted(() =>
            {
                onCompletedCalled = true;
                Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
            });


            Assert.False(awaiterIsCompleted);
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void ReadAsyncThrowsIfPassedCancelledCancellationToken()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            Assert.Throws<OperationCanceledException>(() => _pipe.Reader.ReadAsync(cancellationTokenSource.Token));
        }

        [Fact]
        public void GetResultThrowsIfFlushAsyncCancelledAfterOnCompleted()
        {
            var onCompletedCalled = false;
            var cancellationTokenSource = new CancellationTokenSource();
            var buffer = _pipe.Writer.Alloc(65);
            buffer.Advance(65);

            var awaiter = buffer.FlushAsync(cancellationTokenSource.Token);

            awaiter.OnCompleted(() =>
            {
                onCompletedCalled = true;
                Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
            });

            var awaiterIsCompleted = awaiter.IsCompleted;

            cancellationTokenSource.Cancel();

            Assert.False(awaiterIsCompleted);
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void GetResultThrowsIfFlushAsyncCancelledBeforeOnCompleted()
        {
            var onCompletedCalled = false;
            var cancellationTokenSource = new CancellationTokenSource();
            var buffer = _pipe.Writer.Alloc(65);
            buffer.Advance(65);

            var awaiter = buffer.FlushAsync(cancellationTokenSource.Token);
            var awaiterIsCompleted = awaiter.IsCompleted;

            cancellationTokenSource.Cancel();

            awaiter.OnCompleted(() =>
            {
                onCompletedCalled = true;
                Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
            });

            Assert.False(awaiterIsCompleted);
            Assert.True(onCompletedCalled);
        }


        [Fact]
        public void GetResultThrowsIfFlushAsyncCancellationTokenCancelled()
        {
            var onCompletedCalled = false;
            var cancellationTokenSource = new CancellationTokenSource();
            var buffer = _pipe.Writer.Alloc(65);
            buffer.Advance(65);

            var awaiter = buffer.FlushAsync(cancellationTokenSource.Token);

            awaiter.OnCompleted(() =>
            {
                onCompletedCalled = true;
                Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
            });

            var awaiterIsCompleted = awaiter.IsCompleted;

            cancellationTokenSource.Cancel();

            Assert.False(awaiterIsCompleted);
            Assert.True(onCompletedCalled);
        }

        [Fact]
        public void FlushAsyncThrowsIfPassedCancelledCancellationToken()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var buffer = _pipe.Writer.Alloc();

            Assert.Throws<OperationCanceledException>(() => buffer.FlushAsync(cancellationTokenSource.Token));
        }


        [Fact]
        public async Task ReadAsyncWithNewCancellationTokenNotAffectedByPrevious()
        {
            await _pipe.Writer.WriteAsync(new byte[] { 0 } );

            var cancellationTokenSource1 = new CancellationTokenSource();
            var result = await _pipe.Reader.ReadAsync(cancellationTokenSource1.Token);
            _pipe.Reader.Advance(result.Buffer.Start);

            cancellationTokenSource1.Cancel();
            var cancellationTokenSource2 = new CancellationTokenSource();

            // Verifying that ReadAsync does not throw
            result = await _pipe.Reader.ReadAsync(cancellationTokenSource2.Token);
            _pipe.Reader.Advance(result.Buffer.Start);
        }


        [Fact]
        public async Task FlushAsyncWithNewCancellationTokenNotAffectedByPrevious()
        {
            var cancellationTokenSource1 = new CancellationTokenSource();
            var buffer = _pipe.Writer.Alloc(10);
            buffer.Advance(10);
            await buffer.FlushAsync(cancellationTokenSource1.Token);

            cancellationTokenSource1.Cancel();

            var cancellationTokenSource2 = new CancellationTokenSource();
            buffer = _pipe.Writer.Alloc(10);
            buffer.Advance(10);
            // Verifying that ReadAsync does not throw
            await buffer.FlushAsync(cancellationTokenSource2.Token);
        }


    }
}
