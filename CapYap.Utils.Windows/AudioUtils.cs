using System.IO;
using System.Reflection;
using System.Windows.Media;

namespace CapYap.Utils
{
    public enum AudioClip
    {
        Capture,
        Complete,
    }

    public class AudioUtils
    {
        private readonly MediaPlayer[] _players;
        private readonly string[] _audioFiles = [
            "cap.wav",
            "complete.wav",
        ];

        public AudioUtils()
        {
            List<MediaPlayer> players = new List<MediaPlayer>();

            string assemblyDirectory = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName ?? ".";

            foreach (var file in _audioFiles)
            {
                Uri uri = new Uri(Path.Join(assemblyDirectory, "Assets", "sounds", file));
                MediaPlayer player = new MediaPlayer();
                player.Open(uri);
                player.Stop();
                players.Add(player);
            }

            _players = players.ToArray();
        }

        public void PlayAudioClip(AudioClip clip)
        {
            var player = _players[(int)clip];
            player.Stop();
            player.Position = new TimeSpan(0);
            player.Play();
        }
    }
}
