using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
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
    public partial class MultiplayerScreen : Screen
    {
        private AuthenticationService authService;
        private MultiplayerService multiplayerService;
        private BeatpackManager beatpackManager;
        private GameHost host;
        private FillFlowContainer<LobbyCard> lobbiesContainer;
        private Container playerInfoContainer;
        private Container createLobbyDialog;
        private Container beatpackSelectorDialog;
        private TextBox roomNameTextBox;
        private SpriteText playerNameText;
        private SpriteText statusText;
        private BasicScrollContainer scrollContainer;
        private FillFlowContainer beatpackSelectContainer;
        private long? selectedBeatpackId;
        
        [BackgroundDependencyLoader]
        private void load(AuthenticationService authService, TextureStore textures, BeatpackManager beatpacks, GameHost gameHost)
        {
            this.authService = authService;
            this.multiplayerService = new MultiplayerService(authService);
            this.beatpackManager = beatpacks;
            this.host = gameHost;
            
            // Subscribe to events
            multiplayerService.OnRoomsUpdated += onRoomsUpdated;
            multiplayerService.OnRoomJoined += onRoomJoined;
            multiplayerService.OnError += onError;
            
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
                                        new MenuButton("Back", Color4.Gray, 20f, null, () => this.Exit(), new Vector2(120, 40))
                                        {
                                            Position = new Vector2(0, 20)
                                        },
                                        new SpriteText
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Text = "MULTIPLAYER",
                                            Font = new FontUsage("Kodchasan", size: 36, weight: "Bold"),
                                            Colour = Color4.White
                                        }
                                    }
                                },
                                // Player info and create lobby button
                                playerInfoContainer = new Container
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 60,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = new Color4(40, 40, 50, 255),
                                            Alpha = 0.5f
                                        },
                                        new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Padding = new MarginPadding(10),
                                            Children = new Drawable[]
                                            {
                                                playerNameText = new SpriteText
                                                {
                                                    Anchor = Anchor.CentreLeft,
                                                    Origin = Anchor.CentreLeft,
                                                    Text = authService.IsLoggedIn ? $"Logged in as: {authService.GetUsername()}" : "Not logged in",
                                                    Font = new FontUsage(size: 18),
                                                    Colour = Color4.White
                                                },
                                                new MenuButton("Create Lobby", new Color4(0, 150, 255, 255), 18f, null, showCreateLobbyDialog, new Vector2(150, 40))
                                                {
                                                    Anchor = Anchor.CentreRight,
                                                    Origin = Anchor.CentreRight
                                                }
                                            }
                                        }
                                    }
                                },
                                // Lobbies container
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Children = new Drawable[]
                                    {
                                        scrollContainer = new BasicScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = true,
                                            Child = lobbiesContainer = new FillFlowContainer<LobbyCard>
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Direction = FillDirection.Vertical,
                                                Spacing = new Vector2(0, 15),
                                                Padding = new MarginPadding(10)
                                            }
                                        },
                                        statusText = new SpriteText
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Text = "Loading lobbies...",
                                            Font = new FontUsage(size: 24),
                                            Colour = new Color4(150, 150, 150, 255)
                                        }
                                    }
                                }
                            }
                        },
                        // Create lobby dialog (hidden by default)
                        createLobbyDialog = new Container
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
                                    Size = new Vector2(400, 300),
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
                                            Spacing = new Vector2(0, 20),
                                            Children = new Drawable[]
                                            {
                                                new SpriteText
                                                {
                                                    Text = "Create New Lobby",
                                                    Font = new FontUsage("Kodchasan", size: 24, weight: "Bold"),
                                                    Colour = Color4.White
                                                },
                                                new SpriteText
                                                {
                                                    Text = "Room Name:",
                                                    Font = new FontUsage(size: 16),
                                                    Colour = Color4.White
                                                },
                                                roomNameTextBox = new BasicTextBox
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Height = 40,
                                                    PlaceholderText = "Enter room name...",
                                                    Text = $"{authService.GetUsername()}'s Room"
                                                },
                                                new Container
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Height = 40,
                                                    Margin = new MarginPadding { Top = 40 },
                                                    Children = new Drawable[]
                                                    {
                                                        new MenuButton("Cancel", Color4.Gray, 16f, null, hideCreateLobbyDialog, new Vector2(120, 35))
                                                        {
                                                            Anchor = Anchor.CentreLeft,
                                                            Origin = Anchor.CentreLeft
                                                        },
                                                        new MenuButton("Create", new Color4(0, 200, 100, 255), 16f, null, createLobby, new Vector2(120, 35))
                                                        {
                                                            Anchor = Anchor.CentreRight,
                                                            Origin = Anchor.CentreRight
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        // Beatpack selector dialog (hidden by default)
                        beatpackSelectorDialog = new Container
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
                                        new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Padding = new MarginPadding(20),
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(0, 15),
                                                    Children = new Drawable[]
                                                    {
                                                        new SpriteText
                                                        {
                                                            Text = "Select a Beatpack",
                                                            Font = new FontUsage("Kodchasan", size: 24, weight: "Bold"),
                                                            Colour = Color4.White
                                                        },
                                                        new SpriteText
                                                        {
                                                            Text = "Choose a beatpack for your lobby",
                                                            Font = new FontUsage(size: 14),
                                                            Colour = new Color4(180, 180, 180, 255)
                                                        }
                                                    }
                                                },
                                                new BasicScrollContainer
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Padding = new MarginPadding { Top = 90, Bottom = 60 },
                                                    ScrollbarVisible = true,
                                                    Child = beatpackSelectContainer = new FillFlowContainer
                                                    {
                                                        RelativeSizeAxes = Axes.X,
                                                        AutoSizeAxes = Axes.Y,
                                                        Direction = FillDirection.Vertical,
                                                        Spacing = new Vector2(0, 10)
                                                    }
                                                },
                                                new Container
                                                {
                                                    Anchor = Anchor.BottomLeft,
                                                    Origin = Anchor.BottomLeft,
                                                    RelativeSizeAxes = Axes.X,
                                                    Height = 40,
                                                    Children = new Drawable[]
                                                    {
                                                        new MenuButton("Cancel", Color4.Gray, 16f, null, hideBeatpackSelector, new Vector2(120, 35))
                                                        {
                                                            Anchor = Anchor.CentreLeft,
                                                            Origin = Anchor.CentreLeft
                                                        },
                                                        new MenuButton("Next", new Color4(0, 200, 100, 255), 16f, null, showCreateLobbyDialogWithBeatpack, new Vector2(120, 35))
                                                        {
                                                            Anchor = Anchor.CentreRight,
                                                            Origin = Anchor.CentreRight
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
        }
        
        protected override void LoadComplete()
        {
            base.LoadComplete();
            Task.Run(async () => await refreshLobbies());
        }
        
        private async Task refreshLobbies()
        {
            var rooms = await multiplayerService.GetActiveRooms();
            Schedule(() =>
            {
                if (rooms.Count == 0)
                {
                    statusText.Text = "No active lobbies";
                    statusText.Show();
                }
                else
                {
                    statusText.Hide();
                }
            });
        }
        
        private void onRoomsUpdated(List<Room> rooms)
        {
            Schedule(() =>
            {
                lobbiesContainer.Clear();
                
                foreach (var room in rooms)
                {
                    var card = new LobbyCard(room, joinRoom);
                    lobbiesContainer.Add(card);
                }
                
                if (rooms.Count == 0)
                {
                    statusText.Text = "No active lobbies";
                    statusText.Show();
                }
                else
                {
                    statusText.Hide();
                }
            });
        }
        
        private void onRoomJoined(Room room)
        {
            Schedule(() =>
            {
                // Navigate to lobby screen
                this.Push(new LobbyScreen(room, multiplayerService));
            });
        }
        
        private void onError(string error)
        {
            Schedule(() =>
            {
                Logger.Log($"Multiplayer error: {error}", LoggingTarget.Network, LogLevel.Error);
                // TODO: Show error notification
            });
        }
        
        private void showCreateLobbyDialog()
        {
            if (!authService.IsLoggedIn)
            {
                // TODO: Show login prompt
                return;
            }
            
            // Refresh beatpack list to include newly downloaded beatpacks
            beatpackManager?.RefreshBeatpacks(host);
            
            // Load beatpacks and show selector - ONLY show beatpacks with OnlineBeatpackID
            beatpackSelectContainer.Clear();
            selectedBeatpackId = null;
            
            var onlineBeatpacks = beatpackManager.GetAllBeatpacks()
                .Where(bp => !string.IsNullOrEmpty(bp.OnlineBeatpackID))
                .ToList();
            
            if (onlineBeatpacks.Count == 0)
            {
                // Show message that no online beatpacks are available
                beatpackSelectContainer.Add(new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding(20),
                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 10),
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Text = "No Online Beatpacks Found",
                                Font = new FontUsage("Kodchasan", size: 20, weight: "Bold"),
                                Colour = new Color4(255, 180, 0, 255),
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre
                            },
                            new SpriteText
                            {
                                Text = "To create a multiplayer lobby, you need to download beatpacks from the Exploration screen first.",
                                Font = new FontUsage(size: 14),
                                Colour = new Color4(200, 200, 200, 255),
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre
                            },
                            new SpriteText
                            {
                                Text = "Local beatpacks cannot be used for online play.",
                                Font = new FontUsage(size: 12),
                                Colour = new Color4(150, 150, 150, 255),
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre
                            }
                        }
                    }
                });
            }
            else
            {
                foreach (var beatpack in onlineBeatpacks)
                {
                    var card = new BeatpackSelectorCard(beatpack, selectBeatpack, deselectAllBeatpacks);
                    beatpackSelectContainer.Add(card);
                }
            }
            
            beatpackSelectorDialog.FadeIn(200);
        }
        
        private void deselectAllBeatpacks()
        {
            foreach (var drawable in beatpackSelectContainer)
            {
                if (drawable is BeatpackSelectorCard card)
                {
                    card.Deselect();
                }
            }
        }
        
        private void hideBeatpackSelector()
        {
            beatpackSelectorDialog.FadeOut(200);
        }
        
        private void selectBeatpack(long beatpackId)
        {
            selectedBeatpackId = beatpackId;
            Logger.Log($"Selected beatpack ID: {beatpackId}", LoggingTarget.Runtime, LogLevel.Important);
        }
        
        private void showCreateLobbyDialogWithBeatpack()
        {
            if (!selectedBeatpackId.HasValue)
            {
                Logger.Log("Please select a beatpack first!", LoggingTarget.Runtime, LogLevel.Error);
                return;
            }
            
            hideBeatpackSelector();
            createLobbyDialog.FadeIn(200);
        }
        
        private void hideCreateLobbyDialog()
        {
            createLobbyDialog.FadeOut(200);
        }
        
        private async void createLobby()
        {
            if (!selectedBeatpackId.HasValue)
            {
                Logger.Log("No beatpack selected!", LoggingTarget.Runtime, LogLevel.Error);
                return;
            }
            
            var roomName = roomNameTextBox.Text;
            if (string.IsNullOrWhiteSpace(roomName))
            {
                roomName = $"{authService.GetUsername()}'s Room";
            }
            
            hideCreateLobbyDialog();
            
            var room = await multiplayerService.CreateRoom(roomName, selectedBeatpackId.Value);
            if (room != null)
            {
                // Room creation successful, navigation will be handled by OnRoomJoined event
            }
        }
        
        private async void joinRoom(Room room)
        {
            await multiplayerService.JoinRoom(room.Id);
        }
        
        protected override void Dispose(bool isDisposing)
        {
            multiplayerService?.Dispose();
            base.Dispose(isDisposing);
        }
        
        private class LobbyCard : Container
        {
            private readonly Room room;
            private readonly Action<Room> onJoin;
            private Box hoverBox;
            
            public LobbyCard(Room room, Action<Room> onJoin)
            {
                this.room = room;
                this.onJoin = onJoin;
                
                RelativeSizeAxes = Axes.X;
                Height = 100;
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
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(15),
                        Children = new Drawable[]
                        {
                            // Left side - Beatpack info
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 5),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = room.RoomName,
                                        Font = new FontUsage("Kodchasan", size: 20, weight: "Bold"),
                                        Colour = Color4.White
                                    },
                                    new SpriteText
                                    {
                                        Text = $"Host: {room.Host?.Username ?? "Unknown"}",
                                        Font = new FontUsage(size: 14),
                                        Colour = new Color4(200, 200, 200, 255)
                                    },
                                    new SpriteText
                                    {
                                        Text = room.SelectedBeatmap != null 
                                            ? $"{room.SelectedBeatmap.Beatpack?.Title} - {room.SelectedBeatmap.DifficultyName}"
                                            : "No beatmap selected",
                                        Font = new FontUsage(size: 12),
                                        Colour = new Color4(150, 150, 150, 255)
                                    }
                                }
                            },
                            // Right side - Players
                            new Container
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                AutoSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(10, 0),
                                        Children = new Drawable[]
                                        {
                                            // Player avatars placeholders
                                            new CircularContainer
                                            {
                                                Size = new Vector2(50, 50),
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
                                                        Text = $"{room.Participants?.Count ?? 0}/{room.MaxPlayers}",
                                                        Font = new FontUsage(size: 14),
                                                        Colour = Color4.White
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
            }
            
            protected override bool OnClick(ClickEvent e)
            {
                onJoin?.Invoke(room);
                return true;
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
        
        private class BeatpackSelectorCard : Container
        {
            private readonly Beatpack beatpack;
            private readonly Action<long> onSelect;
            private readonly Action deselectAll;
            private readonly Beatmap firstBeatmap;
            private Box selectionBox;
            private bool isSelected;
            
            public BeatpackSelectorCard(Beatpack beatpack, Action<long> onSelect, Action deselectAll)
            {
                this.beatpack = beatpack;
                this.onSelect = onSelect;
                this.deselectAll = deselectAll;
                
                RelativeSizeAxes = Axes.X;
                Height = 80;
                Masking = true;
                CornerRadius = 8;
                
                // Get first beatmap for metadata
                firstBeatmap = beatpack.Beatmaps?.FirstOrDefault() ?? beatpack.Beatmap;
                var title = firstBeatmap?.Title ?? Path.GetFileNameWithoutExtension(beatpack.FilePath);
                var artist = firstBeatmap?.Artist ?? "Unknown Artist";
                var beatmapCount = beatpack.Beatmaps?.Count ?? (beatpack.Beatmap != null ? 1 : 0);
                
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(50, 50, 60, 255)
                    },
                    selectionBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(0, 200, 100, 100),
                        Alpha = 0
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(12),
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 3),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = title,
                                        Font = new FontUsage("Kodchasan", size: 18, weight: "Bold"),
                                        Colour = Color4.White
                                    },
                                    new SpriteText
                                    {
                                        Text = $"by {artist}",
                                        Font = new FontUsage(size: 14),
                                        Colour = new Color4(180, 180, 180, 255)
                                    },
                                    new SpriteText
                                    {
                                        Text = $"{beatmapCount} beatmap(s)",
                                        Font = new FontUsage(size: 12),
                                        Colour = new Color4(150, 150, 150, 255)
                                    }
                                }
                            }
                        }
                    }
                };
            }
            
            public void Deselect()
            {
                if (!isSelected) return;
                
                isSelected = false;
                selectionBox.FadeOut(200);
            }
            
            protected override bool OnClick(ClickEvent e)
            {
                // Deselect all other cards first (radio button behavior)
                deselectAll?.Invoke();
                
                // Select this card
                isSelected = true;
                selectionBox.FadeTo(1f, 200);
                
                // Parse and send beatpack ID (already filtered to only show valid online beatpacks)
                if (long.TryParse(beatpack.OnlineBeatpackID, out long beatpackId))
                {
                    onSelect(beatpackId);
                }
                
                return true;
            }
            
            protected override bool OnHover(HoverEvent e)
            {
                if (!isSelected)
                    selectionBox.FadeTo(0.3f, 200);
                return base.OnHover(e);
            }
            
            protected override void OnHoverLost(HoverLostEvent e)
            {
                if (!isSelected)
                    selectionBox.FadeOut(200);
                base.OnHoverLost(e);
            }
        }
    }
}
