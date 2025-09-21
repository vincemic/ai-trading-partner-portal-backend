# EDI Trading Partner Self-Service Portal Backend

## Overview

A secure, enterprise-grade ASP.NET Core backend API for EDI trading partner management, built with Clean Architecture principles. This solution enables external EDI trading partners to self-manage their connectivity credentials, monitor file transfers, and access real-time analytics through a comprehensive REST API.

## 🏗️ Architecture

### Clean Architecture Implementation

The solution follows Clean Architecture patterns with clear separation of concerns:

```
├── TradingPartnerPortal.Domain/          # Core business entities and enums
├── TradingPartnerPortal.Application/     # Business logic and interfaces  
├── TradingPartnerPortal.Infrastructure/  # Data access and external services
└── TradingPartnerPortal.Api/            # REST API controllers and middleware
```

### Technology Stack

- **Framework**: ASP.NET Core (.NET 9)
- **Database**: Entity Framework Core with InMemory provider (pilot)
- **Authentication**: Custom session-based authentication for testing
- **Logging**: Serilog with structured logging
- **API Documentation**: Swagger/OpenAPI 3.1
- **Cryptography**: BouncyCastle for PGP operations
- **Validation**: FluentValidation
- **Real-time**: Server-Sent Events (SSE)

## 🔐 Security Features

### Fake Authentication Mode (Pilot)
- Session-based authentication with 8-hour timeout
- Role-based authorization (PartnerUser, PartnerAdmin, InternalSupport)
- Secure session token management
- Request/response audit trail

### Roles & Permissions
- **PartnerUser**: View dashboard, request password reset, view key metadata
- **PartnerAdmin**: Full credential management, key upload/generation/revocation
- **InternalSupport**: Read-only access for troubleshooting

## 🔑 Core Features

### PGP Key Management
- **Upload Public Keys**: ASCII-armored PGP key validation and storage
- **Generate Key Pairs**: Server-side RSA 4096 key generation
- **Key Lifecycle**: PendingActivation → Active → Revoked/Expired/Superseded
- **Primary Key Management**: Automatic promotion/demotion with overlap windows
- **Fingerprint Validation**: Duplicate detection and uniqueness enforcement

### SFTP Credential Management
- **Password Rotation**: Manual (user-provided) or auto-generated
- **Complexity Requirements**: 16+ chars with upper/lower/digit/special characters
- **Secure Storage**: Argon2id hashing with salt
- **One-time Display**: Generated passwords shown once for security

### Real-time Dashboard
- **File Transfer Metrics**: 24h inbound/outbound counts, success rates
- **Performance Analytics**: Average processing times, error categorization
- **Time Series Data**: Hourly file counts over 48h periods
- **Advanced Metrics**: Large file detection, connection health, throughput analysis

### Audit & Compliance
- **Comprehensive Logging**: All credential operations tracked
- **Audit Trail**: Actor, timestamp, operation type, success/failure
- **Metadata Capture**: Detailed context for compliance requirements
- **Query Interface**: Searchable audit history with filtering

## 📡 Real-time Features

### Server-Sent Events (SSE)
- **Live Updates**: File status changes, key promotions, metric updates
- **Reconnection Support**: Automatic reconnection with event replay
- **Buffering**: 500-event rolling buffer per partner
- **Heartbeat**: 15-second keepalive for connection monitoring

## 🗄️ Data Model

### Core Entities

#### Partner
```csharp
- PartnerId (Guid)
- Name (string)
- Status (Active/Suspended)
- CreatedAt (DateTime)
```

#### PgpKey
```csharp
- KeyId (Guid)
- PartnerId (Guid)
- PublicKeyArmored (string)
- Fingerprint (string)
- Algorithm (string)
- KeySize (int)
- ValidFrom/ValidTo (DateTime)
- Status (PendingActivation/Active/Revoked/Expired/Superseded)
- IsPrimary (bool)
```

#### FileTransferEvent
```csharp
- FileId (Guid)
- Direction (Inbound/Outbound)
- DocType (string)
- SizeBytes (long)
- Status (Pending/Processing/Success/Failed)
- ProcessingLatencyMs (computed)
```

## 🚀 API Endpoints

### Authentication
- `POST /api/fake-login` - Create test session
- `GET /api/health` - Health check
- `GET /api/version` - Build information

### Key Management
- `GET /api/keys` - List partner keys
- `POST /api/keys/upload` - Upload public key
- `POST /api/keys/generate` - Generate key pair
- `POST /api/keys/{id}/revoke` - Revoke key
- `POST /api/keys/{id}/promote` - Promote to primary

### SFTP Credentials
- `GET /api/sftp/credential` - Get metadata
- `POST /api/sftp/credential/rotate` - Rotate password

### Dashboard Analytics
- `GET /api/dashboard/summary` - Key metrics
- `GET /api/dashboard/timeseries` - Historical data
- `GET /api/dashboard/errors/top` - Error analysis

### File Monitoring
- `GET /api/files` - Search file events
- `GET /api/files/{id}` - File details

### Audit Trail
- `GET /api/audit` - Audit event search (InternalSupport only)

### Real-time Events
- `GET /api/events/stream` - SSE endpoint

## 🔧 Configuration

### Development Settings
```json
{
  "FakeAuth": {
    "Enabled": true,
    "SessionTimeoutHours": 8
  },
  "Mock": {
    "PersistToFile": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Session Management
- **Session Timeout**: 8 hours idle timeout
- **Cleanup**: Automatic expired session removal
- **Token Format**: GUID-based opaque tokens
- **Header**: `X-Session-Token` for API requests

## 📊 Response Formats

### Standard Response Envelope
```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "Descriptive error message",
    "traceId": "correlation-id"
  }
}
```

### Pagination
```json
{
  "items": [...],
  "page": 1,
  "pageSize": 25,
  "totalItems": 150,
  "totalPages": 6
}
```

### SSE Events
```
event: file.statusChanged
id: 123
data: {"fileId": "...", "oldStatus": "Processing", "newStatus": "Success"}
```

## 🚀 Getting Started

### Prerequisites
- .NET 9 SDK
- Visual Studio 2025 or VS Code with C# extension

### Development with VS Code (Recommended)

For the best development experience, use Visual Studio Code with the configured tasks and launch configurations.

**📖 See [VS Code Development Guide](.vscode/DEVELOPMENT.md) for detailed instructions**

#### Quick Start
1. **Clone the repository**
   ```powershell
   git clone <repository-url>
   cd ai-trading-partner-portal-backend
   ```

2. **Open in VS Code**
   ```powershell
   code .
   ```

3. **Run setup tasks**
   - Press `Ctrl+Shift+P` → "Tasks: Run Task" → "Restore Packages"
   - Press `Ctrl+Shift+P` → "Tasks: Run Task" → "Build Solution"

4. **Start the API**
   - Press `Ctrl+Shift+P` → "Tasks: Run Task" → "Run Trading Partner Portal API"
   - Or press `F5` to start with debugging

5. **Validate the setup**
   - Press `Ctrl+Shift+P` → "Tasks: Run Task" → "Full Validation Suite"

#### API Testing
- Use the included REST client file: `.vscode/api-tests.http`
- Or access Swagger UI at: `http://localhost:5096/swagger`

### Manual Setup

1. **Clone and Build**
   ```powershell
   git clone <repository-url>
   cd ai-trading-partner-portal-backend
   dotnet build
   ```

2. **Run the API**
   ```powershell
   cd TradingPartnerPortal.Api
   dotnet run
   ```

3. **Access Swagger UI**
   ```
   http://localhost:5096/swagger
   ```

### Authentication Flow

1. **Create Session**
   ```bash
   POST /api/fake-login
   {
     "userId": "john.doe@partner.com",
     "partnerId": "guid-here",
     "role": "PartnerAdmin"
   }
   ```

2. **Use Session Token**
   ```bash
   GET /api/keys
   X-Session-Token: session-token-here
   ```

## 🧪 Testing

### Sample Test Flow

1. **Create Admin Session**
2. **Generate PGP Key Pair**
3. **Rotate SFTP Password**
4. **View Dashboard Metrics**
5. **Monitor Real-time Events**

### Postman Collection

A comprehensive Postman collection is available with pre-configured requests for all endpoints, including authentication setup and example payloads.

## 🔮 Roadmap

### Phase 2 Enhancements
- **Microsoft Entra External ID** integration
- **Azure Key Vault** for secrets management
- **Private endpoints** and network security
- **Persistent storage** with Azure SQL
- **Horizontal scaling** capabilities
- **Advanced monitoring** with Application Insights

### Security Hardening
- **Customer-managed encryption keys**
- **Private endpoint networking**
- **Advanced audit controls**
- **HIPAA/HITECH compliance** features

## 📈 Performance & Scaling

### Current Capacity
- **In-Memory Storage**: Suitable for pilot phase
- **Concurrent Sessions**: No artificial limits
- **SSE Connections**: Max 3 per partner
- **Response Times**: <1.5s p95 for dashboard aggregations

### Production Considerations
- **Database Migration**: EF Core migrations ready
- **Caching Strategy**: Redis integration points identified
- **Load Balancing**: Stateless design supports scale-out
- **Monitoring**: Structured logging and metrics collection

## 🏢 Enterprise Features

### Audit & Compliance
- **Complete audit trail** for all operations
- **Role-based access controls**
- **Data retention policies** ready
- **Compliance reporting** capabilities

### Operations
- **Health check endpoints**
- **Structured logging** with correlation IDs
- **Error categorization** and alerting hooks
- **Performance metrics** collection

### Integration Ready
- **OpenAPI 3.1** specification
- **RESTful design** principles
- **Standard HTTP status codes**
- **JSON response format** consistency

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

**Built with Clean Architecture principles for maintainability, testability, and scalability.**