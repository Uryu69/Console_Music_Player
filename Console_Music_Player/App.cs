using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static System.Console;


namespace Console_Music_Player
{
    class App
    {
        static MusicManager musicManager = new MusicManager();
        public static async Task Main(string[] args)
        {
            App app = new App();
            await app.Start();
            //OutputEncoding = Encoding.UTF8;
        }
        public async Task Start()
        {
            Title = "Console Music Player";
            await RunMainMenu();
            

        }

        public async Task RunMainMenu()
        {
            bool isRunning = true;
            while (isRunning)
            {
                Clear();


                string prompt = @"
  __  __           _        ____  _                       
 |  \/  |_   _ ___(_) ___  |  _ \| | __ _ _   _  ___ _ __ 
 | |\/| | | | / __| |/ __| | |_) | |/ _` | | | |/ _ \ '__|
 | |  | | |_| \__ \ | (__  |  __/| | (_| | |_| |  __/ |   
 |_|  |_|\__,_|___/_|\___| |_|   |_|\__,_|\__, |\___|_|   
                                          |___/           
Welcome to my Console Music Player
(Press ↑ or ↓ to navigate)";
                string[] options = {
                    "Buscar y Reproducir",
                    "Ver Cola de Reproducción",
                    "Pausar",
                    "Continuar",
                    "Siguiente",
                    "Salir"
                };
                Menu mainMenu = new Menu(prompt, options);
                int selectedIndex = mainMenu.Run();
                


                switch (selectedIndex)
                {
                    case 0:
                        await SearchAndPlay();
                        
                        break;
                    case 1:
                        ShowQueue();
                        break;
                    case 2:
                        musicManager.Pause();
                        break;
                    case 3:
                        musicManager.Resume();
                        break;
                    case 4:
                       await musicManager.PlayNext();
                       break;
                    case 5:
                        musicManager.StopCurrent();
                        exitApp();
                        isRunning = false;
                        break;
                }
                if (isRunning)
                {
                    WriteLine("\nPresiona cualquier tecla para volver atrás...");
                    ReadKey(true);
                }
            }
        }
        private void exitApp()
        {
            WriteLine("Exiting. Goodbye!");
            Environment.Exit(0);
        }
        private async Task SearchAndPlay()
        {
            /*
            Clear();
            CursorVisible = true;
            */
            bool stayingInSeachMode = true;
            while (stayingInSeachMode) {
                Clear();
                CursorVisible = true;

                // Prompt user for search query
                Console.ForegroundColor = ConsoleColor.Cyan;
                WriteLine("=== Buscador ===");
                WriteLine("Escribe tu consulta de búsqueda o deja vacío para volver al menú principal.");
                Console.ResetColor();

                // Get user input
                Write("Buscar: ");
                string query = ReadLine();
                CursorVisible = false;

                // If the user input is empty, exit search mode
                if (string.IsNullOrWhiteSpace(query)) return;

                // Search for videos
                var results = await musicManager.SearchVideos(query);

                if (results.Count == 0)
                {
                    WriteLine($"No se encontraron resultados para {query}. Presiona una tecla para volver a intentar");
                    ReadKey(true);
                    continue;
                }

                bool browsingResults = true;

                while (browsingResults)
                {
                    // Display search results
                    string[] resultOptions = new string[results.Count + 2];

                    for (int i = 0; i < results.Count; i++)
                    {
                        resultOptions[i] = $"{results[i].Title} | {results[i].Duration}";
                    }

                    resultOptions[results.Count] = "[Nueva Búsqueda]";
                    resultOptions[results.Count + 1] = "[ ← Volver al Menú Principal]";

                    // Create and run the search results menu
                    Menu searchMenu = new Menu($"Resultados para: '{query}'", resultOptions);
                    int choice = searchMenu.Run();

                    // Handle user choice
                    if (choice < results.Count)
                    {
                        // CASE A: User selected a video to play
                        var selectedVideo = results[choice];
                        await musicManager.AddToQueue(selectedVideo);

                        // Inform the user
                        Console.ForegroundColor = ConsoleColor.Green;
                        WriteLine("\nAñadido a la cola: " + selectedVideo.Title + ". Puede seguir seleccionando.");
                        Console.ResetColor();
                        Thread.Sleep(1000); // Pause for a moment to show the message

                    }
                    else if (choice == results.Count)
                    {
                        // CASE B: User wants to perform a new search
                        browsingResults = false; // Break out to the outer loop to start a new search
                    }
                    else
                    {
                        // CASE C: User wants to return to the main menu
                        browsingResults = false;
                        stayingInSeachMode = false; // Exit both loops
                    }

                }
            }
            

            

            

            

            

            
            

            

            
            

        }

        private void ShowQueue()
        {
            Clear();
            WriteLine("=== Cola de Reproducción ===");
            int i = 1;
            foreach(var video in musicManager.playlist)
            {
                WriteLine($"{i}. {video.Title} ({video.Duration})");
                i++;
            }
            if (musicManager.playlist.Count == 0) WriteLine("La cola está vacía.");
        }


    }
}
