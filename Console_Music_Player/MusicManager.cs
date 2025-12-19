using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Common;
using NAudio.Wave;
using System.Runtime.Serialization;

namespace Console_Music_Player
{
    public class MusicManager
    {
        private YoutubeClient youtube;
        private IWavePlayer waveOut;
        private MediaFoundationReader audioReader;
        //private bool isPaused = false;
        private bool isManualStop = false;
        public IVideo CurrentTrack { get; private set; }

        //New: Discovery mode
        public bool IsDiscoverMode { get; set; } = false;

        //Cola de reproducción
        public Queue<IVideo> playlist = new Queue<IVideo>();
        public bool Isplaying => waveOut != null && waveOut.PlaybackState == PlaybackState.Playing;

        public MusicManager()
        {
            youtube = new YoutubeClient();
        }

        //Buscar videos por palabras clave
        public async Task<List<IVideo>>SearchVideos(string query)
        {
            Console.WriteLine("Buscando...");

            var videos = await youtube.Search.GetVideosAsync(query).CollectAsync(10);
            return new List<IVideo>(videos);
        }

        public async Task AddToQueue(IVideo video)
        {
            playlist.Enqueue(video);
            Console.WriteLine($"\n[+] Agregado a la cola: {video.Title} {video.Duration}");

            if (!Isplaying && playlist.Count == 1)
            {
                await PlayNext();
            }
        }

        public async Task PlayNext()
        {
            if (playlist.Count > 0)
            {
                var video = playlist.Peek();

                try {
                    StopCurrent();

                    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
                    var streamInfo = streamManifest.GetMuxedStreams().Where(s => s.Container == Container.Mp4).GetWithHighestBitrate();
                   
                    if (streamInfo == null)
                    {
                        Console.WriteLine("Audio puro no compatible. Cambiando a modo compatibilidad...");
                        streamInfo = streamManifest.GetAudioOnlyStreams()
                                                   .Where(s => s.Container == Container.Mp4)
                                                   .GetWithHighestBitrate();
                    }

                    if (streamInfo != null)
                    {
                        audioReader = new MediaFoundationReader(streamInfo.Url);
                        waveOut = new WaveOutEvent();
                        waveOut.PlaybackStopped += OnPlaybackStopped;
                        waveOut.Init(audioReader);
                        isManualStop = false;
                        waveOut.Play();

                        
                        CurrentTrack = video;
                        playlist.Dequeue();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\nReproduciendo ahora: {video.Title}");
                        Console.WriteLine($"Duración: {video.Duration}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\nError: No se encontró un formato compatible para '{video.Title}'");
                        Console.ForegroundColor = ConsoleColor.White;
                        playlist.Dequeue(); // Lo sacamos para que no se trabe
                        await PlayNext();   // Intentamos con el siguiente
                    }

                } catch (Exception ex){
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nError al reproducir: {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.White;

                    // Si falla, sacamos la canción problemática y probamos la siguiente
                    if (playlist.Count > 0) playlist.Dequeue();
                    await PlayNext();
                }
            }
            else
            {
                CurrentTrack = null;
                Console.WriteLine("La cola de reprodución está vacía.");
            }
        }
        public void Pause() {
            if (waveOut != null)
            {
                try
                {
                    waveOut.Pause();
                    Console.WriteLine("Música Pausada."); // Feedback visual para saber que entró aquí
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al pausar: {ex.Message}");
                }
            }
        }
        public void Resume() { waveOut?.Play(); }

        public void StopCurrent()
        {
            if (waveOut != null)
            {
                isManualStop = true;
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
                Console.Clear();


            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            /*
            if(!isManualStop && playlist.Count > 0)
            {
                Console.WriteLine("\nLa canción terminó. Reproduciendo siguiente...");
                Task.Run(() => PlayNext());
            }
            else if(!isManualStop && playlist.Count == 0)
            {
                Console.WriteLine("\nLista de reprodución finalizada.");
                CurrentTrack = null;
            }
            */
            if (isManualStop) return; // If it was a manual stop, do nothing

            if (playlist.Count > 0)
            {
                //normal behavior: There are songs in the playlist
                Console.WriteLine("\nLa canción terminó. Reproduciendo siguiente...");
                Task.Run(() => PlayNext());
            }
            else if (IsDiscoverMode && CurrentTrack != null)
            {
                // Discovery mode: Get related videos
                Console.WriteLine("\nModo Descubrimiento activo. Buscando canciones relacionadas...");
                Task.Run(() => PlaySimilarSong());
            }
            else
            {
                Console.WriteLine("\nLista de reprodución finalizada.");
                CurrentTrack = null;
            }
        }

        //New Method : Play Similar Songs
        public async Task PlaySimilarSong()
        {
            try {

                //strategy: Get the video's details and fetch related videos
                string query = CurrentTrack.Author.ChannelTitle;

                // Fetch related videos
                var videos = await youtube.Search.GetVideosAsync(query).CollectAsync(20);

                if (videos.Count > 0)
                {
                    // Pick a random video from the related videos
                    Random random = new Random();
                    var randomVideo = videos[random.Next(videos.Count)];

                    // Add to queue and play
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"\n[Modo Descubrimiento] Reproduciendo canción relacionada: {randomVideo.Title} | {randomVideo.Duration}");
                    Console.ResetColor();

                    await AddToQueue(randomVideo);
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error en Modo Descubrimiento: {ex.Message}");
            }
        }

        /*
        {
            var searchResults = await youtube.Search.GetVideosAsync(query).CollectAsync(10);
            return searchResults.ToList();
        }
        */

        /*
        public async Task PlayFromYoutube(string query)
        {
            try
            {
                Stop();

                Console.WriteLine($"Buscando {query} en youtube...");

                var video = await youtube.Videos.GetAsync(query);

                Console.WriteLine($"Reproduciendo: {video.Title}");

                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

                var streamInfo = streamManifest.GetAudioOnlyStreams().Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.Mp4).GetWithHighestBitrate();

                if (streamInfo == null)
                {
                    Console.WriteLine("No se encontraron streams de audio.");
                    return;
                }

                audioReader = new MediaFoundationReader(streamInfo.Url);
                waveOut = new WaveOutEvent();
                waveOut.Init(audioReader);
                waveOut.Play();
                isPaused = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al reproducir desde YouTube: {ex.Message}");
            }
        }*/

        /*
        public void Pause()
        {
            if (waveOut != null && waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Pause();
                isPaused = true;
                Console.WriteLine("Música pausada.");
            }
        }

        public void Resume()
        {
            if (waveOut != null && isPaused)
            {
                waveOut.Play();
                isPaused = false;
                Console.WriteLine("Música reanudada.");
            }
        }

        public void Stop()
        {
            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
            }
            if (audioReader != null)
            {
                audioReader.Dispose();
                audioReader = null;
            }
            isPaused = false;
            Console.WriteLine("Música detenida.");
        }
        */

    }

}
