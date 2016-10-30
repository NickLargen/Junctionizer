using GameMover.ViewModels;

namespace WpfUIExperiments
{
    public class TrivialViewModel
    {
        /// <inheritdoc/>
        public TrivialViewModel() {}

        public static AsyncObservableKeyedSet<int, int> coll { get; } = new AsyncObservableKeyedSet<int, int>(item => item);
//        public static ObservableCollection<int> coll { get; } = new ObservableCollection<int>();
    }
}
