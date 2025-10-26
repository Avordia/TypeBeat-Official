// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace TypeBeat.Game.Editor.UI
{
    /// <summary>
    /// Left sidebar container showing list of beatpacks.
    /// </summary>
    public partial class EditBeatpackListContainer : Container
    {
        [Resolved]
        private LocalBeatpackManager beatpackManager { get; set; }

        public Action OnAddBeatpackClicked { get; set; }

        private FillFlowContainer<BeatpackListItem> beatpackFlow;
        private Container emptyStateContainer;
        private BasicScrollContainer scrollContainer;

        [BackgroundDependencyLoader]
        private void load()
        {
            Masking = true;
            CornerRadius = 0;

            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(28, 28, 28, 255)
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Bottom = 80 },
                    Children = new Drawable[]
                    {
                        scrollContainer = new BasicScrollContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            ClampExtension = 30,
                            ScrollbarVisible = true,
                            Child = beatpackFlow = new FillFlowContainer<BeatpackListItem>
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 10),
                                Padding = new MarginPadding(15)
                            }
                        },
                        emptyStateContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0,
                            Child = new SpriteText
                            {
                                Text = "No beatpacks yet.\nClick + to create one!",
                                Font = FontUsage.Default.With(family: "Inter", size: 16),
                                Colour = Color4.Gray,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                AllowMultiline = true
                            }
                        }
                    }
                },
                // Add button at bottom
                new CircularContainer
                {
                    Size = new Vector2(60),
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Margin = new MarginPadding { Bottom = 15 },
                    Masking = true,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(76, 175, 80, 255) // #4CAF50
                        },
                        new SpriteText
                        {
                            Text = "+",
                            Font = FontUsage.Default.With(size: 36, weight: "Bold"),
                            Colour = Color4.White,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre
                        },
                        new ClickableContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Action = () => OnAddBeatpackClicked?.Invoke()
                        }
                    }
                }
            };

            // Subscribe to beatpack list changes
            beatpackManager.Projects.BindCollectionChanged((_, __) => updateBeatpackList(), true);
        }

        private void updateBeatpackList()
        {
            beatpackFlow.Clear();

            if (beatpackManager.Projects.Count == 0)
            {
                emptyStateContainer.FadeIn(200);
                scrollContainer.FadeOut(200);
                return;
            }

            emptyStateContainer.FadeOut(200);
            scrollContainer.FadeIn(200);

            foreach (var beatpack in beatpackManager.Projects)
            {
                var item = new BeatpackListItem(beatpack);
                item.OnSelected = selectedBeatpack =>
                {
                    // Deselect all other items
                    foreach (var otherItem in beatpackFlow.Children)
                    {
                        otherItem.IsSelected = false;
                    }

                    // Select this item
                    item.IsSelected = true;

                    // Update current beatpack
                    beatpackManager.CurrentBeatpack.Value = selectedBeatpack;
                };

                beatpackFlow.Add(item);
            }
        }
    }
}

