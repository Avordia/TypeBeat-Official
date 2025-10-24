using System;
using System.Threading.Tasks;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using Supabase;
using Supabase.Gotrue;
using TypeBeat.Game.Online.Models;
using static Postgrest.Constants;

#nullable enable

namespace TypeBeat.Game.Online
{
    public partial class AuthenticationService : Component
    {
        public Bindable<UserProfile?> CurrentUser { get; } = new Bindable<UserProfile?>();

        public bool IsLoggedIn => CurrentUser.Value != null;

        public async Task<(bool success, string message)> LoginAsync(string usernameOrEmail, string password)
        {
            try
            {
                if (BackendClient.Client == null)
                {
                    return (false, "Backend not available");
                }

                string email;
                string username;
                
                if (usernameOrEmail.Contains("@"))
                {
                    // User entered an email - use it directly
                    email = usernameOrEmail;
                    username = usernameOrEmail.Split('@')[0];
                }
                else
                {
                    // User entered a username - need to look up their email from the database
                    username = usernameOrEmail;
                    
                    try
                    {
                        Logger.Log($"[DEBUG] Looking up email for username: '{username}'", LoggingTarget.Runtime, LogLevel.Important);
                        
                        // Query the profiles table to get the email for this username
                        var profileQuery = await BackendClient.Client
                            .From<UserProfile>()
                            .Select("email")
                            .Filter("username", Operator.Equals, username)
                            .Single();

                        if (profileQuery?.Email == null)
                        {
                            Logger.Log($"[DEBUG] Username '{username}' not found in database", LoggingTarget.Runtime, LogLevel.Important);
                            return (false, "User not found");
                        }

                        email = profileQuery.Email;
                        Logger.Log($"[DEBUG] ✓ Found email for username '{username}': {email}", LoggingTarget.Runtime, LogLevel.Important);
                    }
                    catch (Exception lookupEx)
                    {
                        Logger.Log($"[DEBUG] ✗ Failed to lookup email for username '{username}': {lookupEx.Message}", LoggingTarget.Runtime, LogLevel.Error);
                        return (false, "User not found");
                    }
                }

                Logger.Log($"Attempting login with email: {email}", LoggingTarget.Runtime, LogLevel.Important);
                
                // Use the correct Supabase auth method
                var response = await BackendClient.Client.Auth.SignInWithPassword(email, password);
                
                Logger.Log($"Login response received. User null: {response?.User == null}", LoggingTarget.Runtime, LogLevel.Important);
                
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (response.User is { Id: not null } user)
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                {
                    loadUserProfile(user.Id, username);
                    return (true, "Login successful");
                }

                return (false, "Invalid credentials");
            }
            catch (Exception ex)
            {
                Logger.Log($"Login exception: {ex.GetType().Name}", LoggingTarget.Runtime, LogLevel.Error);
                Logger.Log($"Login error message: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                if (ex.InnerException != null)
                {
                    Logger.Log($"Inner exception: {ex.InnerException.Message}", LoggingTarget.Runtime, LogLevel.Error);
                }
                
                // Provide user-friendly error messages
                if (ex.Message.Contains("email_not_confirmed"))
                {
                    return (false, "Please verify your email before logging in. Check your inbox for a confirmation link.");
                }
                else if (ex.Message.Contains("invalid_credentials"))
                {
                    return (false, "Invalid username or password.");
                }
                
                return (false, $"Login failed: {ex.Message}");
            }
        }

        // --- CHANGED HERE: Method signature no longer takes 'email' ---
        public async Task<(bool success, string message)> RegisterAsync(string username, string email, string password)
        {
            try
            {
                if (BackendClient.Client == null)
                {
                    return (false, "Backend not available");
                }

                var options = new SignUpOptions
                {
                    Data = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "username", username } // This passes the real username to your SQL trigger
                    }
                };

                // Use the real email provided by the user
                var response = await BackendClient.Client.Auth.SignUp(email, password, options);
                
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (response.User is { Id: not null } user)
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                {
                    loadUserProfile(user.Id, username);
                    
                    // Check if email confirmation is required
                    if (user.EmailConfirmedAt == null)
                    {
                        Logger.Log("Registration successful - email confirmation required", LoggingTarget.Runtime, LogLevel.Important);
                        return (true, "Registration successful! Please check your email to confirm your account before logging in.");
                    }
                    
                    return (true, "Registration successful! You can now log in.");
                }

                return (false, "Registration failed");
            }
            catch (Exception ex)
            {
                Logger.Log($"Registration failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                
                if (ex.Message.Contains("already registered"))
                {
                    return (false, "This email is already registered. Please try logging in.");
                }
                
                return (false, "Registration failed: " + ex.Message);
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                if (BackendClient.Client != null)
                {
                    await BackendClient.Client.Auth.SignOut();
                }
                
                CurrentUser.Value = null;
                Logger.Log("User logged out");
            }
            catch (Exception ex)
            {
                Logger.Log($"Logout failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        private async void loadUserProfile(string userId, string username)
        {
            try
            {
                if (BackendClient.Client == null)
                {
                    return;
                }

                Logger.Log($"[DEBUG] Loading full profile from database for user ID: {userId}", LoggingTarget.Runtime, LogLevel.Important);

                // Query the actual profile from the database
                var response = await BackendClient.Client
                    .From<UserProfile>()
                    .Select("*")
                    .Filter("id", Operator.Equals, userId)
                    .Single();

                if (response != null)
                {
                    CurrentUser.Value = response;
                    Logger.Log($"[DEBUG] ✓ Loaded profile for user: {response.Username} (ID: {userId})", LoggingTarget.Runtime, LogLevel.Important);
                    Logger.Log($"[DEBUG] Profile details - Level: {response.Level}, XP: {response.Xp}", LoggingTarget.Runtime, LogLevel.Important);
                    Logger.Log($"[DEBUG] Avatar URL: {(string.IsNullOrEmpty(response.AvatarUrl) ? "NONE" : response.AvatarUrl)}", LoggingTarget.Runtime, LogLevel.Important);
                    Logger.Log($"[DEBUG] Email: {response.Email}", LoggingTarget.Runtime, LogLevel.Important);
                }
                else
                {
                    Logger.Log($"[DEBUG] ✗ Profile query returned null for user ID: {userId}", LoggingTarget.Runtime, LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[DEBUG] ✗ Failed to load user profile: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                if (ex.InnerException != null)
                {
                    Logger.Log($"[DEBUG] Inner exception: {ex.InnerException.Message}", LoggingTarget.Runtime, LogLevel.Error);
                }
                
                // Fallback to basic profile
                CurrentUser.Value = new UserProfile
                {
                    Id = userId,
                    Username = username,
                    Level = 1,
                    Xp = 0,
                    AvatarUrl = "",
                    Email = "",
                    CreatedAt = DateTime.Now
                };
            }
        }
    }
}