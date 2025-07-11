using Blazored.LocalStorage;
using EventEaseApp.Models;

namespace EventEaseApp.Services
{
    public class RegistrationService
    {
        private readonly ILocalStorageService _localStorage;
        private const string REGISTRATIONS_KEY = "eventease_registrations";
        private const string ATTENDANCE_KEY = "eventease_attendance";

        public RegistrationService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<List<Registration>> GetAllRegistrationsAsync()
        {
            var registrations = await _localStorage.GetItemAsync<List<Registration>>(REGISTRATIONS_KEY);
            return registrations ?? new List<Registration>();
        }

        public async Task<List<Registration>> GetRegistrationsByEventAsync(int eventId)
        {
            var allRegistrations = await GetAllRegistrationsAsync();
            return allRegistrations.Where(r => r.EventId == eventId).ToList();
        }

        public async Task<List<Registration>> GetRegistrationsByEmailAsync(string email)
        {
            var allRegistrations = await GetAllRegistrationsAsync();
            return allRegistrations.Where(r => r.Email.Equals(email, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<Registration?> GetRegistrationByIdAsync(string registrationId)
        {
            var allRegistrations = await GetAllRegistrationsAsync();
            return allRegistrations.FirstOrDefault(r => r.Id == registrationId);
        }

        public async Task<bool> AddRegistrationAsync(Registration registration)
        {
            try
            {
                var registrations = await GetAllRegistrationsAsync();
                registrations.Add(registration);
                await _localStorage.SetItemAsync(REGISTRATIONS_KEY, registrations);
                await UpdateEventAttendanceAsync(registration.EventId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateRegistrationAsync(Registration registration)
        {
            try
            {
                var registrations = await GetAllRegistrationsAsync();
                var index = registrations.FindIndex(r => r.Id == registration.Id);
                if (index >= 0)
                {
                    registrations[index] = registration;
                    await _localStorage.SetItemAsync(REGISTRATIONS_KEY, registrations);
                    await UpdateEventAttendanceAsync(registration.EventId);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteRegistrationAsync(string registrationId)
        {
            try
            {
                var registrations = await GetAllRegistrationsAsync();
                var registration = registrations.FirstOrDefault(r => r.Id == registrationId);
                if (registration != null)
                {
                    registrations.Remove(registration);
                    await _localStorage.SetItemAsync(REGISTRATIONS_KEY, registrations);
                    await UpdateEventAttendanceAsync(registration.EventId);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GetEventRegistrationCountAsync(int eventId)
        {
            var registrations = await GetRegistrationsByEventAsync(eventId);
            return registrations.Sum(r => r.NumberOfTickets);
        }

        public async Task<bool> IsUserRegisteredForEventAsync(int eventId, string email)
        {
            var registrations = await GetRegistrationsByEventAsync(eventId);
            return registrations.Any(r => r.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        private async Task UpdateEventAttendanceAsync(int eventId)
        {
            var attendanceData = await _localStorage.GetItemAsync<Dictionary<int, EventAttendance>>(ATTENDANCE_KEY) ?? new();
            var eventRegistrations = await GetRegistrationsByEventAsync(eventId);
            
            attendanceData[eventId] = new EventAttendance
            {
                EventId = eventId,
                Registrations = eventRegistrations,
                LastUpdated = DateTime.Now
            };

            await _localStorage.SetItemAsync(ATTENDANCE_KEY, attendanceData);
        }

        public async Task<EventAttendance?> GetEventAttendanceAsync(int eventId)
        {
            var attendanceData = await _localStorage.GetItemAsync<Dictionary<int, EventAttendance>>(ATTENDANCE_KEY);
            return attendanceData?.GetValueOrDefault(eventId);
        }

        public async Task<Dictionary<int, int>> GetAllEventRegistrationCountsAsync()
        {
            var registrations = await GetAllRegistrationsAsync();
            return registrations
                .GroupBy(r => r.EventId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.NumberOfTickets));
        }
    }
}