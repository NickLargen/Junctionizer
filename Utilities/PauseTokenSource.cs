using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

#pragma warning disable 420

namespace Utilities
{
    public class PauseTokenSource : INotifyPropertyChanged
    {
        public PauseToken Token => new PauseToken(this);

        private volatile TaskCompletionSource<bool> _paused;

        public bool IsPaused
        {
            get => _paused != null;
            set {
                if (value)
                {
                    if (Interlocked.CompareExchange(ref _paused, new TaskCompletionSource<bool>(), null) == null)
                    {
                        RaisePropertyChanged();
                    }
                }
                else
                {
                    while (true)
                    {
                        var tcs = _paused;
                        if (tcs == null) return;

                        if (Interlocked.CompareExchange(ref _paused, null, tcs) == tcs)
                        {
                            tcs.SetResult(true);
                            RaisePropertyChanged();
                            break;
                        }
                    }
                }
            }
        }

        internal Task WaitWhilePausedAsync()
        {
            var cur = _paused;
            return cur != null ? cur.Task : Task.CompletedTask;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public struct PauseToken
    {
        private readonly PauseTokenSource _source;

        internal PauseToken(PauseTokenSource source)
        {
            _source = source;
        }

        public bool IsPaused => _source?.IsPaused == true;

        public Task WaitWhilePausedAsync() => _source.WaitWhilePausedAsync();
    }
}
