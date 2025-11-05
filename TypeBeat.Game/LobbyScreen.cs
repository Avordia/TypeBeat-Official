using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Online;
using TypeBeat.Game.Ui;

namespace TypeBeat.Game
{
    public partial class LobbyScreen : Screen
    {
        private readonly Room room;
        private readonly MultiplayerService multiplayerService;
        private AuthenticationService authService;
        private BeatpackManager beatpackManager;
        
        private FillFlowContainer playersContainer;
        private Container beatmapInfoContainer;
        private SpriteText roomNameText;
        private SpriteText beatmapNameText;
        private MenuButton startButton;
        private MenuButton readyButton;
        private MenuButton beatmapSelectButton;
        private Container beatmapSelectorOverlay;
        private FillFlowContainer<BeatmapOption> beatmapOptionsContainer;
        
        private bool isHost;
        private bool isReady;
        
        public LobbyScreen(Room room, MultiplayerService multiplayerService)
        {
            this.room = room;
            this.multiplayerService = multiplayerService;
        }
        
        [BackgroundDependencyLoader]
        private void load(AuthenticationService authService, TextureStore textures)
        {
            this.authService = authService;
            this.beatpackManager = new BeatpackManager();
            
            isHost = room.HostId == authService.GetUserId();
            
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
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 20),
                            Children = new Drawable[]
                            {
                                // Header
                                new Container
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 80,
                                    Children = new Drawable[]
                                    {
                                        new MenuButton("Leave", Color4.Gray, 20f, null, leaveLobby, new Vector2(120, 40))
                                        {
                                            Position = new Vector2(0, 20)
                                        },
                                        roomNameText = new SpriteText
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Text = room.RoomName,
                                            Font = new FontUsage("Kodchasan", size: 36, weight: "Bold"),
                                            Colour = Color4.White
                                        }
                                    }
                                },
                                // Main content
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Children = new Drawable[]
                                    {
                                        new GridContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ColumnDimensions = new[]
                                            {
                                                new Dimension(GridSizeMode.Relative, 0.6f),
                                                new Dimension(GridSizeMode.Relative, 0.4f)
                                            },
                                            RowDimensions = new[]
                                            {
                                                new Dimension(GridSizeMode.Relative, 1f)
                                            },
                                            Content = new[]
                                            {
                                                new Drawable[]
                                                {
                                                    // Left side - Beatmap info
                                                    new Container
                                                    {
                                                        RelativeSizeAxes = Axes.Both,
                                                        Padding = new MarginPadding { Right = 10 },
                                                        Children = new Drawable[]
                                                        {
                                                            beatmapInfoContainer = new Container
                                                            {
                                                                RelativeSizeAxes = Axes.Both,
                                                                Masking = true,
                                                                CornerRadius = 10,
                                                                Children = new Drawable[]
                                                                {
                                                                    new Box
                                                                    {
                                                                        RelativeSizeAxes = Axes.Both,
                                                                        Colour = new Color4(40, 40, 50, 255)
                                                                    },
                                                                    new FillFlowContainer
                                                                    {
                                                                        RelativeSizeAxes = Axes.Both,
                                                                        Direction = FillDirection.Vertical,
                                                                        Padding = new MarginPadding(20),
                                                                        Spacing = new Vector2(0, 15),
                                                                        Children = new Drawable[]
                                                                        {
                                                                            new SpriteText
                                                                            {
                                                                                Text = "Selected Beatpack",
                                                                                Font = new FontUsage("Kodchasan", size: 24, weight: "Bold"),
                                                                                Colour = Color4.White
                                                                            },
                                                                            beatmapNameText = new SpriteText
                                                                            {
                                                                                Text = room.SelectedBeatpack != null ? room.SelectedBeatpack.Title : "Loading...",
                                                                                Font = new FontUsage(size: 20, weight: "Bold"),
                                                                                Colour = Color4.White
                                                                            },
                                                                            new SpriteText
                                                                            {
                                                                                Text = room.SelectedBeatpack != null ? $"by {room.SelectedBeatpack.Artist}" : "",
                                                                                Font = new FontUsage(size: 16),
                                                                                Colour = new Color4(200, 200, 200, 255)
                                                                            },
                                                                            new SpriteText
                                                                            {
                                                                                Text = room.SelectedBeatpack != null && room.SelectedBeatpack.Creator != null ? $"Creator: {room.SelectedBeatpack.Creator.Username}" : "",
                                                                                Font = new FontUsage(size: 14),
                                                                                Colour = new Color4(150, 150, 150, 255)
                                                                            },
                                                                            beatmapSelectButton = new MenuButton(
                                                                                isHost ? "Select Beatmap" : "Download Required",
                                                                                new Color4(0, 150, 255, 255),
                                                                                18f,
                                                                                null,
                                                                                isHost ? showBeatmapSelector : downloadBeatmap,
                                                                                new Vector2(200, 40)
                                                                            )
                                                                            {
                                                                                Margin = new MarginPadding { Top = 20 },
                                                                                Alpha = isHost ? 1 : 0 // Hide for non-hosts initially
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    },
                                                    // Right side - Players list
                                                    new Container
                                                    {
                                                        RelativeSizeAxes = Axes.Both,
                                                        Padding = new MarginPadding { Left = 10 },
                                                        Children = new Drawable[]
                                                        {
                                                            new Container
                                                            {
                                                                RelativeSizeAxes = Axes.Both,
                                                                Masking = true,
                                                                CornerRadius = 10,
                                                                Children = new Drawable[]
                                                                {
                                                                    new Box
                                                                    {
                                                                        RelativeSizeAxes = Axes.Both,
                                                                        Colour = new Color4(40, 40, 50, 255)
                                                                    },
                                                                    new FillFlowContainer
                                                                    {
                                                                        RelativeSizeAxes = Axes.Both,
                                                                        Direction = FillDirection.Vertical,
                                                                        Padding = new MarginPadding(20),
                                                                        Spacing = new Vector2(0, 15),
                                                                        Children = new Drawable[]
                                                                        {
                                                                            new SpriteText
                                                                            {
                                                                                Text = "Players",
                                                                                Font = new FontUsage("Kodchasan", size: 24, weight: "Bold"),
                                                                                Colour = Color4.White
                                                                            },
                                                                            new BasicScrollContainer
                                                                            {
                                                                                RelativeSizeAxes = Axes.Both,
                                                                                ScrollbarVisible = false,
                                                                                Child = playersContainer = new FillFlowContainer
                                                                                {
                                                                                    RelativeSizeAxes = Axes.X,
                                                                                    AutoSizeAxes = Axes.Y,
                                                                                    Direction = FillDirection.Vertical,
                                                                                    Spacing = new Vector2(0, 10)
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        },
                                        // Bottom buttons
                                        new Container
                                        {
                                            Anchor = Anchor.BottomCentre,
                                            Origin = Anchor.BottomCentre,
                                            AutoSizeAxes = Axes.Both,
                                            Margin = new MarginPadding { Bottom = 20 },
                                            Children = new Drawable[]
                                            {
                                                readyButton = new MenuButton(
                                                    "Ready",
                                                    new Color4(255, 165, 0, 255),
                                                    20f,
                                                    null,
                                                    toggleReady,
                                                    new Vector2(150, 50)
                                                )
                                                {
                                                    Alpha = isHost ? 0 : 1
                                                },
                                                startButton = new MenuButton(
                                                    "Start Game",
                                                    new Color4(0, 255, 100, 255),
                                                    20f,
                                                    null,
                                                    startGame,
                                                    new Vector2(150, 50)
                                                )
                                                {
                                                    Alpha = isHost ? 1 : 0
                                                }
                                            }
                                        }
                                    }
                                },
                                // Beatmap selector overlay
                                beatmapSelectorOverlay = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Alpha = 0,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = new Color4(0, 0, 0, 200)
                                        },
                                        new Container
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Size = new Vector2(600, 500),
                                            Masking = true,
                                            CornerRadius = 10,
                                            Children = new Drawable[]
                                            {
                                                new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Colour = new Color4(40, 40, 50, 255)
                                                },
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Vertical,
                                                    Padding = new MarginPadding(20),
                                                    Spacing = new Vector2(0, 15),
                                                    Children = new Drawable[]
                                                    {
                                                        new SpriteText
                                                        {
                                                            Text = "Select Beatmap",
                                                            Font = new FontUsage("Kodchasan", size: 24, weight: "Bold"),
                                                            Colour = Color4.White
                                                        },
                                                        new BasicScrollContainer
                                                        {
                                                            RelativeSizeAxes = Axes.Both,
                                                            ScrollbarVisible = true,
                                                            Child = beatmapOptionsContainer = new FillFlowContainer<BeatmapOption>
                                                            {
                                                                RelativeSizeAxes = Axes.X,
                                                                AutoSizeAxes = Axes.Y,
                                                                Direction = FillDirection.Vertical,
                                                                Spacing = new Vector2(0, 5)
                                                            }
                                                        },
                                                        new MenuButton("Close", Color4.Gray, 16f, null, hideBeatmapSelector, new Vector2(120, 35))
                                                        {
                                                            Anchor = Anchor.BottomCentre,
                                                            Origin = Anchor.BottomCentre
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            
            // Add host indicator
            if (isHost)
            {
                addPlayer(authService.GetUsername(), true, false);
            }
            
            // TODO: Load actual participants from room data
        }
        
        private void toggleReady()
        {
            isReady = !isReady;
            readyButton.Text = isReady ? "Not Ready" : "Ready";
            // TODO: Update ready status on server
        }
        
        private async void startGame()
        {
            if (!isHost) return;
            
            // Update room status to "playing"
            await multiplayerService.UpdateRoomStatus(room.Id, "playing");
            
            // Navigate to multiplayer game screen
            // TODO: Create MultiplayerGameScreen
            Logger.Log("Starting multiplayer game...", LoggingTarget.Runtime, LogLevel.Important);
        }
        
        private void showBeatmapSelector()
        {
            if (!isHost) return;
            
            // Load beatpacks
            beatmapOptionsContainer.Clear();
            foreach (var beatpack in beatpackManager.GetAllBeatpacks())
            {
                var option = new BeatmapOption(beatpack, selectBeatmap);
                beatmapOptionsContainer.Add(option);
            }
            
            beatmapSelectorOverlay.FadeIn(200);
        }
        
        private void hideBeatmapSelector()
        {
            beatmapSelectorOverlay.FadeOut(200);
        }
        
        private async void selectBeatmap(Beatpack beatpack)
        {
            hideBeatmapSelector();
            
            // TODO: Get actual beatmap ID from server
            // For now, just update UI
            beatmapNameText.Text = $"{beatpack.Beatmap?.Title} by {beatpack.Beatmap?.Artist}";
            
            // Update on server
            // await multiplayerService.SelectBeatmap(room.Id, beatmapId);
        }
        
        private void downloadBeatmap()
        {
            // TODO: Implement beatmap download
            Logger.Log("Downloading beatmap...", LoggingTarget.Runtime, LogLevel.Important);
        }
        
        private async void leaveLobby()
        {
            // Check if current user is the host
            bool isHost = room.HostId == multiplayerService.GetUserId();
            
            if (isHost)
            {
                // Host is leaving - delete the entire room
                Logger.Log($"Host leaving - deleting room {room.Id}", LoggingTarget.Runtime, LogLevel.Important);
                await multiplayerService.DeleteRoom(room.Id);
            }
            else
            {
                // Regular player - just leave
                await multiplayerService.LeaveRoom(room.Id);
            }
            
            this.Exit();
        }
        
        private void addPlayer(string username, bool isHost, bool isReady)
        {
            var playerCard = new PlayerCard(username, isHost, isReady);
            playersContainer.Add(playerCard);
        }
        
        private class PlayerCard : Container
        {
            public PlayerCard(string username, bool isHost, bool isReady)
            {
                RelativeSizeAxes = Axes.X;
                Height = 60;
                Masking = true;
                CornerRadius = 5;
                
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(60, 60, 70, 255)
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(10),
                        Children = new Drawable[]
                        {
                            new CircularContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Size = new Vector2(40, 40),
                                Masking = true,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = new Color4(100, 100, 120, 255)
                                    },
                                    new SpriteText
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Text = username.Substring(0, 1).ToUpper(),
                                        Font = new FontUsage(size: 18, weight: "Bold"),
                                        Colour = Color4.White
                                    }
                                }
                            },
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(10, 0),
                                Margin = new MarginPadding { Left = 50 },
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = username,
                                        Font = new FontUsage(size: 16),
                                        Colour = Color4.White
                                    },
                                    new SpriteText
                                    {
                                        Text = isHost ? "[HOST]" : "",
                                        Font = new FontUsage(size: 14, weight: "Bold"),
                                        Colour = new Color4(255, 215, 0, 255)
                                    }
                                }
                            },
                            new SpriteText
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Text = isReady ? "READY" : "NOT READY",
                                Font = new FontUsage(size: 14, weight: "Bold"),
                                Colour = isReady ? new Color4(0, 255, 100, 255) : new Color4(255, 100, 100, 255)
                            }
                        }
                    }
                };
            }
        }
        
        private class BeatmapOption : Container
        {
            private readonly Beatpack beatpack;
            private readonly Action<Beatpack> onSelect;
            private Box hoverBox;
            
            public BeatmapOption(Beatpack beatpack, Action<Beatpack> onSelect)
            {
                this.beatpack = beatpack;
                this.onSelect = onSelect;
                
                RelativeSizeAxes = Axes.X;
                Height = 60;
                Masking = true;
                CornerRadius = 5;
                
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(60, 60, 70, 255)
                    },
                    hoverBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(80, 80, 90, 0),
                        Alpha = 0
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(10),
                        Child = new SpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = $"{beatpack.Beatmap?.Title} - {beatpack.Beatmap?.Artist}",
                            Font = new FontUsage(size: 16),
                            Colour = Color4.White
                        }
                    }
                };
            }
            
            protected override bool OnClick(ClickEvent e)
            {
                onSelect?.Invoke(beatpack);
                return true;
            }
            
            protected override bool OnHover(HoverEvent e)
            {
                hoverBox.FadeTo(0.3f, 200);
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
