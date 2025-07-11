# EventEase - Blazor Server Application

EventEase is a modern event management application built with Blazor Server and LocalStorage for data persistence. It provides a complete solution for browsing, registering, and tracking corporate and social events.

## Features

### Core Features
- **Event Browsing**: Browse all available events with detailed information
- **Event Registration**: Complete registration form with comprehensive validation
- **LocalStorage Persistence**: All data stored in browser's localStorage
- **User Session Management**: Remembers user information for future registrations
- **Attendance Tracking**: Track registrations and capacity for each event
- **Registration History**: View all your past registrations

### Advanced Features
- **Real-time Notifications**: Toast notifications for user feedback
- **Loading States**: Professional loading indicators throughout the app
- **Capacity Management**: Real-time seat availability tracking
- **Form Validation**: Comprehensive client-side and server-side validation
- **Responsive Design**: Works perfectly on desktop and mobile devices
- **Performance Optimized**: Fast loading and efficient data management

## Technical Stack

- **Blazor Server** (.NET 10.0)
- **Blazored.LocalStorage** (v4.5.0) - For browser storage
- **Bootstrap 5** - For responsive UI
- **Bootstrap Icons** - For modern icons
- **SignalR** - Built-in for real-time updates

## Project Structure

```
EventEaseApp/
├── Components/
│   ├── EventCard.razor - Reusable event card component
│   ├── EventList.razor - Event listing component
│   ├── NotificationAlert.razor - Toast notification component
│   └── Pages/
│       ├── Home.razor - Landing page
│       ├── Events.razor - All events page
│       ├── EventDetails.razor - Event details with capacity
│       ├── Register.razor - Registration form with validation
│       ├── MyRegistrations.razor - User registration history
│       └── EventAttendance.razor - Attendance tracking
├── Models/
│   ├── Event.cs - Event data model
│   ├── Registration.cs - Registration with validation attributes
│   ├── UserSession.cs - User session data
│   └── EventAttendance.cs - Attendance tracking model
├── Services/
│   ├── EventService.cs - Mock event data service
│   ├── RegistrationService.cs - Registration management with LocalStorage
│   ├── UserSessionService.cs - User session management
│   └── NotificationService.cs - Toast notification service
└── wwwroot/
    └── app.css - Custom styles with animations
```

## LocalStorage Schema

### Registrations
```json
{
  "eventease_registrations": [
    {
      "id": "guid",
      "eventId": 1,
      "eventName": "Event Name",
      "firstName": "John",
      "lastName": "Doe",
      "email": "john@example.com",
      "phone": "123-456-7890",
      "company": "Company Inc",
      "numberOfTickets": 2,
      "specialRequests": "Dietary requirements",
      "registrationDate": "2024-01-01T00:00:00",
      "totalAmount": 599.98,
      "status": "Confirmed"
    }
  ]
}
```

### User Session
```json
{
  "eventease_user_session": {
    "sessionId": "guid",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "phone": "123-456-7890",
    "company": "Company Inc",
    "createdAt": "2024-01-01T00:00:00",
    "lastAccessedAt": "2024-01-01T00:00:00",
    "preferences": {}
  }
}
```

## Getting Started

### Prerequisites
- .NET 10.0 SDK or later
- A modern web browser with LocalStorage support

### Running the Application

1. Clone the repository
2. Navigate to the project directory
3. Run the application:
   ```bash
   dotnet run
   ```
4. Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`

## Key Features Implementation

### 1. Enhanced Registration Form
- Uses Blazor's `EditForm` with `DataAnnotationsValidator`
- Custom validation attributes for all fields
- Real-time validation feedback
- Loading states during submission
- Prevents duplicate registrations

### 2. LocalStorage Integration
- All data persisted in browser's localStorage
- Automatic serialization/deserialization
- Error handling for storage failures
- Data persistence across browser sessions

### 3. User Session Management
- Automatically fills forms with saved user data
- 30-day session validity
- Updates on each successful registration
- Clear session option available

### 4. Attendance Tracking
- Real-time capacity tracking
- Visual progress bars
- Export functionality (CSV)
- Sortable attendee lists

### 5. Notification System
- Toast notifications for all actions
- Auto-dismiss after 5 seconds
- Different styles for success/error/warning/info
- Non-intrusive positioning

## Performance Optimizations

1. **Component Lifecycle**: Proper use of `OnInitializedAsync`
2. **Loading States**: Prevents UI freezing during data operations
3. **Efficient Queries**: LINQ optimizations for data filtering
4. **CSS Animations**: Hardware-accelerated transitions
5. **Responsive Images**: Placeholder images with proper sizing

## Security Considerations

1. **Input Validation**: Comprehensive validation on all user inputs
2. **XSS Prevention**: Blazor's built-in protection
3. **Data Sanitization**: All inputs sanitized before storage
4. **No Sensitive Data**: No passwords or payment info stored

## Browser Compatibility

- Chrome (recommended)
- Firefox
- Safari
- Edge
- Any modern browser with LocalStorage support

## Production Deployment

1. **Build for Production**:
   ```bash
   dotnet publish -c Release
   ```

2. **Configure HTTPS**: Ensure HTTPS is properly configured

3. **Set Environment**: Update `appsettings.json` for production

4. **Deploy**: Deploy to your preferred hosting service (Azure, AWS, etc.)

## Future Enhancements

- Email notifications
- Payment integration
- QR code tickets
- Calendar integration
- Multi-language support
- Dark mode theme

## Troubleshooting

### LocalStorage Issues
- Clear browser cache and localStorage
- Check browser privacy settings
- Ensure cookies are enabled

### Performance Issues
- Check browser console for errors
- Verify LocalStorage size limits
- Clear old registration data

## License

This is a demo application for educational purposes.