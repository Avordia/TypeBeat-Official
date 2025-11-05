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
using osu.Framework.Logging;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Online;
using TypeBeat.Game.Gameplay.Scoring;

namespace TypeBeat.Game
{
    public partial class MultiplayerGameScreen : GameScreen
    {
        private readonly Room room;
        private readonly MultiplayerService multiplayerService;
        private readonly string currentUserId;
        
        // Multiplayer UI elements
        private Container multiplayerOverlay;
        private FillFlowContainer<PlayerScoreCard> topPlayersContainer;
        private Container playerArrowIndicator;
        private SpriteText playerRankText;
        private Sprite arrowSprite;
        
        // Score tracking
        private List<PlayerScore> playerScores = new List<PlayerScore>();
        private int currentRank = 0;
        private bool scoreUpdateInProgress = false;
        
        public MultiplayerGameScreen(Beatpack beatpack, Beatmap beatmap, Room room, MultiplayerService multiplayerService, string userId) 
            : base(beatpack, beatmap)
        {
            this.room = room;
            this.multiplayerService = multiplayerService;
            this.currentUserId = userId;
            
            // Subscribe to multiplayer events
            multiplayerService.OnScoresUpdated += onScoresUpdated;
        }
        
        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            // Add multiplayer overlay after base game screen loads
            AddInternal(multiplayerOverlay = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Depth = -100, // Ensure it's on top
                Children = new Drawable[]
                {
                    // Top 3 players display at bottom
                    new Container
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Size = new Vector2(800, 120),
                        Y = -50,
                        Children = new Drawable[]
                        {
                            topPlayersContainer = new FillFlowContainer<PlayerScoreCard>
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(20, 0)
                            }
                        }
                    },
                    // Player's own rank indicator
                    playerArrowIndicator = new Container
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.TopCentre,
                        Size = new Vector2(50, 50),
                        Y = -180,
                        Alpha = 0, // Hidden initially
                        Children = new Drawable[]
                        {
                            arrowSprite = new Sprite
                            {
                                Texture = textures.Get("images/ArrowHead.png"),
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Scale = new Vector2(0.5f),
                                Colour = Color4.Red,
                                Rotation = 180 // Point downward
                            },
                            playerRankText = new SpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Y = -30,
                                Font = new FontUsage("Kodchasan", size: 18, weight: "Bold"),
                                Colour = Color4.White,
                                Text = "#4"
                            }
                        }
                    }
                }
            });
            
            // Initialize with dummy players for testing
            initializePlayers();
            
            // Start periodic score updates
            Task.Run(async () =>
            {
                while (!IsDisposed)
                {
                    await updateScores();
                    await Task.Delay(2000); // Update every 2 seconds
                }
            });
        }
        
        private void initializePlayers()
        {
            // Add sample players for now
            // In production, this would come from the room participants
            playerScores = new List<PlayerScore>
            {
                new PlayerScore { UserId = currentUserId, Username = "You", Score = 0, IsCurrentPlayer = true },
                new PlayerScore { UserId = "player2", Username = "Player 2", Score = 0 },
                new PlayerScore { UserId = "player3", Username = "Player 3", Score = 0 },
                new PlayerScore { UserId = "player4", Username = "Player 4", Score = 0 }
            };
            
            updatePlayerDisplay();
        }
        
        private async Task updateScores()
        {
            if (scoreUpdateInProgress) return;
            scoreUpdateInProgress = true;
            
            try
            {
                // Get current player's score from base game
                var currentScore = getCurrentScore();
                var accuracy = getCurrentAccuracy();
                var maxCombo = getCurrentCombo();
                
                // Submit score to server
                await multiplayerService.SubmitScore(room.Id, 0, currentScore, accuracy, maxCombo);
                
                // Get all scores from server
                var matchScores = await multiplayerService.GetMatchScores(room.Id);
                
                Schedule(() =>
                {
                    // Update local player scores
                    foreach (var score in matchScores)
                    {
                        var playerScore = playerScores.FirstOrDefault(p => p.UserId == score.UserId);
                        if (playerScore != null)
                        {
                            playerScore.Score = score.Score;
                        }
                    }
                    
                    updatePlayerDisplay();
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"Error updating multiplayer scores: {ex.Message}", LoggingTarget.Network, LogLevel.Error);
            }
            finally
            {
                scoreUpdateInProgress = false;
            }
        }
        
        private void updatePlayerDisplay()
        {
            // Sort players by score
            var sortedPlayers = playerScores.OrderByDescending(p => p.Score).ToList();
            
            // Update top 3 display
            topPlayersContainer.Clear();
            for (int i = 0; i < Math.Min(3, sortedPlayers.Count); i++)
            {
                var card = new PlayerScoreCard(sortedPlayers[i], i + 1);
                topPlayersContainer.Add(card);
                
                // If current player is in top 3, show arrow above their card
                if (sortedPlayers[i].IsCurrentPlayer)
                {
                    playerArrowIndicator.Alpha = 1;
                    playerArrowIndicator.X = (i - 1) * 260; // Adjust position based on rank
                    currentRank = i + 1;
                }
            }
            
            // If player is not in top 3, update rank display
            var currentPlayer = sortedPlayers.FirstOrDefault(p => p.IsCurrentPlayer);
            if (currentPlayer != null)
            {
                var rank = sortedPlayers.IndexOf(currentPlayer) + 1;
                currentRank = rank;
                
                if (rank > 3)
                {
                    playerArrowIndicator.Alpha = 1;
                    playerArrowIndicator.X = 0; // Center position
                    playerRankText.Text = $"#{rank}";
                }
            }
        }
        
        private void onScoresUpdated(List<MatchScore> scores)
        {
            Schedule(() =>
            {
                foreach (var score in scores)
                {
                    var playerScore = playerScores.FirstOrDefault(p => p.UserId == score.UserId);
                    if (playerScore != null)
                    {
                        playerScore.Score = score.Score;
                    }
                    else
                    {
                        // Add new player if not in list
                        playerScores.Add(new PlayerScore
                        {
                            UserId = score.UserId,
                            Username = score.User?.Username ?? "Unknown",
                            Score = score.Score,
                            IsCurrentPlayer = score.UserId == currentUserId
                        });
                    }
                }
                
                updatePlayerDisplay();
            });
        }
        
        private long getCurrentScore()
        {
            // Get score from base game's score processor
            // This would need to be exposed from the base GameScreen
            return 0; // Placeholder
        }
        
        private float getCurrentAccuracy()
        {
            // Get accuracy from base game
            return 100f; // Placeholder
        }
        
        private int getCurrentCombo()
        {
            // Get combo from base game
            return 0; // Placeholder
        }
        
        protected override void Dispose(bool isDisposing)
        {
            multiplayerService.OnScoresUpdated -= onScoresUpdated;
            base.Dispose(isDisposing);
        }
        
        private class PlayerScore
        {
            public string UserId { get; set; }
            public string Username { get; set; }
            public long Score { get; set; }
            public bool IsCurrentPlayer { get; set; }
        }
        
        private class PlayerScoreCard : Container
        {
            public PlayerScoreCard(PlayerScore player, int rank)
            {
                Size = new Vector2(240, 80);
                Masking = true;
                CornerRadius = 40;
                
                // Different colors for ranks
                Color4 bgColor = rank switch
                {
                    1 => new Color4(255, 215, 0, 255), // Gold
                    2 => new Color4(192, 192, 192, 255), // Silver
                    3 => new Color4(205, 127, 50, 255), // Bronze
                    _ => new Color4(100, 100, 100, 255) // Default
                };
                
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(30, 30, 30, 255)
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(10),
                        Children = new Drawable[]
                        {
                            // Avatar placeholder
                            new CircularContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Size = new Vector2(60, 60),
                                Masking = true,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = bgColor
                                    },
                                    new SpriteText
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Text = player.Username.Substring(0, Math.Min(2, player.Username.Length)).ToUpper(),
                                        Font = new FontUsage(size: 20, weight: "Bold"),
                                        Colour = Color4.Black
                                    }
                                }
                            },
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Margin = new MarginPadding { Left = 70 },
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = player.Username.ToUpper(),
                                        Font = new FontUsage(size: 14, weight: "Bold"),
                                        Colour = Color4.White
                                    },
                                    new SpriteText
                                    {
                                        Text = player.Score.ToString("N0"),
                                        Font = new FontUsage(size: 18),
                                        Colour = Color4.White
                                    }
                                }
                            }
                        }
                    },
                    // Add arrow indicator if this is the current player
                    player.IsCurrentPlayer ? new Sprite
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.BottomCentre,
                        Y = -10,
                        Scale = new Vector2(0.3f),
                        Colour = Color4.Red,
                        Rotation = 180
                    } : Empty()
                };
            }
        }
    }
}
