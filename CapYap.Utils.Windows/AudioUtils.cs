using System.IO;
using System.Reflection;
using System.Windows.Media;

namespace CapYap.Utils.Windows
{
    public enum AudioClip
    {
        Capture,
        Complete,
    }

    public class AudioUtils
    {
        private readonly Dictionary<AudioClip, string> _fileMap = new();
        private readonly Dictionary<AudioClip, MediaPlayer> _players = new();

        public AudioUtils()
        {
            string assemblyDirectory =
                new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName ?? ".";

            string soundsDirectory = Path.Combine(assemblyDirectory, "Assets", "sounds");

            var defaultMap = new Dictionary<AudioClip, string>
            {
                { AudioClip.Capture, "cap.wav" },
                { AudioClip.Complete, "complete.wav" },
            };

            foreach ((AudioClip id, string file) in defaultMap)
            {
                string fullPath = Path.Combine(soundsDirectory, file);
                _fileMap[id] = fullPath;
            }
        }

        public void PlayAudioClip(AudioClip clip)
        {
            try
            {
                if (!_fileMap.TryGetValue(clip, out var path) || !File.Exists(path))
                {
                    return;
                }

                if (!_players.TryGetValue(clip, out var player))
                {
                    player = new MediaPlayer();
                    player.Open(new Uri(path));
                    _players[clip] = player;
                }

                player.Stop();
                player.Position = TimeSpan.Zero;
                player.Play();
            }
            catch
            {
                return;
            }
        }
    }
}
