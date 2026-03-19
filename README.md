# SupportTicketAPI

ASP.NET Web API backend for the Customer Support Ticket System. Built with .NET 8, Dapper, MySQL, and JWT authentication.

---

## Overview

This API serves as the backend for the Customer Support Ticket System desktop application. It handles all business logic, authentication, and database operations. The desktop application communicates exclusively through this API using JSON over HTTP.

---

## Tech Stack

| Technology | Purpose |
|---|---|
| ASP.NET Web API (.NET 8) | REST API framework |
| Dapper | Lightweight ORM for MySQL queries |
| MySQL 8.x | Database |
| BCrypt.Net-Next | Password hashing |
| JWT Bearer Tokens | Authentication and authorization |
| Swagger / OpenAPI | API documentation |

---

## Project Structure

```
SupportTicketAPI/
├── Controllers/
│   ├── AuthController.cs       ← Login + password hash utility
│   └── TicketsController.cs    ← All ticket CRUD endpoints
├── Data/
│   └── Repositories.cs         ← Dapper DB access (5 repositories)
├── Models/
│   └── Models.cs               ← Domain entities + request/response DTOs
├── Services/
│   └── JwtService.cs           ← JWT token generation
├── Program.cs                  ← DI registration, middleware, JWT config
└── appsettings.json            ← Connection string and JWT settings
```

---

## Prerequisites

- .NET 8 SDK
- MySQL 8.x running on localhost
- MySQL database created using `Database/schema.sql`

---

## Configuration

Open `appsettings.json` and set your MySQL password:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=SupportTicketDB;Uid=root;Pwd=YOUR_PASSWORD;"
  },
  "JwtSettings": {
    "SecretKey": "SupportTicket_SuperSecretKey_2024_ChangeInProduction!",
    "Issuer": "SupportTicketAPI",
    "Audience": "SupportTicketSystem",
    "ExpiryHours": 8
  },
  "Urls": "http://localhost:5000"
}
```

---

## Running the API

```bash
cd API
dotnet restore
dotnet run
```

API runs at: `http://localhost:5000`

Swagger UI: `http://localhost:5000/swagger`

---

## API Endpoints

### Auth

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/login` | None | Login and receive JWT token |
| GET | `/api/auth/hash/{password}` | None | Generate BCrypt hash for a password | This is optional only for checking password

**Login request body:**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Login response:**
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "token": "eyJhbGci...",
    "userId": 1,
    "username": "admin",
    "fullName": "System Administrator",
    "role": "Admin"
  }
}
```

---

### Tickets

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/tickets` | Required | Get tickets — all for Admin, own for User |
| POST | `/api/tickets` | User only | Create a new ticket |
| GET | `/api/tickets/{id}` | Required | Get ticket details with history and comments |
| PUT | `/api/tickets/{id}/assign` | Admin only | Assign ticket to an admin user |
| PUT | `/api/tickets/{id}/status` | Admin only | Update ticket status |
| POST | `/api/tickets/{id}/comments` | Required | Add a comment to a ticket |
| GET | `/api/tickets/admins` | Admin only | Get list of admin users for assignment |

---

## Using Authenticated Endpoints

After login, copy the token and click **Authorize** in Swagger:

```
Bearer eyJhbGci...your-token-here
```

Or set the Authorization header in HTTP requests:
```
Authorization: Bearer eyJhbGci...
```

---

## Request / Response Examples

### Create Ticket
```json
POST /api/tickets
{
  "subject": "Login page not loading",
  "description": "The login page shows a blank screen on Firefox 126+",
  "priority": "High"
}
```

### Assign Ticket
```json
PUT /api/tickets/1/assign
{
  "assignedToUserId": 2
}
```

### Update Status
```json
PUT /api/tickets/1/status
{
  "newStatus": "InProgress",
  "remarks": "Investigating the issue now"
}
```

### Add Comment
```json
POST /api/tickets/1/comments
{
  "commentText": "I can reproduce this issue on Firefox 126.",
  "isInternal": false
}
```

---

## Status Flow

```
Open  →  InProgress  →  Closed
```

The API enforces this flow strictly. Any attempt to skip a status or go backwards will return a 400 Bad Request error.

---

## Error Responses

All endpoints return a consistent response format:

```json
{
  "success": false,
  "message": "Error description here",
  "data": null
}
```

Common HTTP status codes used:

| Code | Meaning |
|---|---|
| 200 | Success |
| 201 | Created (new ticket) |
| 400 | Bad request / validation error |
| 401 | Unauthorized (invalid or missing token) |
| 403 | Forbidden (insufficient role) |
| 404 | Ticket not found |

---

## Test Credentials

| Username | Password | Role |
|---|---|---|
| `admin` | `admin123` | Admin |
| `jsmith` | `user123` | Admin |
| `alice` | `user123` | User |
| `bob` | `user123` | User |

---

## NuGet Packages Used

| Package | Version | Purpose |
|---|---|---|
| BCrypt.Net-Next | 4.0.3 | Password hashing |
| Dapper | 2.1.35 | Database access |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.0 | JWT authentication |
| MySql.Data | 8.3.0 | MySQL driver |
| Swashbuckle.AspNetCore | 6.5.0 | Swagger UI |
| System.IdentityModel.Tokens.Jwt | 7.3.1 | JWT token handling |

---

## Notes

- The `/api/auth/hash/{password}` endpoint is a development utility for generating BCrypt hashes to seed the database. Remove it before production deployment.
- All timestamps are stored in UTC using `UTC_TIMESTAMP()` and converted to local time by the client.
- JWT tokens expire after 8 hours by default. This can be changed in `appsettings.json`.