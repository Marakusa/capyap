using CapYap.API.Models;

namespace CapYap.Models
{
    public class StatsChangedEventArgs : EventArgs
    {
        public Stats? Stats { get; }
        public bool Error { get; }
        public string? ErrorMessage { get; }

        public StatsChangedEventArgs(Stats? stats, bool error = false, string? errorMessage = null)
        {
            Stats = stats;
            Error = error;
            ErrorMessage = errorMessage;
        }
    }
}
