# Findora — Intelligent Lost & Found Network

Findora allows users and organizations to report lost/found belongings and
intelligently match lost reports with found reports using attributes such as
descriptions, images, location, time, category, brand, color, and physical
characteristics.

## Architecture

This backend starts as a **modular monolith** built with ASP.NET Core Web API
(C#). It is organized into clear layers within a single deployable project so
the codebase stays simple now, but can be split into services later without a
rewrite.

```
Findora/
├── Findora.sln
├── Findora.API/
│   ├── Controllers/      # API endpoints
│   ├── Data/              # DbContext, EF Core configuration (later)
│   ├── Models/             # Domain/entity models (later)
│   ├── DTOs/               # Request/response contracts (later)
│   ├── Services/           # Business logic (later)
│   ├── Repositories/       # Data access abstractions (later)
│   ├── Middleware/         # Custom middleware (later)
│   ├── Configuration/      # Strongly-typed options/config binding (later)
│   ├── Mappings/           # Object-to-object mapping profiles (later)
│   ├── Properties/         # launchSettings.json
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Findora.API.csproj
└── README.md
```

## Technology Stack

- ASP.NET Core Web API (.NET 8 LTS)
- C#
- Entity Framework Core (planned)
- PostgreSQL (planned)
- ASP.NET Core Identity / JWT authentication (planned)
- REST APIs
- Swagger / OpenAPI
- Docker (planned)
- AWS deployment (planned)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Running locally

```bash
cd Findora.API
dotnet run
```

The API will start (by default on `https://localhost:7251` and
`http://localhost:5222`, per `Properties/launchSettings.json`) and open the
Swagger UI at `/swagger`.

To run against a specific port manually:

```bash
dotnet run --urls "http://localhost:5299"
```

## Build

```bash
dotnet build
```

## Status

Foundation setup only. No authentication, database models, business logic,
AI matching, or AWS integration has been implemented yet.
