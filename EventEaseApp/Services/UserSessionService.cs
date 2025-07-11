using Blazored.LocalStorage;
using EventEaseApp.Models;

namespace EventEaseApp.Services
{
    public class UserSessionService
    {
        private readonly ILocalStorageService _localStorage;
        private const string SESSION_KEY = "eventease_user_session";
        private const string PREFERENCES_KEY = "eventease_user_preferences";
        private UserSession? _currentSession;

        public UserSessionService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<UserSession?> GetCurrentSessionAsync()
        {
            if (_currentSession == null)
            {
                _currentSession = await _localStorage.GetItemAsync<UserSession>(SESSION_KEY);
            }
            return _currentSession;
        }

        public async Task<UserSession> CreateOrUpdateSessionAsync(string email, string firstName, string lastName, string phone, string company)
        {
            var session = await GetCurrentSessionAsync();
            
            if (session == null)
            {
                session = new UserSession();
            }

            session.Email = email;
            session.FirstName = firstName;
            session.LastName = lastName;
            session.Phone = phone;
            session.Company = company;
            session.LastAccessedAt = DateTime.Now;

            await _localStorage.SetItemAsync(SESSION_KEY, session);
            _currentSession = session;
            
            return session;
        }

        public async Task ClearSessionAsync()
        {
            await _localStorage.RemoveItemAsync(SESSION_KEY);
            _currentSession = null;
        }

        public async Task<Dictionary<string, string>> GetUserPreferencesAsync()
        {
            var preferences = await _localStorage.GetItemAsync<Dictionary<string, string>>(PREFERENCES_KEY);
            return preferences ?? new Dictionary<string, string>();
        }

        public async Task SaveUserPreferenceAsync(string key, string value)
        {
            var preferences = await GetUserPreferencesAsync();
            preferences[key] = value;
            await _localStorage.SetItemAsync(PREFERENCES_KEY, preferences);
        }

        public async Task<string?> GetUserPreferenceAsync(string key)
        {
            var preferences = await GetUserPreferencesAsync();
            return preferences.TryGetValue(key, out var value) ? value : null;
        }

        public async Task<bool> HasValidSessionAsync()
        {
            var session = await GetCurrentSessionAsync();
            if (session == null) return false;
            
            // Session is valid for 30 days
            var sessionAge = DateTime.Now - session.LastAccessedAt;
            return sessionAge.TotalDays < 30;
        }

        public async Task UpdateSessionAccessTimeAsync()
        {
            var session = await GetCurrentSessionAsync();
            if (session != null)
            {
                session.LastAccessedAt = DateTime.Now;
                await _localStorage.SetItemAsync(SESSION_KEY, session);
            }
        }
    }
}