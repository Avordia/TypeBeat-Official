using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Online;
using TypeBeat.Game.Ui;

namespace TypeBeat.Game
{
    public partial class ExplorationScreen : Screen
    {
        private readonly HttpClient httpClient = new HttpClient();
        private FillFlowContainer<BeatpackCard> beatpacksContainer;
        private Container loadingContainer;
        private SpriteText statusText;
        private GameHost host;
        private TextureStore textures;
        private BasicScrollContainer scrollContainer;
        private AuthenticationService authService;
        private BeatpackManager beatpackManager;
        private string songsDirectory;
        private Container notificationContainer;
        
        private const string SUPABASE_URL = "https://fjxnfrdssccqzmapavch.supabase.co";
        private const string SUPABASE_ANON_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZqeG5mcmRzc2NjcXptYXBhdmNoIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjEyODcwMDAsImV4cCI6MjA3Njg2MzAwMH0.pxIcI-fDDxkqPTopSlbymo-9VweqIPpCZA17u1igHDk";
        
        [BackgroundDependencyLoader]
        private void load(GameHost host, TextureStore textures, AuthenticationService authService, BeatpackManager beatpackManager)
        {
            this.host = host;
            this.textures = textures;
            this.authService = authService;
            this.beatpackManager = beatpackManager;
            
            // Set up songs directory
            songsDirectory = Path.Combine(host.Storage.GetFullPath(""), "songs");
            if (!Directory.Exists(songsDirectory))
                Directory.CreateDirectory(songsDirectory);
            
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(20, 20, 30, 255)
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(20),
                    Children = new Drawable[]
                    {
                        // Header
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 80,
                            Children = new Drawable[]
                            {
                                new MenuButton("Back", Color4.Gray, 20f, null, () => this.Exit(), new Vector2(120, 40))
                                {
                                    Position = new Vector2(0, 20)
                                },
                                new SpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = "EXPLORE BEATPACKS",
                                    Font = new FontUsage("Kodchasan", size: 36, weight: "Bold"),
                                    Colour = Color4.White
                                }
                            }
                        },
                        // Beatpacks container
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = 100 },
                            Children = new Drawable[]
                                    {
                                        scrollContainer = new BasicScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = true,
                                            Child = beatpacksContainer = new FillFlowContainer<BeatpackCard>
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Direction = FillDirection.Full,
                                                Spacing = new Vector2(20, 20),
                                                Padding = new MarginPadding(10)
                                            }
                                        },
                                        loadingContainer = new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Alpha = 1,
                                            Children = new Drawable[]
                                            {
                                                new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Colour = new Color4(0, 0, 0, 200)
                                                },
                                                statusText = new SpriteText
                                                {
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Text = "Loading beatpacks...",
                                                    Font = new FontUsage(size: 24),
                                                    Colour = Color4.White
                                                }
                                            }
                                        }
                                    }
                                },
                        // Notification container
                        notificationContainer = new Container
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Padding = new MarginPadding(20)
                        }
                            }
                        }
            };
        }
        
        protected override void LoadComplete()
        {
            base.LoadComplete();
            Task.Run(async () => await loadBeatpacks());
        }
        
        private async Task loadBeatpacks()
        {
            try
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("apikey", SUPABASE_ANON_KEY);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SUPABASE_ANON_KEY}");
                
                // Query published beatpacks with profile information
                var response = await httpClient.GetAsync($"{SUPABASE_URL}/rest/v1/beatpacks?select=*,profiles!beatpacks_creator_id_fkey(username)");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Logger.Log($"[Load] Received beatpacks JSON: {json}", LoggingTarget.Network, LogLevel.Debug);
                    
                    var beatpacks = JsonConvert.DeserializeObject<List<BeatpackData>>(json);
                    Logger.Log($"[Load] Loaded {beatpacks?.Count ?? 0} beatpacks", LoggingTarget.Network, LogLevel.Important);
                    
                    Schedule(() =>
                    {
                        beatpacksContainer.Clear();
                        
                        foreach (var beatpack in beatpacks)
                        {
                            Logger.Log($"[Load] Beatpack: {beatpack.title} | File URL: {beatpack.beatpack_file_url ?? "NULL"}", LoggingTarget.Network, LogLevel.Debug);
                            
                            // Count beatmaps for this beatpack
                            Task.Run(async () =>
                            {
                                var beatmapResponse = await httpClient.GetAsync($"{SUPABASE_URL}/rest/v1/beatmaps?beatpack_id=eq.{beatpack.id}&select=id");
                                if (beatmapResponse.IsSuccessStatusCode)
                                {
                                    var beatmapJson = await beatmapResponse.Content.ReadAsStringAsync();
                                    var beatmaps = JsonConvert.DeserializeObject<List<object>>(beatmapJson);
                                    
                                    Schedule(() =>
                                    {
                                        var card = new BeatpackCard(beatpack, beatmaps?.Count ?? 0, downloadBeatpack);
                                        beatpacksContainer.Add(card);
                                    });
                                }
                            });
                        }
                        
                        loadingContainer.FadeOut(500);
                    });
                }
                else
                {
                    Logger.Log($"Failed to load beatpacks: {response.StatusCode}", LoggingTarget.Network, LogLevel.Error);
                    Schedule(() =>
                    {
                        statusText.Text = "Failed to load beatpacks";
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading beatpacks: {ex.Message}", LoggingTarget.Network, LogLevel.Error);
                Schedule(() =>
                {
                    statusText.Text = "Error connecting to server";
                });
            }
        }
        
        private async void downloadBeatpack(BeatpackData beatpack)
        {
            DownloadNotification notification = null;
            
            try
            {
                Logger.Log($"[Download] Starting download for: {beatpack.title} (ID: {beatpack.id})", LoggingTarget.Network, LogLevel.Important);
                Logger.Log($"[Download] Beatpack file URL: {beatpack.beatpack_file_url}", LoggingTarget.Network, LogLevel.Debug);
                
                var fileName = $"{beatpack.id}_{beatpack.title.Replace(" ", "_")}.tbbp";
                var filePath = Path.Combine(songsDirectory, fileName);
                
                Logger.Log($"[Download] Target file path: {filePath}", LoggingTarget.Network, LogLevel.Debug);
                
                // Check if already downloaded
                if (File.Exists(filePath))
                {
                    Logger.Log($"[Download] Beatpack already downloaded: {fileName}", LoggingTarget.Runtime, LogLevel.Important);
                    Schedule(() => showNotification($"Already downloaded: {beatpack.title}", Color4.Orange));
                    return;
                }
                
                if (string.IsNullOrEmpty(beatpack.beatpack_file_url))
                {
                    Logger.Log($"[Download] ERROR: beatpack_file_url is null or empty!", LoggingTarget.Network, LogLevel.Error);
                    Schedule(() => showNotification($"Error: No download URL for {beatpack.title}", Color4.Red));
                    return;
                }
                
                // Show download notification
                Schedule(() =>
                {
                    notification = new DownloadNotification(beatpack.title);
                    notificationContainer.Add(notification);
                });
                
                // Download the .tbbp file with progress
                Logger.Log($"[Download] Requesting file from: {beatpack.beatpack_file_url}", LoggingTarget.Network, LogLevel.Debug);
                var response = await httpClient.GetAsync(beatpack.beatpack_file_url, HttpCompletionOption.ResponseHeadersRead);
                
                Logger.Log($"[Download] Response status: {response.StatusCode}", LoggingTarget.Network, LogLevel.Debug);
                
                if (response.IsSuccessStatusCode)
                {
                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    var buffer = new byte[8192];
                    var bytesRead = 0L;
                    
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        int read;
                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            bytesRead += read;
                            
                            if (totalBytes > 0)
                            {
                                var progress = (float)bytesRead / totalBytes;
                                Schedule(() => notification?.SetProgress(progress));
                            }
                        }
                    }
                    
                    Logger.Log($"[Download] Downloaded {bytesRead} bytes", LoggingTarget.Network, LogLevel.Debug);
                    Logger.Log($"[Download] ✓ Successfully downloaded beatpack: {fileName}", LoggingTarget.Runtime, LogLevel.Important);
                    
                    // Show success and refresh
                    Schedule(() =>
                    {
                        notification?.SetComplete();
                        beatpackManager?.RefreshBeatpacks(host);
                    });
                    
                    // Auto-hide notification after 3 seconds
                    await Task.Delay(3000);
                    Schedule(() => notification?.FadeOut(500).Expire());
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Logger.Log($"[Download] Failed with status {response.StatusCode}: {errorContent}", LoggingTarget.Network, LogLevel.Error);
                    Schedule(() =>
                    {
                        notification?.SetError($"Download failed: {response.StatusCode}");
                        showNotification($"Failed to download {beatpack.title}", Color4.Red);
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[Download] Exception: {ex.Message}", LoggingTarget.Network, LogLevel.Error);
                Logger.Log($"[Download] Stack trace: {ex.StackTrace}", LoggingTarget.Network, LogLevel.Error);
                Schedule(() =>
                {
                    notification?.SetError(ex.Message);
                    showNotification($"Error downloading {beatpack.title}", Color4.Red);
                });
            }
        }
        
        private void showNotification(string message, Color4 color)
        {
            var notif = new Container
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                Masking = true,
                CornerRadius = 20,
                Alpha = 0,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = color
                    },
                    new SpriteText
                    {
                        Text = message,
                        Font = new FontUsage(size: 16),
                        Colour = Color4.White,
                        Padding = new MarginPadding { Horizontal = 20, Vertical = 10 }
                    }
                }
            };
            
            notificationContainer.Add(notif);
            notif.FadeIn(300);
            
            Task.Run(async () =>
            {
                await Task.Delay(3000);
                Schedule(() => notif.FadeOut(500).Expire());
            });
        }
        
        private class BeatpackData
        {
            public int id { get; set; }
            public string creator_id { get; set; }
            public string title { get; set; }
            public string artist { get; set; }
            public string description { get; set; }
            public string[] tags { get; set; }
            public string preview_image_url { get; set; }
            public string beatpack_file_url { get; set; }
            public ProfileData profiles { get; set; }
        }
        
        private class ProfileData
        {
            public string username { get; set; }
        }
        
        private partial class BeatpackCard : Container
        {
            private readonly BeatpackData beatpack;
            private readonly int beatmapCount;
            private readonly Action<BeatpackData> onDownload;
            private Box hoverBox;
            private bool isDownloaded;
            private Container imageContainer;
            private IRenderer renderer;
            
            public BeatpackCard(BeatpackData beatpack, int beatmapCount, Action<BeatpackData> onDownload)
            {
                this.beatpack = beatpack;
                this.beatmapCount = beatmapCount;
                this.onDownload = onDownload;
                
                Size = new Vector2(300, 400);
                Masking = true;
                CornerRadius = 10;
                
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(40, 40, 50, 255)
                    },
                    hoverBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(60, 60, 70, 0),
                        Alpha = 0
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            // Preview image 
                            imageContainer = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 200,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = new Color4(80, 80, 90, 255)
                                    },
                                    new SpriteText
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Text = "♪",
                                        Font = new FontUsage(size: 64),
                                        Colour = new Color4(120, 120, 140, 255)
                                    }
                                }
                            },
                            // Info section
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 200,
                                Padding = new MarginPadding(15),
                                Child = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 5),
                                    Children = new Drawable[]
                                    {
                                        new SpriteText
                                        {
                                            Text = beatpack.title,
                                            Font = new FontUsage("Kodchasan", size: 18, weight: "Bold"),
                                            Colour = Color4.White,
                                            Truncate = true,
                                            RelativeSizeAxes = Axes.X
                                        },
                                        new SpriteText
                                        {
                                            Text = $"by {beatpack.artist}",
                                            Font = new FontUsage(size: 14),
                                            Colour = new Color4(200, 200, 200, 255),
                                            Truncate = true,
                                            RelativeSizeAxes = Axes.X
                                        },
                                        new SpriteText
                                        {
                                            Text = $"Creator: {beatpack.profiles?.username ?? "Unknown"}",
                                            Font = new FontUsage(size: 12),
                                            Colour = new Color4(150, 150, 150, 255)
                                        },
                                        new SpriteText
                                        {
                                            Text = $"{beatmapCount} beatmap{(beatmapCount != 1 ? "s" : "")}",
                                            Font = new FontUsage(size: 12),
                                            Colour = new Color4(150, 150, 150, 255)
                                        },
                                        new Container
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Height = 40,
                                            Margin = new MarginPadding { Top = 20 },
                                            Child = new MenuButton(
                                                "Download", 
                                                new Color4(0, 200, 100, 255),
                                                16f,
                                                null,
                                                () => {
                                                    onDownload(beatpack);
                                                    markAsDownloaded();
                                                },
                                                new Vector2(150, 35)
                                            )
                                            {
                                                Anchor = Anchor.Centre,
                                                Origin = Anchor.Centre
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }
            
            [BackgroundDependencyLoader]
            private void load(IRenderer gameRenderer)
            {
                renderer = gameRenderer;
            }
            
            protected override void LoadComplete()
            {
                base.LoadComplete();
                
                // Load preview image if URL exists
                if (!string.IsNullOrEmpty(beatpack.preview_image_url))
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            using (var client = new HttpClient())
                            {
                                var imageBytes = await client.GetByteArrayAsync(beatpack.preview_image_url);
                                
                                Schedule(() =>
                                {
                                    using (var stream = new MemoryStream(imageBytes))
                                    {
                                        var texture = Texture.FromStream(renderer, stream);
                                        imageContainer.Clear();
                                        imageContainer.Add(new Sprite
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Texture = texture,
                                            FillMode = FillMode.Fill,
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre
                                        });
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"Failed to load image for {beatpack.title}: {ex.Message}", LoggingTarget.Network, LogLevel.Error);
                        }
                    });
                }
            }
            
            private void markAsDownloaded()
            {
                isDownloaded = true;
                // TODO: Update button visual state to show downloaded
            }
            
            protected override bool OnHover(HoverEvent e)
            {
                hoverBox.FadeTo(0.2f, 200);
                return base.OnHover(e);
            }
            
            protected override void OnHoverLost(HoverLostEvent e)
            {
                hoverBox.FadeOut(200);
                base.OnHoverLost(e);
            }
        }
    }
}
