// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Beatmaps;

namespace TypeBeat.Game.Editor
{
    /// <summary>
    /// Timeline component for the TypeBeat editor with grid-based note placement.
    /// </summary>
    public partial class TypeBeatTimeline : Container
    {
        private readonly LocalBeatmap beatmap;
        public readonly double tempo;
        public readonly int step;
        public readonly bool magnetEnabled;
        private readonly double defaultTailLength;
        public readonly bool showTail;
        
        private const int ROW_COUNT = 8;
        public const float FIXED_ROW_HEIGHT = 60f; // Fixed height per row in pixels
        public const int TOTAL_MEASURES = 200; // Large number for near-infinite scrolling
        
        public event Action OnContentChanged;
        
        // Zoom state
        private float zoomLevel = 1.0f;
        
        private Container gridContainer;
        private Container notesContainer;
        private Container wordDisplayContainer;
        
        private List<EditorNote> editorNotes = new List<EditorNote>();
        private EditorNote selectedNote;
        private EditorNote draggingNote;
        
        public TypeBeatTimeline(LocalBeatmap beatmap, double tempo, int step, bool magnetEnabled, double defaultTailLength, bool showTail)
        {
            this.beatmap = beatmap;
            this.tempo = tempo;
            this.step = step;
            this.magnetEnabled = magnetEnabled;
            this.defaultTailLength = defaultTailLength;
            this.showTail = showTail;
        }
        
        protected override void LoadComplete()
        {
            base.LoadComplete();
            
            Children = new Drawable[]
            {
                // Word display area (top)
                wordDisplayContainer = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 60,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(40, 30, 20, 255)
                        }
                    }
                },
                // Grid area (main timeline)
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = 60 },
                    Children = new Drawable[]
                    {
                        // Background
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(30, 40, 45, 255)
                        },
                        // Grid lines
                        gridContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both
                        },
                        // Notes layer
                        notesContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both
                        }
                    }
                }
            };
            
            buildGrid();
            loadNotes();
            updateWordDisplay();
        }
        
        private void buildGrid()
        {
            gridContainer.Clear();
            
            // Use fixed row height
            float rowHeight = FIXED_ROW_HEIGHT;
            
            // Draw horizontal lines for rows
            for (int i = 0; i <= ROW_COUNT; i++)
            {
                gridContainer.Add(new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 1,
                    Y = i * rowHeight,
                    Colour = new Color4(60, 70, 75, 255)
                });
            }
            
            // Draw vertical lines for measures and subdivisions
            float measureWidth = DrawWidth / TOTAL_MEASURES;
            // Music notation: 1/1 = 4 beats, 1/2 = 8 subdivisions, 1/4 = 16, etc.
            int subdivisions = 4 * step;
            
            for (int measure = 0; measure <= TOTAL_MEASURES; measure++)
            {
                float x = measure * measureWidth;
                
                // Measure line (thicker)
                gridContainer.Add(new Box
                {
                    Width = 2,
                    RelativeSizeAxes = Axes.Y,
                    X = x,
                    Colour = new Color4(80, 90, 95, 255)
                });
                
                // Measure number
                if (measure < TOTAL_MEASURES)
                {
                    gridContainer.Add(new SpriteText
                    {
                        Text = (measure + 1).ToString(),
                        Font = FontUsage.Default.With(family: "Inter-Bold", size: 20),
                        Colour = Color4.White,
                        Position = new Vector2(x + 10, 5)
                    });
                }
                
                // Subdivision lines
                if (measure < TOTAL_MEASURES)
                {
                    float subdivisionWidth = measureWidth / subdivisions;
                    for (int sub = 1; sub < subdivisions; sub++)
                    {
                        float subX = x + (sub * subdivisionWidth);
                        gridContainer.Add(new Box
                        {
                            Width = 1,
                            RelativeSizeAxes = Axes.Y,
                            X = subX,
                            Colour = new Color4(50, 60, 65, 255),
                            Alpha = 0.5f
                        });
                    }
                }
            }
        }
        
        private void loadNotes()
        {
            notesContainer.Clear();
            editorNotes.Clear();
            
            if (beatmap.MapData == null || !beatmap.MapData.Any())
                return;
            
            foreach (var segment in beatmap.MapData)
            {
                if (segment.Notes == null) continue;
                
                foreach (var note in segment.Notes)
                {
                    // Use saved row position, or default to 0 if not set
                    int row = note.Row;
                    if (row < 0 || row >= ROW_COUNT)
                        row = 0;
                    
                    var editorNote = new EditorNote(note, row, this);
                    editorNotes.Add(editorNote);
                    notesContainer.Add(editorNote);
                }
            }
            
            // Sort notes by end time
            editorNotes = editorNotes.OrderBy(n => n.Note.EndTime).ToList();
        }
        
        private void updateWordDisplay()
        {
            wordDisplayContainer.Clear();
            
            // Reconstruct word display from notes
            var segments = getWordSegments();
            
            float xOffset = 50;
            foreach (var segment in segments)
            {
                if (segment.IsSlash)
                {
                    // Display asterisk for slash
                    wordDisplayContainer.Add(new SpriteText
                    {
                        Text = "*",
                        Font = FontUsage.Default.With(family: "Inter-Bold", size: 24),
                        Colour = Color4.White,
                        Position = new Vector2(xOffset, 15)
                    });
                    xOffset += 30;
                }
                else if (!string.IsNullOrEmpty(segment.Word))
                {
                    // Display word
                    var wordContainer = new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Position = new Vector2(xOffset, 10),
                        Masking = true,
                        CornerRadius = 15,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Red,
                                Alpha = 0.8f
                            },
                            new SpriteText
                            {
                                Text = segment.Word.ToUpperInvariant(),
                                Font = FontUsage.Default.With(family: "Inter-Bold", size: 20),
                                Colour = Color4.White,
                                Padding = new MarginPadding { Horizontal = 15, Vertical = 5 }
                            }
                        }
                    };
                    
                    wordDisplayContainer.Add(wordContainer);
                    xOffset += wordContainer.DrawWidth + 20;
                }
            }
        }
        
        private List<WordSegmentDisplay> getWordSegments()
        {
            var segments = new List<WordSegmentDisplay>();
            
            if (beatmap.MapData == null || !beatmap.MapData.Any())
                return segments;
            
            foreach (var segment in beatmap.MapData)
            {
                if (segment.Notes == null || !segment.Notes.Any())
                    continue;
                
                // Build word from characters
                string word = "";
                bool endsWithSlash = false;
                
                foreach (var note in segment.Notes)
                {
                    if (note.Character == "/")
                    {
                        endsWithSlash = true;
                    }
                    else
                    {
                        word += note.Character;
                    }
                }
                
                // Add word if it exists
                if (!string.IsNullOrEmpty(word))
                {
                    segments.Add(new WordSegmentDisplay { Word = word, IsSlash = false });
                }
                
                // Add slash marker if segment ends with slash
                if (endsWithSlash)
                {
                    segments.Add(new WordSegmentDisplay { IsSlash = true });
                }
            }
            
            return segments;
        }
        
        public override bool HandlePositionalInput => true;
        
        protected override bool OnScroll(ScrollEvent e)
        {
            // Ctrl + Scroll = Zoom
            if (e.ControlPressed)
            {
                float zoomDelta = e.ScrollDelta.Y > 0 ? 0.1f : -0.1f;
                zoomLevel = Math.Clamp(zoomLevel + zoomDelta, 0.2f, 5.0f);
                
                // Update timeline width based on zoom (base width = 20000 for 200 measures)
                Width = 20000 * zoomLevel;
                
                // Rebuild grid and reposition notes
                buildGrid();
                foreach (var note in editorNotes)
                {
                    note.UpdatePosition();
                    note.UpdateTailVisibility();
                }
                
                Logger.Log($"[Timeline] Zoom level: {zoomLevel:F2}", LoggingTarget.Runtime, LogLevel.Debug);
                return true;
            }
            
            return base.OnScroll(e);
        }
        
        
        protected override bool OnClick(ClickEvent e)
        {
            // Calculate time and row from click position
            float relativeX = e.MousePosition.X;
            float relativeY = e.MousePosition.Y - 60; // Subtract word display height

            // Debug: log click coordinates and timeline size to diagnose input issues
            Logger.Log($"[Timeline] OnClick at mouse={e.MousePosition}, relativeX={relativeX:F1}, relativeY={relativeY:F1}, DrawSize=(W={DrawWidth:F1}, H={DrawHeight:F1})", LoggingTarget.Runtime, LogLevel.Important);

            if (relativeY < 0)
            {
                Logger.Log("[Timeline] Click was in word display area or above timeline; ignoring.", LoggingTarget.Runtime, LogLevel.Debug);
                return base.OnClick(e);
            }

            double time = calculateTimeFromX(relativeX);
            int row = calculateRowFromY(relativeY);
            
            // Snap to grid immediately if magnet is enabled
            if (magnetEnabled)
            {
                time = snapToGrid(time);
            }

            Logger.Log($"[Timeline] Calculated time={time:F2}, row={row}", LoggingTarget.Runtime, LogLevel.Important);

            // Check if clicking on existing note
            var clickedNote = findNoteAtPosition(time, row);
            if (clickedNote != null)
            {
                selectNote(clickedNote);
                Logger.Log($"[Timeline] Clicked existing note at time={time:F2}, row={row}", LoggingTarget.Runtime, LogLevel.Important);
                return true;
            }

            // Create new note
            Logger.Log($"[Timeline] Creating new note at time={time:F2}, row={row}", LoggingTarget.Runtime, LogLevel.Important);
            createNoteAtPosition(time, row);
            return true;
        }
        
        private void createNoteAtPosition(double endTime, int row)
        {
            // Calculate start time based on default tail length
            double startTime = Math.Max(0, endTime - defaultTailLength);
            
            // Snap start time to grid if magnet is enabled (endTime is already snapped)
            if (magnetEnabled)
            {
                startTime = snapToGrid(startTime);
            }
            
            // Check if end time conflicts with existing notes
            if (editorNotes.Any(n => Math.Abs(n.Note.EndTime - endTime) < 0.01))
            {
                Logger.Log("[Timeline] Cannot create note: End time conflicts with existing note", LoggingTarget.Runtime, LogLevel.Important);
                return;
            }
            
            // Create note with default character (user will need to edit)
            var note = new Note
            {
                Character = "A",
                StartTime = startTime,
                EndTime = endTime,
                Row = row
            };
            
            var editorNote = new EditorNote(note, row, this);
            editorNotes.Add(editorNote);
            notesContainer.Add(editorNote);
            
            // Sort notes by end time
            editorNotes = editorNotes.OrderBy(n => n.Note.EndTime).ToList();
            
            // Update word display
            updateWordDisplay();
            
            // Notify changes
            NotifyContentChanged();
            
            Logger.Log($"[Timeline] Created note at {endTime:F2}s, row {row}", LoggingTarget.Runtime, LogLevel.Debug);
        }
        
        private EditorNote findNoteAtPosition(double time, int row)
        {
            return editorNotes.FirstOrDefault(n =>
                n.Row == row &&
                time >= n.Note.StartTime &&
                time <= n.Note.EndTime);
        }
        
        private void selectNote(EditorNote note)
        {
            if (selectedNote != null)
                selectedNote.IsSelected = false;
            
            selectedNote = note;
            selectedNote.IsSelected = true;
        }
        
        private double calculateTimeFromX(float x)
        {
            // Precise time-to-pixel conversion
            double secondsPerMeasure = 60.0 / tempo * 4; // 4 beats per measure at 4/4 time
            float measureWidth = DrawWidth / TOTAL_MEASURES;
            float pixelsPerSecond = measureWidth / (float)secondsPerMeasure;
            
            return x / pixelsPerSecond;
        }
        
        private int calculateRowFromY(float y)
        {
            // Use fixed row height for precise row calculation
            return Math.Clamp((int)(y / FIXED_ROW_HEIGHT), 0, ROW_COUNT - 1);
        }
        
        private double snapToGrid(double time)
        {
            double secondsPerMeasure = 60.0 / tempo * 4;
            // Music notation: 1/1 = 4 subdivisions, 1/2 = 8, 1/4 = 16, etc.
            double secondsPerSubdivision = secondsPerMeasure / (4 * step);
            return Math.Round(time / secondsPerSubdivision) * secondsPerSubdivision;
        }
        
        public List<WordSegment> GetMapData()
        {
            // Group notes into word segments based on slashes
            var segments = new List<WordSegment>();
            var currentSegment = new List<Note>();
            
            foreach (var editorNote in editorNotes.OrderBy(n => n.Note.EndTime))
            {
                currentSegment.Add(editorNote.Note);
                
                // If note is a slash, finalize current segment
                if (editorNote.Note.Character == "/")
                {
                    segments.Add(new WordSegment { Notes = new List<Note>(currentSegment) });
                    currentSegment.Clear();
                }
            }
            
            // Add remaining notes as final segment if not empty
            if (currentSegment.Any())
            {
                segments.Add(new WordSegment { Notes = new List<Note>(currentSegment) });
            }
            
            return segments;
        }
        
        public void RemoveNote(EditorNote note)
        {
            if (note == null) return;
            
            editorNotes.Remove(note);
            notesContainer.Remove(note, true);
            OnContentChanged?.Invoke();
            Logger.Log($"[Timeline] Deleted note at time {note.Note.EndTime:F2}", LoggingTarget.Runtime, LogLevel.Debug);
        }
        
        public void UpdateAllNoteTails()
        {
            foreach (var note in editorNotes)
            {
                note.UpdateTailVisibility();
            }
        }
        
        public void NotifyContentChanged()
        {
            OnContentChanged?.Invoke();
        }
        
        private class WordSegmentDisplay
        {
            public string Word { get; set; }
            public bool IsSlash { get; set; }
        }
    }
    
    /// <summary>
    /// Visual representation of a note in the editor.
    /// </summary>
    public partial class EditorNote : CompositeDrawable
    {
        public Note Note { get; }
        public int Row { get; }
        private readonly TypeBeatTimeline timeline;
        
        private Box tailBox;
        private Box headBox;
        private SpriteText characterText;
        private osu.Framework.Graphics.UserInterface.TextBox characterInput;
        private bool isSelected;
        private bool isEditing;
        private bool isDragging;
        private bool isResizing;
        private Vector2 dragStartPosition;
        private double dragStartTime;
        private double dragStartEndTime;
        
        
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                updateAppearance();
            }
        }
        
        public EditorNote(Note note, int row, TypeBeatTimeline timeline)
        {
            Note = note;
            Row = row;
            this.timeline = timeline;
            
            RelativeSizeAxes = Axes.None;
        }
        
        public override bool HandlePositionalInput => true;
        
        protected override void LoadComplete()
        {
            base.LoadComplete();
            
            updatePosition();
            buildVisuals();
        }
        
        private void buildVisuals()
        {
            // Calculate subdivision width for note head
            float measureWidth = timeline.DrawWidth / TypeBeatTimeline.TOTAL_MEASURES;
            // Music notation: 1/1 = 4 subdivisions per measure
            float subdivisionWidth = measureWidth / (4 * timeline.step);
            
            InternalChildren = new Drawable[]
            {
                // Tail (bold horizontal line - visible based on showTail setting)
                tailBox = new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 6,
                    Colour = getColorForCharacter(Note.Character),
                    Alpha = timeline.showTail ? 1.0f : 0f,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft
                },
                // Head (bold rectangle at end)
                headBox = new Box
                {
                    Width = subdivisionWidth,
                    Height = Height,
                    Colour = getColorForCharacter(Note.Character),
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.Centre
                },
                // Character text
                characterText = new SpriteText
                {
                    Text = Note.Character.ToUpperInvariant(),
                    Font = FontUsage.Default.With(family: "Inter-Bold", size: 18),
                    Colour = Color4.White,
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.Centre,
                    Margin = new MarginPadding { Right = 10 }
                },
                // Character input (hidden by default)
                characterInput = new osu.Framework.Graphics.UserInterface.BasicTextBox
                {
                    Size = new Vector2(40, 30),
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.Centre,
                    Alpha = 0,
                    CommitOnFocusLost = true
                }
            };
            
            // Subscribe to input events
            characterInput.OnCommit += (sender, newText) =>
            {
                if (!string.IsNullOrEmpty(characterInput.Text) && characterInput.Text.Length > 0)
                {
                    // Take only the first character
                    Note.Character = characterInput.Text.Substring(0, 1).ToUpperInvariant();
                    characterText.Text = Note.Character;
                    updateColors();
                    timeline.NotifyContentChanged();
                }
                exitEditMode();
            };
        }
        
        public void UpdatePosition()
        {
            updatePosition();
        }
        
        public void UpdateTailVisibility()
        {
            tailBox.Alpha = timeline.showTail ? 1.0f : 0f;
        }
        
        private void updatePosition()
        {
            // Use fixed row height
            float rowHeight = TypeBeatTimeline.FIXED_ROW_HEIGHT;
            
            // Calculate time-to-pixel conversion with precision
            double secondsPerMeasure = 60.0 / timeline.tempo * 4; // 4 beats per measure at 4/4 time
            float measureWidth = timeline.DrawWidth / TypeBeatTimeline.TOTAL_MEASURES;
            float pixelsPerSecond = measureWidth / (float)secondsPerMeasure;
            
            // Precise position calculations
            float startX = (float)(Note.StartTime * pixelsPerSecond);
            float endX = (float)(Note.EndTime * pixelsPerSecond);
            float y = Row * rowHeight;
            
            // Set position and size with precision
            X = startX;
            Y = y;
            Width = Math.Max(endX - startX, 10f);
            Height = rowHeight;
        }
        
        private void updateAppearance()
        {
            if (isSelected)
            {
                // Brighten when selected
                tailBox.FadeColour(Color4.Yellow, 200, Easing.OutQuint);
                headBox.FadeColour(Color4.Yellow, 200, Easing.OutQuint);
            }
            else
            {
                // Return to original color
                var originalColor = getColorForCharacter(Note.Character);
                tailBox.FadeColour(originalColor, 200, Easing.OutQuint);
                headBox.FadeColour(originalColor, 200, Easing.OutQuint);
            }
        }
        
        private Color4 getColorForCharacter(string character)
        {
            // Slash is yellow/green
            if (character == "/")
                return new Color4(180, 200, 50, 255);
            
            // Regular characters are orange
            return new Color4(255, 140, 50, 255);
        }
        
        protected override bool OnClick(ClickEvent e)
        {
            // Let parent handle selection
            return false;
        }
        
        protected override bool OnDoubleClick(DoubleClickEvent e)
        {
            // Enter edit mode
            enterEditMode();
            return true;
        }
        
        
        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button == osuTK.Input.MouseButton.Right)
            {
                // Delete this note
                deleteNote();
                return true;
            }
            
            if (e.Button == osuTK.Input.MouseButton.Left)
            {
                // Check if clicking near the tail start (left side) for resizing
                float clickX = e.MousePosition.X;
                if (clickX < 20) // Within 20px of the left edge
                {
                    isResizing = true;
                    dragStartPosition = e.MousePosition;
                    dragStartTime = Note.StartTime;
                    return true;
                }
                else
                {
                    // Otherwise, start dragging
                    isDragging = true;
                    dragStartPosition = e.MousePosition;
                    dragStartTime = Note.StartTime;
                    dragStartEndTime = Note.EndTime;
                    return true;
                }
            }
            
            return base.OnMouseDown(e);
        }
        
        protected override bool OnDragStart(DragStartEvent e)
        {
            return isDragging || isResizing;
        }
        
        protected override void OnDrag(DragEvent e)
        {
            // Calculate precise pixels per second
            double secondsPerMeasure = 60.0 / timeline.tempo * 4;
            float measureWidth = timeline.DrawWidth / TypeBeatTimeline.TOTAL_MEASURES;
            float pixelsPerSecond = measureWidth / (float)secondsPerMeasure;
            
            if (isResizing)
            {
                // Resize the tail by changing start time (keep end time fixed)
                float deltaX = e.MousePosition.X - dragStartPosition.X;
                double deltaTime = deltaX / pixelsPerSecond;
                
                Note.StartTime = Math.Max(0, Math.Min(Note.EndTime - 0.1, dragStartTime + deltaTime));
                
                if (timeline.magnetEnabled)
                {
                    Note.StartTime = snapToGrid(Note.StartTime);
                }
                
                updatePosition();
                timeline.NotifyContentChanged();
            }
            else if (isDragging)
            {
                // Move the entire note
                float deltaX = e.MousePosition.X - dragStartPosition.X;
                double deltaTime = deltaX / pixelsPerSecond;
                
                double noteDuration = dragStartEndTime - dragStartTime;
                Note.StartTime = Math.Max(0, dragStartTime + deltaTime);
                Note.EndTime = Note.StartTime + noteDuration;
                
                if (timeline.magnetEnabled)
                {
                    Note.StartTime = snapToGrid(Note.StartTime);
                    Note.EndTime = Note.StartTime + noteDuration;
                }
                
                updatePosition();
                timeline.NotifyContentChanged();
            }
        }
        
        protected override void OnDragEnd(DragEndEvent e)
        {
            isDragging = false;
            isResizing = false;
        }
        
        private double snapToGrid(double time)
        {
            double secondsPerMeasure = 60.0 / timeline.tempo * 4;
            double secondsPerSubdivision = secondsPerMeasure / timeline.step;
            return Math.Round(time / secondsPerSubdivision) * secondsPerSubdivision;
        }
        
        private void enterEditMode()
        {
            isEditing = true;
            characterText.FadeOut(100);
            characterInput.FadeIn(100);
            characterInput.Text = Note.Character;
            Schedule(() => GetContainingFocusManager()?.ChangeFocus(characterInput));
        }
        
        private void exitEditMode()
        {
            isEditing = false;
            characterInput.FadeOut(100);
            characterText.FadeIn(100);
        }
        
        private void deleteNote()
        {
            timeline.RemoveNote(this);
        }
        
        private void updateColors()
        {
            var color = getColorForCharacter(Note.Character);
            tailBox.FadeColour(color, 200, Easing.OutQuint);
            headBox.FadeColour(color, 200, Easing.OutQuint);
        }
    }
}
