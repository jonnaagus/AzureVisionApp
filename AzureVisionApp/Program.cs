using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Drawing.Imaging;
using System.Drawing;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;

// Elev: Jonna Gustafsson
// Klass: .NET23

namespace AzureVisionApp
{
    class Program
    {
        private static ComputerVisionClient cvClient;

        static async Task Main(string[] args)
        {
            PrintAsciiArt();
            PrintWelcomeMessage();

            try
            {
                // Hämta konfigurationsinställningar från AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string cogSvcEndpoint = configuration["Azure:ComputerVision:Endpoint"];
                string cogSvcKey = configuration["Azure:ComputerVision:ApiKey"];

                // Autentisera Computer Vision-klienten
                ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(cogSvcKey);
                cvClient = new ComputerVisionClient(credentials)
                {
                    Endpoint = cogSvcEndpoint
                };

                // Menyalternativ
                while (true)
                {
                    PrintMenu();
                    string choice = Console.ReadLine().Trim().ToLower();

                    switch (choice)
                    {
                        case "1":
                            Console.WriteLine("Ange sökvägen till bilden:");
                            string imageFile = Console.ReadLine();
                            await AnalyzeImageFromFile(imageFile);
                            break;
                        case "2":
                            Console.WriteLine("Ange URL till bilden:");
                            string imageUrl = Console.ReadLine();
                            await AnalyzeImageFromUrl(imageUrl);
                            break;
                        case "3":
                            Console.WriteLine("Avslutar programmet...");
                            return;
                        case "4":
                            PrintHelp();
                            break;
                        default:
                            Console.WriteLine("Ogiltigt val, vänligen försök igen.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ett fel inträffade: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("Kontrollera att bilden är korrekt och att URL:en är tillgänglig.");
            }
        }

        // En kamera i asciikod
        static void PrintAsciiArt()
        {
            Console.WriteLine(@"
      .-------------------.
     /--""--.------.------/|
     |     |__Ll__| [==] ||
     |     | .--. | """""""" ||
     |     |( () )|      ||
     |     | `--' |      |/
     `-----'------'------'
    ");
        }

        // Välkomsttext
        static void PrintWelcomeMessage()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Välkommen till Azure Vision App!");
            Console.ResetColor();
            Console.WriteLine("Denna applikation analyserar bilder och ger dig detaljerad information om dem.");
        }

        // Meny för användaren
        static void PrintMenu()
        {
            Console.WriteLine("Välj ett alternativ:");
            Console.WriteLine("1. Analysera en lokal fil");
            Console.WriteLine("2. Analysera en bild-URL");
            Console.WriteLine("3. Avsluta");
            Console.WriteLine("4. Hjälp");
        }

        // Vad analysen innehåller/går igenom
        static async Task AnalyzeImageFromFile(string imageFile)
        {
            Console.WriteLine("Analyserar bilden, vänligen vänta...");

            var features = new List<VisualFeatureTypes?>()
            {
                VisualFeatureTypes.Description,
                VisualFeatureTypes.Tags,
                VisualFeatureTypes.Categories,
                VisualFeatureTypes.Brands,
                VisualFeatureTypes.Objects,
                VisualFeatureTypes.Adult
            };

            try
            {
                using (var imageData = File.OpenRead(imageFile))
                {
                    var analysis = await cvClient.AnalyzeImageInStreamAsync(imageData, features);
                    await ProcessAnalysis(analysis);
                    await GetThumbnail(imageFile);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ett fel inträffade vid filanalys: {ex.Message}");
                Console.ResetColor();
            }
        }

        // Vad analysen innehåller/går igenom
        static async Task AnalyzeImageFromUrl(string imageUrl)
        {
            Console.WriteLine("Analyserar bilden, vänligen vänta...");

            var features = new List<VisualFeatureTypes?>()
            {
                VisualFeatureTypes.Description,
                VisualFeatureTypes.Tags,
                VisualFeatureTypes.Categories,
                VisualFeatureTypes.Brands,
                VisualFeatureTypes.Objects,
                VisualFeatureTypes.Adult
            };

            try
            {
                var analysis = await cvClient.AnalyzeImageAsync(imageUrl, features);
                await ProcessAnalysis(analysis);
                await GetThumbnailFromUrl(imageUrl);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ett fel inträffade vid URL-analys: {ex.Message}");
                Console.ResetColor();
            }
        }

        // Analysprocessen av bilden
        static async Task ProcessAnalysis(ImageAnalysis analysis)
        {
            if (analysis == null)
            {
                Console.WriteLine("Ingen analysresultat.");
                return;
            }

            if (analysis.Description?.Captions != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                foreach (var caption in analysis.Description.Captions)
                {
                    Console.WriteLine($"Beskrivning: {caption.Text} (tillförlitlighet: {caption.Confidence.ToString("P")})");
                }
                Console.ResetColor();
            }

            if (analysis.Tags?.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Taggar:");
                foreach (var tag in analysis.Tags)
                {
                    Console.WriteLine($" - {tag.Name} (tillförlitlighet: {tag.Confidence.ToString("P")})");
                }
                Console.ResetColor();
            }

            if (analysis.Categories?.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Kategorier:");
                foreach (var category in analysis.Categories)
                {
                    Console.WriteLine($" - {category.Name} (tillförlitlighet: {category.Score.ToString("P")})");
                }
                Console.ResetColor();
            }

            if (analysis.Brands?.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Varumärken:");
                foreach (var brand in analysis.Brands)
                {
                    Console.WriteLine($" - {brand.Name} (tillförlitlighet: {brand.Confidence.ToString("P")})");
                }
                Console.ResetColor();
            }

            if (analysis.Objects?.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Objekt i bilden:");
                foreach (var detectedObject in analysis.Objects)
                {
                    Console.WriteLine($" - {detectedObject.ObjectProperty} (tillförlitlighet: {detectedObject.Confidence.ToString("P")})");
                }
                Console.ResetColor();
            }

            if (analysis.Adult != null)
            {
                string ratings = $"Bedömningar:\n -Vuxet: {analysis.Adult.IsAdultContent}\n -Racy: {analysis.Adult.IsRacyContent}\n -Blodigt: {analysis.Adult.IsGoryContent}";
                Console.WriteLine(ratings);
            }
        }

        // Genererar miniatyrbild genom fil
        static async Task GetThumbnail(string imageFile)
        {
            Console.WriteLine("Genererar miniatyrbild...");

            try
            {
                Console.WriteLine("Ange storlek på miniatyrbilden (t.ex. 100x100):");
                string sizeInput = Console.ReadLine();
                var dimensions = sizeInput.Split('x');
                int width = int.Parse(dimensions[0]);
                int height = int.Parse(dimensions[1]);

                using (var imageData = File.OpenRead(imageFile))
                {
                    var thumbnailStream = await cvClient.GenerateThumbnailInStreamAsync(width, height, imageData, true);
                    string thumbnailFileName = $"thumbnail_{width}x{height}.png";
                    using (Stream thumbnailFile = File.Create(thumbnailFileName))
                    {
                        await thumbnailStream.CopyToAsync(thumbnailFile);
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Miniatyrbild sparad i {thumbnailFileName}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ett fel inträffade: {ex.Message}");
                Console.ResetColor();
            }
        }

        // Genererar miniatyrbild genom URL
        static async Task GetThumbnailFromUrl(string imageUrl)
        {
            Console.WriteLine("Genererar miniatyrbild...");

            try
            {
                Console.WriteLine("Ange storlek på miniatyrbilden (t.ex. 100x100):");
                string sizeInput = Console.ReadLine();
                var dimensions = sizeInput.Split('x');
                int width = int.Parse(dimensions[0]);
                int height = int.Parse(dimensions[1]);

                using (HttpClient httpClient = new HttpClient())
                {
                    using (Stream imageStream = await httpClient.GetStreamAsync(imageUrl))
                    {
                        using (var image = Image.FromStream(imageStream))
                        {
                            var thumbnail = image.GetThumbnailImage(width, height, () => false, IntPtr.Zero);

                            string thumbnailFileName = $"thumbnail_{width}x{height}.png";
                            using (var thumbnailStream = new FileStream(thumbnailFileName, FileMode.Create))
                            {
                                thumbnail.Save(thumbnailStream, ImageFormat.Png);
                            }

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Miniatyrbild sparad i {thumbnailFileName}");
                            Console.ResetColor();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ett fel inträffade: {ex.Message}");
                Console.ResetColor();
            }
        }

        // Hjälpsektion för att förstå applikationen bättre
        static void PrintHelp()
        {
            Console.WriteLine("Hjälpsektion:");
            Console.WriteLine("1. Skriv '1' för att analysera en lokal bildfil. Du behöver ange den fullständiga sökvägen till filen.");
            Console.WriteLine("2. Skriv '2' för att analysera en bild via en URL. Se till att URL:en är korrekt och att bilden är tillgänglig.");
            Console.WriteLine("3. Skriv '3' för att avsluta applikationen.");
            Console.WriteLine("4. Skriv '4' för att visa denna hjälpsektion.");
        }
    }
}
