# EventEase API Documentation

**Version**: 2.0
**Base URL**: `https://api.eventease.com`
**Last Updated**: November 12, 2025

---

## Table of Contents

1. [Authentication](#authentication)
2. [Core APIs](#core-apis)
3. [AI-Powered APIs](#ai-powered-apis)
4. [Real-Time APIs](#real-time-apis)
5. [Mobile APIs](#mobile-apis)
6. [Analytics & Reporting](#analytics--reporting)
7. [GraphQL API](#graphql-api)
8. [Rate Limiting](#rate-limiting)
9. [Error Handling](#error-handling)

---

## Authentication

### JWT Bearer Token Authentication

All API requests (except `/api/auth/*`) require a JWT bearer token in the Authorization header.

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Authentication Endpoints

#### Register Tenant
```http
POST /api/auth/register
Content-Type: application/json

{
  "tenantName": "Acme Events",
  "email": "owner@acme.com",
  "password": "SecurePass123!",
  "fullName": "John Doe"
}
```

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "owner@acme.com",
  "password": "SecurePass123!"
}
```

**Response**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "expiresIn": 3600
}
```

#### Refresh Token
```http
POST /api/auth/refresh
Content-Type: application/json

{
  "token": "expired_token",
  "refreshToken": "refresh_token_here"
}
```

---

## Core APIs

### Events API

#### List Events
```http
GET /api/events?page=1&pageSize=20&status=Published
```

**Query Parameters**:
- `page` (int): Page number (default: 1)
- `pageSize` (int): Items per page (default: 20, max: 100)
- `status` (string): Event status filter
- `category` (string): Event category filter
- `startDate` (date): Filter events starting after date
- `endDate` (date): Filter events ending before date

**Response**:
```json
{
  "data": [
    {
      "id": "uuid",
      "title": "Tech Summit 2025",
      "description": "Annual technology conference",
      "startDate": "2025-06-15T09:00:00Z",
      "endDate": "2025-06-17T18:00:00Z",
      "venue": "San Francisco Convention Center",
      "location": "San Francisco, CA",
      "capacity": 1000,
      "registrationCount": 756,
      "status": "Published",
      "category": "Technology",
      "isVirtual": false,
      "price": 299.00,
      "currency": "USD"
    }
  ],
  "totalCount": 45,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

#### Create Event
```http
POST /api/events
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "AI Workshop 2025",
  "description": "Hands-on AI and ML workshop",
  "startDate": "2025-08-20T10:00:00Z",
  "endDate": "2025-08-20T17:00:00Z",
  "venue": "Tech Hub",
  "location": "New York, NY",
  "capacity": 50,
  "category": "Workshop",
  "isVirtual": false,
  "registrationOpenDate": "2025-07-01T00:00:00Z",
  "registrationCloseDate": "2025-08-15T23:59:59Z",
  "price": 149.00,
  "currency": "USD"
}
```

#### Get Event Details
```http
GET /api/events/{eventId}
```

#### Update Event
```http
PUT /api/events/{eventId}
Authorization: Bearer {token}
```

#### Delete Event
```http
DELETE /api/events/{eventId}
Authorization: Bearer {token}
```

#### Publish Event
```http
POST /api/events/{eventId}/publish
Authorization: Bearer {token}
```

---

### Registrations API

#### Register for Event
```http
POST /api/registrations
Authorization: Bearer {token}
Content-Type: application/json

{
  "eventId": "uuid",
  "numberOfGuests": 2,
  "dietaryRestrictions": "Vegetarian",
  "specialRequirements": "Wheelchair accessible seating"
}
```

#### Check-In Attendee
```http
POST /api/registrations/{registrationId}/checkin
Authorization: Bearer {token}
```

#### List User Registrations
```http
GET /api/registrations/my-registrations
Authorization: Bearer {token}
```

---

### Guests API

#### Add Guest to Event
```http
POST /api/guests
Authorization: Bearer {token}
Content-Type: application/json

{
  "eventId": "uuid",
  "name": "Jane Smith",
  "email": "jane@example.com",
  "vipStatus": false,
  "dietaryRestrictions": "None",
  "specialRequirements": ""
}
```

#### Send Invitation
```http
POST /api/guests/{guestId}/invite
Authorization: Bearer {token}
```

---

### Credits & Packages API

#### List Credit Packages
```http
GET /api/credits/packages
```

**Response**:
```json
[
  {
    "id": "uuid",
    "name": "Starter Pack",
    "baseCredits": 10000,
    "bonusCredits": 1000,
    "price": 49.99,
    "currency": "USD",
    "validityDays": 365,
    "features": ["AI Event Planning", "Email Automation"]
  }
]
```

#### Get Credit Balance
```http
GET /api/credits/balance
Authorization: Bearer {token}
```

#### Credit Transaction History
```http
GET /api/credits/transactions?page=1&pageSize=20
Authorization: Bearer {token}
```

---

### Payments API

#### Initiate Payment
```http
POST /api/payments/initiate
Authorization: Bearer {token}
Content-Type: application/json

{
  "packageId": "uuid",
  "paymentMethodId": "pm_card_visa",
  "savePaymentMethod": true
}
```

**Response**:
```json
{
  "success": true,
  "paymentIntentId": "pi_abc123",
  "clientSecret": "pi_abc123_secret_xyz",
  "amount": 49.99,
  "currency": "USD",
  "message": "Use client secret to complete payment"
}
```

#### Confirm Payment
```http
POST /api/payments/confirm
Authorization: Bearer {token}
Content-Type: application/json

{
  "paymentIntentId": "pi_abc123"
}
```

#### Get Invoice
```http
GET /api/payments/invoice/{purchaseId}
Authorization: Bearer {token}
```

#### Download Invoice PDF
```http
GET /api/payments/invoice/{purchaseId}/pdf
Authorization: Bearer {token}
```

---

## AI-Powered APIs

### Event Assistant

#### Create Event from Natural Language
```http
POST /api/AIAssistant/event-assistant/create-from-language
Authorization: Bearer {token}
Content-Type: application/json

{
  "description": "I want to host a tech conference in San Francisco next March with about 500 people focusing on AI and machine learning. Budget is around $100k."
}
```

**Response**:
```json
{
  "success": true,
  "eventSuggestion": {
    "title": "AI & ML Tech Conference 2025",
    "description": "...",
    "startDate": "2025-03-15T09:00:00Z",
    "endDate": "2025-03-17T18:00:00Z",
    "capacity": 500,
    "estimatedBudget": 95000,
    "recommendations": ["..."]
  },
  "creditsUsed": 50
}
```

#### Get Venue Recommendations
```http
POST /api/AIAssistant/event-assistant/recommend-venues
Authorization: Bearer {token}
Content-Type: application/json

{
  "location": "San Francisco, CA",
  "capacity": 500,
  "eventType": "Conference",
  "budget": 100000,
  "startDate": "2025-03-15"
}
```

#### Suggest Speakers
```http
POST /api/AIAssistant/event-assistant/suggest-speakers
Authorization: Bearer {token}
Content-Type: application/json

{
  "eventTopic": "Artificial Intelligence",
  "numberOfSpeakers": 5,
  "speakerType": "Industry Experts"
}
```

#### Optimize Event Agenda
```http
POST /api/AIAssistant/event-assistant/optimize-agenda
Authorization: Bearer {token}
Content-Type: application/json

{
  "eventId": "uuid",
  "sessions": [
    {
      "title": "Keynote: Future of AI",
      "duration": 60,
      "speakerCount": 1
    }
  ],
  "constraints": ["No back-to-back technical sessions"]
}
```

---

### AI Matchmaking & Networking

#### Get Networking Suggestions
```http
GET /api/AIAssistant/matchmaking/networking-suggestions/{eventId}?maxSuggestions=10
Authorization: Bearer {token}
```

**Response**:
```json
{
  "suggestions": [
    {
      "attendee": {
        "id": "uuid",
        "name": "John Doe",
        "title": "Software Engineer",
        "company": "Tech Corp"
      },
      "similarityScore": 0.92,
      "matchReason": "Both interested in Machine Learning and Python",
      "conversationStarters": [
        "Ask about their experience with TensorFlow",
        "Discuss the latest ML frameworks"
      ]
    }
  ],
  "creditsUsed": 20
}
```

#### Generate Conversation Starters
```http
POST /api/AIAssistant/matchmaking/conversation-starters
Authorization: Bearer {token}
Content-Type: application/json

{
  "user1Id": "uuid",
  "user2Id": "uuid",
  "context": "Conference networking"
}
```

---

### Predictive Analytics

#### Forecast Event Attendance
```http
GET /api/AIAssistant/analytics/forecast-attendance/{eventId}
Authorization: Bearer {token}
```

**Response**:
```json
{
  "eventId": "uuid",
  "predictedAttendance": 456,
  "confidenceInterval": {
    "lower": 420,
    "upper": 492
  },
  "confidence": 0.85,
  "factors": [
    "Historical attendance for similar events",
    "Current registration pace",
    "Day of week and seasonality"
  ],
  "creditsUsed": 30
}
```

#### Predict No-Shows
```http
GET /api/AIAssistant/analytics/predict-no-shows/{eventId}
Authorization: Bearer {token}
```

#### Optimize Event Revenue
```http
POST /api/AIAssistant/analytics/optimize-revenue/{eventId}
Authorization: Bearer {token}
```

#### Predict Tenant Churn
```http
GET /api/AIAssistant/analytics/predict-churn
Authorization: Bearer {token}
```

---

### Content Generation

#### Generate Event Description
```http
POST /api/AIAssistant/content/generate-description
Authorization: Bearer {token}
Content-Type: application/json

{
  "eventTitle": "AI Summit 2025",
  "eventType": "Conference",
  "targetAudience": "Software Engineers, Data Scientists",
  "keyTopics": ["Machine Learning", "Deep Learning", "AI Ethics"],
  "tone": "Professional",
  "length": "medium"
}
```

#### Generate Email Content
```http
POST /api/AIAssistant/content/generate-email
Authorization: Bearer {token}
Content-Type: application/json

{
  "emailType": "invitation",
  "eventId": "uuid",
  "recipientName": "John Doe",
  "tone": "professional"
}
```

#### Generate Social Media Posts
```http
POST /api/AIAssistant/content/generate-social-media
Authorization: Bearer {token}
Content-Type: application/json

{
  "eventId": "uuid",
  "platform": "LinkedIn",
  "postType": "announcement",
  "tone": "professional"
}
```

---

## Real-Time APIs

### SignalR WebSocket Connections

#### Event Hub
**Endpoint**: `wss://api.eventease.com/hubs/events`

**Client Methods**:
```typescript
// Receive event updates
connection.on("EventUpdated", (eventId, eventData) => {
  console.log("Event updated:", eventId);
});

// Receive chat messages
connection.on("ReceiveChatMessage", (userId, userName, message, timestamp) => {
  displayMessage(userName, message, timestamp);
});

// Receive attendee count updates
connection.on("AttendeeCountUpdated", (eventId, count) => {
  updateCount(count);
});

// Receive poll results
connection.on("PollResultUpdated", (pollId, results) => {
  updatePollChart(results);
});
```

**Server Methods**:
```typescript
// Join an event
await connection.invoke("JoinEventAsync", eventId);

// Send chat message
await connection.invoke("SendChatMessageAsync", eventId, "Hello!");

// Submit poll vote
await connection.invoke("SubmitPollVoteAsync", pollId, optionId);
```

#### Notification Hub
**Endpoint**: `wss://api.eventease.com/hubs/notifications`

**Client Methods**:
```typescript
// Receive notifications
connection.on("ReceiveNotification", (notification) => {
  showNotification(notification);
});

// Credit balance update
connection.on("CreditBalanceUpdated", (balance) => {
  updateBalance(balance);
});
```

#### Analytics Hub
**Endpoint**: `wss://api.eventease.com/hubs/analytics`
**Authorization**: Admin roles only

**Client Methods**:
```typescript
// Receive dashboard updates
connection.on("DashboardMetricsUpdated", (metrics) => {
  updateDashboard(metrics);
});

// Revenue updates
connection.on("RevenueUpdated", (totalRevenue, monthlyRevenue) => {
  updateRevenueCharts(totalRevenue, monthlyRevenue);
});
```

---

## Mobile APIs

### Mobile-Optimized REST Endpoints

#### Get Mobile Events
```http
GET /api/mobile/events?upcomingOnly=true&page=1&pageSize=20
Authorization: Bearer {token}
```

#### QR Code Generation
```http
POST /api/mobile/qrcode/generate
Authorization: Bearer {token}
Content-Type: application/json

{
  "type": "event",
  "eventId": "uuid",
  "format": "png",
  "size": 300
}
```

**Response**:
```json
{
  "success": true,
  "qrCodeData": "EVENTEASE:EVENT:uuid",
  "qrCodeImage": "data:image/png;base64,iVBORw0KG...",
  "format": "png"
}
```

#### QR Code Check-In
```http
POST /api/mobile/qrcode/checkin
Authorization: Bearer {token}
Content-Type: application/json

{
  "qrCodeData": "EVENTEASE:REGISTRATION:uuid"
}
```

#### Geolocation Check-In
```http
POST /api/mobile/checkin/geolocation
Authorization: Bearer {token}
Content-Type: application/json

{
  "registrationId": "uuid",
  "latitude": 37.7749,
  "longitude": -122.4194
}
```

**Response**:
```json
{
  "success": true,
  "checkedInAt": "2025-11-12T10:30:00Z",
  "withinGeofence": true,
  "distanceFromVenue": 25.5,
  "message": "Successfully checked in!"
}
```

#### Register Device for Push Notifications
```http
POST /api/devices/register
Authorization: Bearer {token}
Content-Type: application/json

{
  "deviceToken": "fcm_token_here",
  "platform": "iOS",
  "deviceName": "John's iPhone",
  "osVersion": "17.0",
  "appVersion": "1.0.0"
}
```

#### Offline Sync
```http
POST /api/mobile/sync
Authorization: Bearer {token}
Content-Type: application/json

{
  "lastSyncTimestamp": "2025-11-12T10:00:00Z",
  "pendingActions": [
    {
      "id": "local_action_1",
      "type": "checkin",
      "data": {"registrationId": "uuid"},
      "timestamp": "2025-11-12T10:15:00Z"
    }
  ]
}
```

---

## Analytics & Reporting

### Analytics Endpoints

#### Dashboard Metrics
```http
GET /api/analytics/dashboard
Authorization: Bearer {token}
```

**Response**:
```json
{
  "totalEvents": 142,
  "totalAttendees": 15234,
  "totalRevenue": 458900,
  "averageAttendance": 107.3,
  "upcomingEvents": 23,
  "activeRegistrations": 1456,
  "creditBalance": 25000,
  "creditsUsedThisMonth": 5420,
  "periodComparison": {
    "eventsChange": 15.2,
    "attendeesChange": 22.5,
    "revenueChange": 18.7
  }
}
```

#### Event Analytics
```http
GET /api/analytics/events/{eventId}
Authorization: Bearer {token}
```

#### Tenant Analytics
```http
GET /api/analytics/tenant?startDate=2025-01-01&endDate=2025-12-31
Authorization: Bearer {token}
```

#### Tenant Health Score
```http
GET /api/analytics/health-score
Authorization: Bearer {token}
```

**Response**:
```json
{
  "score": 87,
  "category": "Excellent",
  "factors": {
    "eventActivity": 90,
    "creditUsage": 85,
    "revenue": 88,
    "engagement": 82,
    "growth": 95
  },
  "recommendations": ["Continue growing event portfolio", "..."],
  "riskIndicators": []
}
```

---

### Report Generation

#### Generate PDF Report
```http
POST /api/reports/pdf
Authorization: Bearer {token}
Content-Type: application/json

{
  "reportType": "TenantSummary",
  "startDate": "2025-01-01",
  "endDate": "2025-12-31",
  "includeCharts": true
}
```

#### Generate Excel Report
```http
POST /api/reports/excel
Authorization: Bearer {token}
```

#### Export Event Registrations
```http
GET /api/reports/export/events/{eventId}/registrations?format=csv
Authorization: Bearer {token}
```

---

## GraphQL API

### Endpoint
```
POST https://api.eventease.com/graphql
WebSocket wss://api.eventease.com/graphql
```

### Example Queries

#### Get Events
```graphql
query GetEvents {
  events(first: 10, where: { status: { eq: PUBLISHED } }) {
    nodes {
      id
      title
      startDate
      venue
      registrationCount
      availableSpots
    }
  }
}
```

#### Create Registration
```graphql
mutation RegisterForEvent {
  createRegistration(
    input: {
      eventId: "uuid"
      numberOfGuests: 2
      dietaryRestrictions: "Vegetarian"
    }
  ) {
    id
    status
    event {
      title
    }
  }
}
```

#### Subscribe to Event Updates
```graphql
subscription OnEventUpdate {
  onEventUpdate(eventId: "uuid") {
    eventId
    updateType
    data
    timestamp
  }
}
```

---

## Rate Limiting

- **Standard Tier**: 1,000 requests/hour
- **Professional Tier**: 5,000 requests/hour
- **Enterprise Tier**: Unlimited

**Rate Limit Headers**:
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 847
X-RateLimit-Reset: 1699891200
```

---

## Error Handling

### Standard Error Response
```json
{
  "type": "https://api.eventease.com/errors/validation-error",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred",
  "errors": {
    "title": ["The title field is required"],
    "capacity": ["Capacity must be greater than 0"]
  },
  "traceId": "00-abc123-def456-00"
}
```

### HTTP Status Codes

- **200 OK**: Successful request
- **201 Created**: Resource created successfully
- **204 No Content**: Successful deletion
- **400 Bad Request**: Invalid request data
- **401 Unauthorized**: Missing or invalid authentication
- **403 Forbidden**: Insufficient permissions
- **404 Not Found**: Resource not found
- **409 Conflict**: Resource conflict (e.g., duplicate)
- **429 Too Many Requests**: Rate limit exceeded
- **500 Internal Server Error**: Server error

---

## SDKs & Client Libraries

### JavaScript/TypeScript
```bash
npm install @eventease/sdk
```

```typescript
import EventEaseSDK from '@eventease/sdk';

const client = new EventEaseSDK({
  apiKey: 'your_api_key',
  environment: 'production'
});

const events = await client.events.list();
```

### Python
```bash
pip install eventease-sdk
```

```python
from eventease import EventEaseClient

client = EventEaseClient(api_key='your_api_key')
events = client.events.list()
```

### .NET
```bash
dotnet add package EventEase.SDK
```

```csharp
var client = new EventEaseClient("your_api_key");
var events = await client.Events.ListAsync();
```

---

## Support

- **Documentation**: https://docs.eventease.com
- **API Status**: https://status.eventease.com
- **Support Email**: api-support@eventease.com
- **Developer Community**: https://community.eventease.com

---

**EventEase API v2.0** - Built with ASP.NET Core 9.0 🚀
