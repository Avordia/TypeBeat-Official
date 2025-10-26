// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Beatmaps;

namespace TypeBeat.Game.Editor
{
    /// <summary>
    /// Preview area showing how the beatmap will look during gameplay.
    /// </summary>
    public partial class TypeBeatPreviewArea : Container
    {
        private readonly LocalBeatmap beatmap;
        private Container previewContainer;
        private Container wordBubble;
        
        public TypeBeatPreviewArea(LocalBeatmap beatmap)
        {
            this.beatmap = beatmap;
        }
        
        protected override void LoadComplete()
        {
            base.LoadComplete();
            
            Children = new Drawable[]
            {
                // Background
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                    Alpha = 0.5f
                },
                // Preview content
                previewContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.X,
                        Height = 100,
                        Child = createPreviewVisualization()
                    }
                }
            };
        }
        
        private Drawable createPreviewVisualization()
        {
            // Create the curved path visualization similar to the mockup
            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    // Left curve
                    new Container
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreRight,
                        RelativeSizeAxes = Axes.Y,
                        Width = 200,
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Text = "◠",
                                Font = FontUsage.Default.With(size: 100),
                                Colour = new Color4(100, 100, 100, 255),
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Rotation = 90
                            }
                        }
                    },
                    // Center word bubble
                    wordBubble = new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 25,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Red,
                                Alpha = 0.9f
                            },
                            new SpriteText
                            {
                                Text = getPreviewWord(),
                                Font = FontUsage.Default.With(family: "Inter-Bold", size: 32),
                                Colour = Color4.White,
                                Padding = new MarginPadding { Horizontal = 30, Vertical = 10 }
                            }
                        }
                    },
                    // Right curve
                    new Container
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.Y,
                        Width = 200,
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Text = "◠",
                                Font = FontUsage.Default.With(size: 100),
                                Colour = new Color4(100, 100, 100, 255),
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Rotation = -90
                            }
                        }
                    },
                    // Trail visualization (curved lines on sides)
                    createTrailVisualization()
                }
            };
        }
        
        private Container createTrailVisualization()
        {
            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    // Left side trails
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreRight,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(20, 0),
                        X = -250,
                        Children = new Drawable[]
                        {
                            createCurvedTrail(),
                            createCurvedTrail(),
                            createCurvedTrail()
                        }
                    },
                    // Right side trails
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreLeft,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(20, 0),
                        X = 250,
                        Children = new Drawable[]
                        {
                            createCurvedTrail(),
                            createCurvedTrail(),
                            createCurvedTrail()
                        }
                    }
                }
            };
        }
        
        private Drawable createCurvedTrail()
        {
            return new Container
            {
                Size = new Vector2(60, 80),
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(80, 80, 80, 255),
                    Alpha = 0.3f
                }
            };
        }
        
        private string getPreviewWord()
        {
            if (beatmap.MapData == null || !beatmap.MapData.Any())
                return "TYPE";
            
            // Get the first complete word from map data
            var firstSegment = beatmap.MapData.FirstOrDefault();
            if (firstSegment?.Notes == null || !firstSegment.Notes.Any())
                return "TYPE";
            
            string word = "";
            foreach (var note in firstSegment.Notes)
            {
                if (note.Character != "/")
                    word += note.Character;
            }
            
            return string.IsNullOrEmpty(word) ? "TYPE" : word.ToUpperInvariant();
        }
        
        public void UpdatePreview(double currentTime)
        {
            // Update preview based on current playback time
            // This would show which word should be displayed at the current time
            Schedule(() =>
            {
                // Find the active word at current time
                string activeWord = findWordAtTime(currentTime);
                if (!string.IsNullOrEmpty(activeWord))
                {
                    updateWordBubble(activeWord);
                }
            });
        }
        
        private string findWordAtTime(double time)
        {
            if (beatmap.MapData == null) return "";
            
            foreach (var segment in beatmap.MapData)
            {
                if (segment.Notes == null || !segment.Notes.Any()) continue;
                
                // Check if any note in this segment is active at current time
                bool isActive = segment.Notes.Any(n => time >= n.StartTime && time <= n.EndTime);
                
                if (isActive)
                {
                    // Build word from segment
                    string word = "";
                    foreach (var note in segment.Notes)
                    {
                        if (note.Character != "/")
                            word += note.Character;
                    }
                    return word;
                }
            }
            
            return "";
        }
        
        private void updateWordBubble(string word)
        {
            // Update the word bubble text
            var textSprite = wordBubble.Children.OfType<SpriteText>().FirstOrDefault();
            if (textSprite != null)
            {
                textSprite.Text = word.ToUpperInvariant();
            }
        }
    }
}
