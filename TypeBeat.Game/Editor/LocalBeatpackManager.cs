// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace TypeBeat.Game.Editor
{
    /// <summary>
    /// Manages local editor beatpack projects.
    /// </summary>
    public partial class LocalBeatpackManager : Component
    {
        public Bindable<LocalBeatpack> CurrentBeatpack { get; } = new Bindable<LocalBeatpack>();

        public BindableList<LocalBeatpack> Projects { get; } = new BindableList<LocalBeatpack>();

        private Storage editorStorage;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            editorStorage = host.Storage.GetStorageForDirectory("EditorProjects");
            
            // Ensure directory exists
            if (!editorStorage.ExistsDirectory(string.Empty))
            {
                editorStorage.GetFullPath(string.Empty, true);
                Logger.Log("[LocalBeatpackManager] Created EditorProjects directory", LoggingTarget.Runtime, LogLevel.Debug);
            }
            
            LoadProjects();
        }

        /// <summary>
        /// Loads all projects from the EditorProjects directory.
        /// </summary>
        public void LoadProjects()
        {
            Projects.Clear();

            if (editorStorage == null)
            {
                Logger.Log("Editor storage not initialized during LoadProjects", LoggingTarget.Runtime, LogLevel.Error);
                return;
            }

            var fullPath = editorStorage.GetFullPath(string.Empty);
            Logger.Log($"[LocalBeatpackManager] Loading projects from: {fullPath}", LoggingTarget.Runtime, LogLevel.Important);

            if (!editorStorage.ExistsDirectory(string.Empty))
            {
                Logger.Log("EditorProjects directory does not exist yet.", LoggingTarget.Runtime, LogLevel.Debug);
                return;
            }

            var files = editorStorage.GetFiles(string.Empty, "*.json");
            Logger.Log($"[LocalBeatpackManager] Found {files.Count()} JSON files", LoggingTarget.Runtime, LogLevel.Important);

            foreach (var file in files)
            {
                try
                {
                    Logger.Log($"[LocalBeatpackManager] Loading project from: {file}", LoggingTarget.Runtime, LogLevel.Debug);
                    using (var stream = editorStorage.GetStream(file))
                    using (var reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        var project = LocalBeatpack.FromJson(json);
                        Projects.Add(project);
                        Logger.Log($"[LocalBeatpackManager] ✓ Loaded project: {project.Name}", LoggingTarget.Runtime, LogLevel.Important);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to load project {file}: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                    Logger.Log($"Stack trace: {ex.StackTrace}", LoggingTarget.Runtime, LogLevel.Error);
                }
            }

            Logger.Log($"[LocalBeatpackManager] ✓ Successfully loaded {Projects.Count} editor projects.", LoggingTarget.Runtime, LogLevel.Important);
        }

        /// <summary>
        /// Saves a project to disk.
        /// </summary>
        public void SaveProject(LocalBeatpack beatpack)
        {
            if (beatpack == null)
            {
                Logger.Log("Cannot save null beatpack", LoggingTarget.Runtime, LogLevel.Error);
                return;
            }

            if (editorStorage == null)
            {
                Logger.Log("Editor storage not initialized", LoggingTarget.Runtime, LogLevel.Error);
                return;
            }

            beatpack.LastModified = DateTime.Now;

            try
            {
                string json = beatpack.ToJson();
                string filename = $"{beatpack.Id}.json";
                var fullPath = editorStorage.GetFullPath(filename);

                Logger.Log($"[LocalBeatpackManager] Saving project to: {fullPath}", LoggingTarget.Runtime, LogLevel.Important);

                using (var stream = editorStorage.GetStream(filename, FileAccess.Write, FileMode.Create))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(json);
                    writer.Flush();
                }

                Logger.Log($"[LocalBeatpackManager] ✓ Saved project {beatpack.Name} ({beatpack.Id})", LoggingTarget.Runtime, LogLevel.Important);
                
                // Verify the file was actually written
                if (editorStorage.Exists(filename))
                {
                    Logger.Log($"[LocalBeatpackManager] ✓ Verified file exists: {filename}", LoggingTarget.Runtime, LogLevel.Debug);
                }
                else
                {
                    Logger.Log($"[LocalBeatpackManager] ✗ WARNING: File not found after save: {filename}", LoggingTarget.Runtime, LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to save project {beatpack.Name}: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                Logger.Log($"Stack trace: {ex.StackTrace}", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        /// <summary>
        /// Creates a new project with the given name and metadata.
        /// Copies all media files to local storage to preserve them.
        /// </summary>
        public LocalBeatpack CreateNewProject(string name, string title = "", string artist = "", string description = "",
                                              List<string> tags = null, string musicFilePath = "", string backgroundImagePath = "",
                                              string videoPath = "")
        {
            var beatpack = new LocalBeatpack
            {
                Name = name,
                Title = title,
                Artist = artist,
                Description = description,
                Tags = tags ?? new List<string>(),
                LastModified = DateTime.Now
            };

            // Create project-specific directory for media files
            string projectDir = beatpack.Id;
            
            try
            {
                // Copy music file to local storage (required)
                if (!string.IsNullOrEmpty(musicFilePath) && File.Exists(musicFilePath))
                {
                    string musicFileName = Path.GetFileName(musicFilePath);
                    string localMusicPath = Path.Combine(projectDir, musicFileName);
                    
                    using (var sourceStream = File.OpenRead(musicFilePath))
                    using (var destStream = editorStorage.GetStream(localMusicPath, FileAccess.Write, FileMode.Create))
                    {
                        sourceStream.CopyTo(destStream);
                    }
                    
                    beatpack.MusicFilePath = localMusicPath;
                    Logger.Log($"Copied music file to: {localMusicPath}", LoggingTarget.Runtime, LogLevel.Debug);
                }
                else
                {
                    Logger.Log($"Music file not found or invalid: {musicFilePath}", LoggingTarget.Runtime, LogLevel.Error);
                }

                // Copy background image to local storage (optional)
                if (!string.IsNullOrEmpty(backgroundImagePath) && File.Exists(backgroundImagePath))
                {
                    string bgFileName = Path.GetFileName(backgroundImagePath);
                    string localBgPath = Path.Combine(projectDir, bgFileName);
                    
                    using (var sourceStream = File.OpenRead(backgroundImagePath))
                    using (var destStream = editorStorage.GetStream(localBgPath, FileAccess.Write, FileMode.Create))
                    {
                        sourceStream.CopyTo(destStream);
                    }
                    
                    beatpack.BackgroundImagePath = localBgPath;
                    Logger.Log($"Copied background image to: {localBgPath}", LoggingTarget.Runtime, LogLevel.Debug);
                }

                // Copy video file to local storage (optional)
                if (!string.IsNullOrEmpty(videoPath) && File.Exists(videoPath))
                {
                    string videoFileName = Path.GetFileName(videoPath);
                    string localVideoPath = Path.Combine(projectDir, videoFileName);
                    
                    using (var sourceStream = File.OpenRead(videoPath))
                    using (var destStream = editorStorage.GetStream(localVideoPath, FileAccess.Write, FileMode.Create))
                    {
                        sourceStream.CopyTo(destStream);
                    }
                    
                    beatpack.VideoPath = localVideoPath;
                    Logger.Log($"Copied video file to: {localVideoPath}", LoggingTarget.Runtime, LogLevel.Debug);
                }

                Logger.Log($"Created new project with local file copies: {beatpack.Name} ({beatpack.Id})", LoggingTarget.Runtime, LogLevel.Important);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error copying files for project {name}: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                // Even if file copy fails, we still create the project with original paths
                beatpack.MusicFilePath = musicFilePath;
                beatpack.BackgroundImagePath = backgroundImagePath;
                beatpack.VideoPath = videoPath;
            }

            Projects.Add(beatpack);
            SaveProject(beatpack);
            CurrentBeatpack.Value = beatpack;

            return beatpack;
        }

        /// <summary>
        /// Deletes a project from disk and the list, including all associated media files.
        /// </summary>
        public void DeleteProject(string id)
        {
            var project = Projects.FirstOrDefault(p => p.Id == id);

            if (project == null)
                return;

            try
            {
                // Delete the project JSON file
                string filename = GetProjectPath(id);
                editorStorage.Delete(filename);
                
                // Delete the project directory with all media files
                string projectDir = id;
                if (editorStorage.ExistsDirectory(projectDir))
                {
                    editorStorage.DeleteDirectory(projectDir);
                    Logger.Log($"Deleted project directory: {projectDir}", LoggingTarget.Runtime, LogLevel.Debug);
                }

                Projects.Remove(project);

                if (CurrentBeatpack.Value?.Id == id)
                    CurrentBeatpack.Value = null;

                Logger.Log($"Deleted project {project.Name} ({id})", LoggingTarget.Runtime, LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to delete project {id}: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        /// <summary>
        /// Saves a specific beatmap within a beatpack.
        /// </summary>
        public void SaveBeatmap(LocalBeatpack beatpack, LocalBeatmap beatmap)
        {
            if (beatpack == null || beatmap == null)
            {
                Logger.Log("Cannot save null beatpack or beatmap", LoggingTarget.Runtime, LogLevel.Error);
                return;
            }

            // Update the beatmap in the beatpack's list
            var existingBeatmap = beatpack.LocalBeatmaps?.FirstOrDefault(b => b.Id == beatmap.Id);
            if (existingBeatmap != null)
            {
                // Update existing beatmap
                int index = beatpack.LocalBeatmaps.IndexOf(existingBeatmap);
                beatpack.LocalBeatmaps[index] = beatmap;
                Logger.Log($"[LocalBeatpackManager] Updated beatmap: {beatmap.DifficultyName}", LoggingTarget.Runtime, LogLevel.Important);
            }
            else
            {
                // Add new beatmap if not found
                if (beatpack.LocalBeatmaps == null)
                    beatpack.LocalBeatmaps = new List<LocalBeatmap>();
                
                beatpack.LocalBeatmaps.Add(beatmap);
                Logger.Log($"[LocalBeatpackManager] Added new beatmap: {beatmap.DifficultyName}", LoggingTarget.Runtime, LogLevel.Important);
            }

            // Save the entire beatpack
            SaveProject(beatpack);
        }

        /// <summary>
        /// Exports a beatpack to the game's beatmaps directory for testing.
        /// </summary>
        public bool ExportForTesting(LocalBeatpack beatpack)
        {
            if (beatpack == null)
            {
                Logger.Log("Cannot export null beatpack", LoggingTarget.Runtime, LogLevel.Error);
                return false;
            }

            try
            {
                // TODO: Implement export to game's beatmaps directory
                // For now, just log that we would export
                Logger.Log($"[LocalBeatpackManager] Would export {beatpack.Name} for testing", LoggingTarget.Runtime, LogLevel.Important);
                Logger.Log($"[LocalBeatpackManager] TODO: Copy to game beatmaps directory", LoggingTarget.Runtime, LogLevel.Important);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to export project {beatpack.Name}: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Gets the file path for a project by ID.
        /// </summary>
        private string GetProjectPath(string id)
        {
            return $"{id}.json";
        }
    }
}

