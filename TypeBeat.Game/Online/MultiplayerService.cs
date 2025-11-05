using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using osu.Framework.Logging;

namespace TypeBeat.Game.Online
{
    public class MultiplayerService
    {
        private readonly HttpClient httpClient;
        private readonly AuthenticationService authService;
        private const string SUPABASE_URL = "https://fjxnfrdssccqzmapavch.supabase.co";
        private const string SUPABASE_ANON_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZqeG5mcmRzc2NjcXptYXBhdmNoIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjEyODcwMDAsImV4cCI6MjA3Njg2MzAwMH0.pxIcI-fDDxkqPTopSlbymo-9VweqIPpCZA17u1igHDk";
        
        // Events
        public event Action<List<Room>> OnRoomsUpdated;
        public event Action<Room> OnRoomJoined;
        public event Action<Room> OnRoomUpdated;
        public event Action<List<MatchScore>> OnScoresUpdated;
        public event Action<string> OnError;
        
        public MultiplayerService(AuthenticationService authService)
        {
            this.authService = authService;
            httpClient = new HttpClient();
            setupHttpClient();
        }
        
        private void setupHttpClient()
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("apikey", SUPABASE_ANON_KEY);
            
            var token = authService.GetAccessToken();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SUPABASE_ANON_KEY}");
            }
        }
        
        public async Task<List<Room>> GetActiveRooms()
        {
            try
            {
                var response = await httpClient.GetAsync(
                    $"{SUPABASE_URL}/rest/v1/rooms?status=eq.waiting&select=*,profiles!rooms_host_id_fkey(username,avatar_url),beatpacks!rooms_selected_beatpack_id_fkey(id,title,artist,creator_id,profiles!beatpacks_creator_id_fkey(username)),room_participants(user_id,profiles!room_participants_user_id_fkey(username,avatar_url))"
                );
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var rooms = JsonConvert.DeserializeObject<List<Room>>(json);
                    OnRoomsUpdated?.Invoke(rooms);
                    return rooms;
                }
                
                Logger.Log($"Failed to get rooms: {response.StatusCode}", LoggingTarget.Network, LogLevel.Error);
                return new List<Room>();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error getting rooms: {ex.Message}", LoggingTarget.Network, LogLevel.Error);
                OnError?.Invoke($"Failed to load rooms: {ex.Message}");
                return new List<Room>();
            }
        }
        
        public async Task<Room> CreateRoom(string roomName, long beatpackId, int maxPlayers = 4)
        {
            try
            {
                if (!authService.IsLoggedIn)
                {
                    OnError?.Invoke("You must be logged in to create a room");
                    return null;
                }
                
                var userId = authService.GetUserId();
                var requestData = new
                {
                    host_id = userId,
                    room_name = roomName,
                    status = "waiting",
                    max_players = maxPlayers,
                    selected_beatpack_id = beatpackId
                };
                
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync($"{SUPABASE_URL}/rest/v1/rooms", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    Logger.Log($"Room creation response: {responseJson}", LoggingTarget.Network, LogLevel.Debug);
                    
                    var rooms = JsonConvert.DeserializeObject<List<Room>>(responseJson);
                    var room = rooms?.FirstOrDefault();
                    
                    if (room != null)
                    {
                        // Join the room as host
                        await JoinRoom(room.Id);
                        Logger.Log($"Successfully created room: {room.RoomName} (ID: {room.Id})", LoggingTarget.Network, LogLevel.Important);
                        return room;
                    }
                    else
                    {
                        Logger.Log($"Room creation returned null or empty list", LoggingTarget.Network, LogLevel.Error);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Logger.Log($"Failed to create room: {response.StatusCode} - {errorContent}", LoggingTarget.Network, LogLevel.Error);
                }
                
                OnError?.Invoke("Failed to create room");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error creating room: {ex.Message}", LoggingTarget.Network, LogLevel.Error);
                OnError?.Invoke($"Error creating room: {ex.Message}");
                return null;
            }
        }
        
        public async Task<bool> JoinRoom(long roomId)
        {
            try
            {
                if (!authService.IsLoggedIn)
                {
                    OnError?.Invoke("You must be logged in to join a room");
                    return false;
                }
                
                var userId = authService.GetUserId();
                var requestData = new
                {
                    room_id = roomId,
                    user_id = userId,
                    status = "not_ready"
                };
                
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync($"{SUPABASE_URL}/rest/v1/room_participants", content);
                
                if (response.IsSuccessStatusCode)
                {
                    // Get full room details
                    var roomResponse = await httpClient.GetAsync(
                        $"{SUPABASE_URL}/rest/v1/rooms?id=eq.{roomId}&select=*,profiles!rooms_host_id_fkey(username,avatar_url)"
                    );
                    
                    if (roomResponse.IsSuccessStatusCode)
                    {
                        var roomJson = await roomResponse.Content.ReadAsStringAsync();
                        var rooms = JsonConvert.DeserializeObject<List<Room>>(roomJson);
                        var room = rooms?.FirstOrDefault();
                        
                        if (room != null)
                        {
                            OnRoomJoined?.Invoke(room);
                            return true;
                        }
                    }
                }
                
                Logger.Log($"Failed to join room: {response.StatusCode}", LoggingTarget.Network, LogLevel.Error);
                OnError?.Invoke("Failed to join room");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error joining room: {ex.Message}", LoggingTarget.Network, LogLevel.Error);
                OnError?.Invoke($"Error joining room: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> LeaveRoom(long roomId)
        {
            try
            {
                if (!authService.IsLoggedIn) return false;
                
                var userId = authService.GetUserId();
                var response = await httpClient.DeleteAsync(
                    $"{SUPABASE_URL}/rest/v1/room_participants?room_id=eq.{roomId}&user_id=eq.{userId}"
                );
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error leaving room: {ex.Message}", LoggingTarget.Network, LogLevel.Error);
                return false;
            }
        }
        
        public async Task<bool> DeleteRoom(long roomId)
        {
            try
            {
                if (!authService.IsLoggedIn) return false;
                
                // Delete room (will cascade delete participants and scores)
                var response = await httpClient.DeleteAsync(
                    $"{SUPABASE_URL}/rest/v1/rooms?id=eq.{roomId}"
                );
                
                if (response.IsSuccessStatusCode)
                {
                    Logger.Log($"Successfully deleted room {roomId}", LoggingTarget.Network, LogLevel.Important);
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Logger.Log($"Failed to delete room: {response.StatusCode} - {error}", LoggingTarget.Network, LogLevel.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error deleting room: {ex.Message}", LoggingTarget.Network, LogLevel.Error);
                return false;
            }
        }
        
        public async Task<bool> UpdateRoomStatus(long roomId, string status)
        {
            try
            {
                var requestData = new { status = status };
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), 
                    $"{SUPABASE_URL}/rest/v1/rooms?id=eq.{roomId}")
                {
                    Content = content
                };
                
                var response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error updating room status: {ex.Message}", LoggingTarget.Network, LogLevel.Error);
                return false;
            }
        }
        
        public async Task<bool> SelectBeatmap(long roomId, long beatmapId)
        {
            try
            {
                var requestData = new { selected_beatmap_id = beatmapId };
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), 
                    $"{SUPABASE_URL}/rest/v1/rooms?id=eq.{roomId}")
                {
                    Content = content
                };
                
                var response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error selecting beatmap: {ex.Message}", LoggingTarget.Network, LogLevel.Error);
                return false;
            }
        }
        
        public async Task<bool> SubmitScore(long roomId, long beatmapId, long score, float accuracy, int maxCombo)
        {
            try
            {
                if (!authService.IsLoggedIn) return false;
                
                var userId = authService.GetUserId();
                var requestData = new
                {
                    room_id = roomId,
                    user_id = userId,
                    beatmap_id = beatmapId,
                    score = score,
                    accuracy = accuracy,
                    max_combo = maxCombo
                };
                
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync($"{SUPABASE_URL}/rest/v1/match_scores", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error submitting score: {ex.Message}", LoggingTarget.Network, LogLevel.Error);
                return false;
            }
        }
        
        public async Task<List<MatchScore>> GetMatchScores(long roomId)
        {
            try
            {
                var response = await httpClient.GetAsync(
                    $"{SUPABASE_URL}/rest/v1/match_scores?room_id=eq.{roomId}&select=*,profiles!match_scores_user_id_fkey(username,avatar_url)&order=score.desc"
                );
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var scores = JsonConvert.DeserializeObject<List<MatchScore>>(json);
                    OnScoresUpdated?.Invoke(scores);
                    return scores;
                }
                
                return new List<MatchScore>();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error getting scores: {ex.Message}", LoggingTarget.Network, LogLevel.Error);
                return new List<MatchScore>();
            }
        }
        
        public string GetUserId()
        {
            return authService.GetUserId();
        }
        
        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}
