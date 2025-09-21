# Trading Partner Portal Backend API - Frontend Developer Guide

## Table of Contents

1. [Overview](#overview)
2. [API Endpoints](#api-endpoints)
3. [Authentication & Authorization](#authentication--authorization)
4. [Real-Time Notifications (SSE)](#real-time-notifications-server-sent-events)
5. [Data Models](#data-models)
6. [Test Environment Configuration](#test-environment-configuration)
7. [Error Handling](#error-handling)
8. [Examples & Sample Requests](#examples--sample-requests)
9. [Testing Tools](#testing-tools)

## Overview

The Trading Partner Portal Backend API provides REST endpoints for managing EDI trading partner credentials and monitoring file transfer operations. It's built with ASP.NET Core and follows OpenAPI specifications.

### Base URLs

- **Development HTTP**: `http://localhost:5096`
- **Development HTTPS**: `https://localhost:7096`
- **Swagger UI**: Available at both URLs with `/swagger` path

### Key Features

- PGP key management (upload, generate, revoke, promote)
- SFTP credential management
- Dashboard metrics and analytics
- Audit trail for security operations
- Real-time monitoring data
- Server-Sent Events (SSE) for real-time notifications

## API Endpoints

### System Endpoints

#### Health Check

```http
GET /api/health
```

Returns basic health status for the API service.

**Response:**

```json
{
  "status": "healthy",
  "timestamp": "2024-09-21T10:30:00.000Z"
}
```

#### Version Information

```http
GET /api/version
```

Returns API version and build information.

**Response:**

```json
{
  "version": "1.0.0",
  "build": "pilot",
  "timestamp": "2024-09-21T10:30:00.000Z"
}
```

### Dashboard Endpoints

#### Get Dashboard Summary

```http
GET /api/dashboard/summary
```

Returns key performance indicators for the dashboard.

**Response:**

```json
{
  "inboundFiles24h": 45,
  "outboundFiles24h": 38,
  "successRate": 94.2,
  "avgProcessingTime": 12.5,
  "openErrors": 3,
  "totalBytes24h": 15728640,
  "avgFileSizeBytes": 189456.3,
  "connectionSuccessRate24h": 98.1,
  "largeFileCount24h": 2
}
```

#### Get Time Series Data

```http
GET /api/dashboard/timeseries?from={datetime}&to={datetime}
```

Returns hourly file count data for charting.

**Parameters:**

- `from` (optional): Start datetime (ISO 8601 format, defaults to 48 hours ago)
- `to` (optional): End datetime (ISO 8601 format, defaults to now)

**Response:**

```json
{
  "points": [
    {
      "timestamp": "2024-09-21T10:00:00.000Z",
      "inboundCount": 5,
      "outboundCount": 3
    }
  ]
}
```

#### Get Top Errors

```http
GET /api/dashboard/errors/top?from={datetime}&to={datetime}&top={number}
```

Returns the most frequent error categories.

**Parameters:**

- `from` (optional): Start datetime (defaults to 24 hours ago)
- `to` (optional): End datetime (defaults to now)
- `top` (optional): Number of top errors to return (default: 5)

**Response:**

```json
{
  "categories": [
    {
      "category": "VALIDATION_FAILED",
      "count": 12
    },
    {
      "category": "TIMEOUT",
      "count": 8
    }
  ]
}
```

#### Get Connection Health

```http
GET /api/dashboard/connection/health?from={datetime}&to={datetime}
```

Returns connection success/failure metrics over time.

**Response:**

```json
[
  {
    "timestamp": "2024-09-21T10:00:00.000Z",
    "success": 45,
    "failed": 2,
    "authFailed": 1,
    "successRatePct": 93.75
  }
]
```

#### Get Connection Status

```http
GET /api/dashboard/connection/status
```

Returns current connection status for the partner.

**Response:**

```json
{
  "partnerId": "11111111-1111-1111-1111-111111111111",
  "status": "Connected",
  "lastCheck": "2024-09-21T10:25:00.000Z"
}
```

#### Get Throughput Metrics

```http
GET /api/dashboard/throughput?from={datetime}&to={datetime}
```

Returns data throughput metrics over time.

**Response:**

```json
[
  {
    "timestamp": "2024-09-21T10:00:00.000Z",
    "totalBytes": 2048576,
    "fileCount": 12,
    "avgFileSizeBytes": 170714.7
  }
]
```

#### Get Large Files

```http
GET /api/dashboard/large-files?from={datetime}&to={datetime}&limit={number}
```

Returns information about large files processed.

**Parameters:**

- `limit` (optional): Maximum number of files to return (default: 10, max: 50)

**Response:**

```json
[
  {
    "fileName": "large_edi_850_20240921.txt",
    "sizeBytes": 8388608,
    "receivedAt": "2024-09-21T09:15:00.000Z"
  }
]
```

#### Get Connection Performance

```http
GET /api/dashboard/connection/performance?from={datetime}&to={datetime}
```

Returns connection performance metrics.

**Response:**

```json
[
  {
    "timestamp": "2024-09-21T10:00:00.000Z",
    "avgMs": 125.4,
    "p95Ms": 450.2,
    "maxMs": 1205.8,
    "count": 45
  }
]
```

#### Get Daily Summary

```http
GET /api/dashboard/daily-summary?days={number}
```

Returns daily operation summaries.

**Parameters:**

- `days` (optional): Number of days to include (default: 7, max: 14)

**Response:**

```json
[
  {
    "date": "2024-09-21",
    "totalFiles": 83,
    "successfulFiles": 78,
    "failedFiles": 5,
    "successRatePct": 93.98
  }
]
```

#### Get Failure Bursts

```http
GET /api/dashboard/failure-bursts?lookbackMinutes={number}
```

Returns periods of high failure rates.

**Parameters:**

- `lookbackMinutes` (optional): Minutes to look back (default: 1440 = 24 hours)

**Response:**

```json
[
  {
    "windowStart": "2024-09-21T08:00:00.000Z",
    "failureCount": 8
  }
]
```

#### Get Zero File Window Status

```http
GET /api/dashboard/zero-file-window?windowHours={number}
```

Returns information about periods with no file activity.

**Parameters:**

- `windowHours` (optional): Hours to check (default: 4, range: 1-12)

**Response:**

```json
{
  "windowHours": 4,
  "inboundFiles": 0,
  "flagged": true
}
```

### Real-Time Notifications (Server-Sent Events)

#### SSE Stream Endpoint

```http
GET /api/events/stream
```

Establishes a Server-Sent Events connection for real-time notifications about key operations and system events.

**Headers Required:**

- `X-Session-Token`: Valid session token
- `Accept: text/event-stream` (recommended)
- `Cache-Control: no-cache` (recommended)

**Optional Headers:**

- `Last-Event-ID`: Resume from specific event sequence (for reconnection)

**Response Format:**

The endpoint returns a continuous stream of events in SSE format:

```text
event: key.promoted
id: 1
data: {"keyId":"abc123","previousPrimaryKeyId":"def456"}

event: key.revoked  
id: 2
data: {"keyId":"xyz789"}

:hb

event: file.created
id: 3
data: {"fileId":"file123","direction":"Inbound","docType":"850"}
```

#### Event Types

The SSE stream delivers these event types:

**Key Management Events:**

- `key.promoted`: A key has been promoted to primary status
- `key.revoked`: A key has been revoked

**File Transfer Events:**

- `file.created`: New file has been received or sent
- `file.statusChanged`: File processing status has changed

**Dashboard Events:**

- `dashboard.metricsTick`: Real-time dashboard metrics update

**Connection Management:**

- `:hb`: Heartbeat sent every 15 seconds to keep connection alive

#### Event Data Schemas

**Key Promoted Event:**

```json
{
  "keyId": "string",
  "previousPrimaryKeyId": "string|null"
}
```

**Key Revoked Event:**

```json
{
  "keyId": "string"
}
```

**File Created Event:**

```json
{
  "fileId": "string",
  "direction": "Inbound|Outbound", 
  "docType": "string"
}
```

**File Status Changed Event:**

```json
{
  "fileId": "string",
  "oldStatus": "string",
  "newStatus": "string"
}
```

#### Implementation Example

```javascript
// Establish SSE connection
function connectToEventStream() {
  const eventSource = new EventSource('/api/events/stream', {
    headers: {
      'X-Session-Token': sessionToken
    }
  });

  // Handle key promotion events
  eventSource.addEventListener('key.promoted', (event) => {
    const data = JSON.parse(event.data);
    console.log('Key promoted:', data.keyId);
    // Update UI to reflect new primary key
    updateKeyStatus(data.keyId, 'primary');
  });

  // Handle key revocation events
  eventSource.addEventListener('key.revoked', (event) => {
    const data = JSON.parse(event.data);
    console.log('Key revoked:', data.keyId);
    // Update UI to show revoked status
    updateKeyStatus(data.keyId, 'revoked');
  });

  // Handle file events
  eventSource.addEventListener('file.created', (event) => {
    const data = JSON.parse(event.data);
    console.log('New file:', data.fileId, data.direction);
    // Update dashboard or file list
    refreshDashboard();
  });

  // Handle connection errors
  eventSource.onerror = (error) => {
    console.error('SSE connection error:', error);
    // Implement exponential backoff reconnection
    setTimeout(() => connectToEventStream(), 5000);
  };

  // Store last event ID for reconnection
  eventSource.onmessage = (event) => {
    if (event.lastEventId) {
      localStorage.setItem('lastEventId', event.lastEventId);
    }
  };

  return eventSource;
}

// Reconnection with event replay
function reconnectWithReplay() {
  const lastEventId = localStorage.getItem('lastEventId');
  const url = lastEventId 
    ? `/api/events/stream?lastEventId=${lastEventId}`
    : '/api/events/stream';
    
  const eventSource = new EventSource(url, {
    headers: {
      'X-Session-Token': sessionToken,
      'Last-Event-ID': lastEventId || ''
    }
  });
  
  // ... handle events as above
}
```

#### Best Practices for SSE Implementation

**Connection Management:**

- Always handle connection errors and implement reconnection logic
- Use exponential backoff for reconnection attempts (start with 1s, max 30s)
- Store the `Last-Event-ID` to resume from the correct point after disconnection

**Event Handling:**

- Parse event data as JSON
- Implement event-specific handlers for different event types
- Update UI incrementally rather than refreshing entire sections

**Performance:**

- Limit to one SSE connection per browser tab
- Close connections when navigating away from pages that need real-time updates
- Use heartbeat events to detect broken connections

**Error Handling:**

```javascript
eventSource.addEventListener('error', (event) => {
  if (eventSource.readyState === EventSource.CONNECTING) {
    console.log('Reconnecting to event stream...');
  } else {
    console.error('SSE connection failed');
    // Implement fallback polling if needed
  }
});
```

**Authentication:**

- SSE connections respect the same authentication requirements as REST endpoints
- Include session token in connection headers
- Handle 401/403 errors by redirecting to login

### PGP Key Management

#### List Keys

```http
GET /api/keys
```

Returns all PGP keys for the authenticated partner.

**Response:**

```json
[
  {
    "keyId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "fingerprint": "1234567890ABCDEF1234567890ABCDEF12345678",
    "algorithm": "RSA",
    "keySize": 2048,
    "createdAt": "2024-09-11T10:00:00.000Z",
    "validFrom": "2024-09-11T10:00:00.000Z",
    "validTo": "2025-09-11T10:00:00.000Z",
    "status": "Active",
    "isPrimary": true
  }
]
```

#### Upload Key

```http
POST /api/keys/upload
```

**Required Role:** PartnerAdmin

Uploads a new PGP public key.

**Request Body:**

```json
{
  "publicKeyArmored": "-----BEGIN PGP PUBLIC KEY BLOCK-----\n...\n-----END PGP PUBLIC KEY BLOCK-----",
  "validFrom": "2024-09-21T00:00:00.000Z",
  "validTo": "2025-09-21T00:00:00.000Z",
  "makePrimary": true
}
```

**Response:**

```json
{
  "keyId": "new-key-id",
  "fingerprint": "ABCDEF1234567890ABCDEF1234567890ABCDEF12",
  "algorithm": "RSA",
  "keySize": 4096,
  "createdAt": "2024-09-21T10:30:00.000Z",
  "validFrom": "2024-09-21T00:00:00.000Z",
  "validTo": "2025-09-21T00:00:00.000Z",
  "status": "Active",
  "isPrimary": true
}
```

#### Generate Key

```http
POST /api/keys/generate
```

**Required Role:** PartnerAdmin

Generates a new PGP key pair server-side.

**Request Body:**

```json
{
  "validFrom": "2024-09-21T00:00:00.000Z",
  "validTo": "2025-09-21T00:00:00.000Z",
  "makePrimary": false
}
```

**Response:**

```json
{
  "privateKeyArmored": "-----BEGIN PGP PRIVATE KEY BLOCK-----\n...\n-----END PGP PRIVATE KEY BLOCK-----",
  "key": {
    "keyId": "generated-key-id",
    "fingerprint": "GENERATED1234567890ABCDEF1234567890ABCDEF",
    "algorithm": "RSA",
    "keySize": 4096,
    "createdAt": "2024-09-21T10:30:00.000Z",
    "validFrom": "2024-09-21T00:00:00.000Z",
    "validTo": "2025-09-21T00:00:00.000Z",
    "status": "Active",
    "isPrimary": false
  }
}
```

**⚠️ Security Note:** The private key is returned only once and should be immediately saved by the client.

#### Revoke Key

```http
POST /api/keys/{keyId}/revoke
```

**Required Role:** PartnerAdmin

Revokes a PGP key, making it unavailable for future operations.

**Request Body:**

```json
{
  "reason": "Key rotation - replaced with new key"
}
```

**Response:**

```json
{
  "success": true,
  "auditId": "audit-record-id"
}
```

#### Promote Key

```http
POST /api/keys/{keyId}/promote
```

**Required Role:** PartnerAdmin

Promotes a key to primary status for outbound encryption.

**Response:**

```json
{
  "success": true,
  "auditId": "audit-record-id"
}
```

### SFTP Credential Management

#### Get Credential Metadata

```http
GET /api/sftp/credential
```

Returns SFTP credential metadata (without the actual password).

**Response:**

```json
{
  "lastRotatedAt": "2024-09-15T14:20:00.000Z",
  "rotationMethod": "auto"
}
```

#### Rotate Password

```http
POST /api/sftp/credential/rotate
```

**Required Role:** PartnerAdmin

Rotates the SFTP password either automatically or with a provided password.

**Request Body (Auto-generate):**

```json
{
  "mode": "auto"
}
```

**Request Body (Manual):**

```json
{
  "mode": "manual",
  "newPassword": "ComplexPassword123!@#"
}
```

**Response:**

```json
{
  "password": "NewGeneratedPassword123!@#",
  "metadata": {
    "lastRotatedAt": "2024-09-21T10:30:00.000Z",
    "rotationMethod": "auto"
  }
}
```

**⚠️ Security Note:** The password is returned only once and should be immediately saved by the client.

## Authentication & Authorization

### Middleware-Based Authentication (Test Environment)

For testing purposes, the API uses predefined session tokens that are recognized by authentication middleware. In production, this will be replaced with Microsoft Entra External ID.

#### Test Session Tokens

The system recognizes these predefined test tokens:

**PartnerAdmin Access:**

- `admin-session-token`
- Any token starting with `test-admin-` (e.g., `test-admin-user1`)

**PartnerUser Access:**

- `user-session-token`
- `test-session-token`
- Any token starting with `test-user-` (e.g., `test-user-john`)

#### Using Session Tokens

Include any of the predefined session tokens in the `X-Session-Token` header for all protected endpoints:

```http
GET /api/dashboard/summary
X-Session-Token: admin-session-token
```

**No login endpoint required** - just use the predefined tokens directly.

### Roles

- **PartnerUser**: Read-only access to dashboard and key listings
- **PartnerAdmin**: Full access including key management and SFTP operations
- **InternalSupport**: Extended read access for troubleshooting (not implemented in test environment)

## Data Models

### Key Status Values

- `Active`: Key is valid and can be used
- `Revoked`: Key has been revoked and cannot be used
- `Expired`: Key has passed its validity period
- `PendingActivation`: Key is not yet valid (future-dated)
- `Superseded`: Key has been replaced but may still accept inbound for overlap period

### File Status Values

- `Success`: File processed successfully
- `Failed`: File processing failed
- `Processing`: File is currently being processed
- `Pending`: File is queued for processing

### Connection Outcomes

- `Success`: Connection established successfully
- `Failed`: Connection failed due to network or technical issues
- `AuthFailed`: Connection failed due to authentication issues

### Document Types (EDI)

Common EDI document types you'll see in test data:

- `850`: Purchase Order
- `810`: Invoice
- `997`: Functional Acknowledgment
- `856`: Advance Ship Notice
- `832`: Price/Sales Catalog
- `204`: Motor Carrier Load Tender

## Test Environment Configuration

### Automatic Data Seeding

The test environment automatically seeds sample data when the API starts. This ensures consistent, predictable data for development and testing.

### Test Partners

The system creates these test partners:

1. **Acme Corporation** (`11111111-1111-1111-1111-111111111111`)
   - Status: Active
   - Created: 90 days ago
   - Has 2 PGP keys (1 primary, 1 secondary)

2. **Global Logistics Inc** (`22222222-2222-2222-2222-222222222222`)
   - Status: Active
   - Created: 75 days ago

3. **TechFlow Systems** (`33333333-3333-3333-3333-333333333333`)
   - Status: Active
   - Created: 60 days ago

4. **MegaTrade Ltd** (`44444444-4444-4444-4444-444444444444`)
   - Status: Active
   - Created: 45 days ago

5. **DataSync Partners** (`55555555-5555-5555-5555-555555555555`)
   - Status: Suspended
   - Created: 30 days ago

### Test PGP Keys

Each partner gets seeded with:

- **Primary Active Key**: RSA 4096-bit, created 30 days ago, valid for 365 days
- **Secondary Active Key**: RSA 4096-bit, created 10 days ago, valid for 365 days
- **Revoked Key** (optional): RSA 2048-bit, created 120 days ago, revoked 30 days ago

### Test Data Characteristics

#### File Transfer Events (Last 30 Days)

- **Volume**: 5-25 events per day per partner
- **Success Rate**: ~80% success, ~15% failed, ~5% processing/pending
- **File Sizes**: 1KB to 10MB
- **Document Types**: Mix of 850, 810, 997, 856, 832, 204
- **Processing Time**: 1-30 minutes for successful files

#### Connection Events (Last 7 Days)

- **Volume**: 20-100 connections per day per partner
- **Success Rate**: ~80% success, ~15% failed, ~5% auth failed
- **Connection Times**: 50-2000ms for successful connections, 5-30 seconds for failures

#### Error Messages

Sample error messages you'll see in test data:

- "Invalid document structure in line 45"
- "Missing required segment ISA"
- "Authentication timeout after 30 seconds"
- "File size exceeds maximum limit of 50MB"
- "Unsupported document type specified"
- "Network connection interrupted during transfer"
- "Encryption key validation failed"
- "Trading partner not found in directory"
- "Duplicate transaction control number detected"
- "Schema validation failed for segment GS"

#### Audit Events (Last 60 Days)

- **Types**: KeyUpload, KeyGenerate, KeyRevoke, SftpPasswordChange, KeyPromote
- **Success Rate**: ~90% successful operations
- **Actors**: Mix of PartnerAdmin, PartnerUser, and InternalSupport roles

### Test Credentials

#### Default Test User

- **User ID**: `test-user@acme.com`
- **Partner ID**: `11111111-1111-1111-1111-111111111111` (Acme Corporation)
- **Role**: `PartnerAdmin`

#### SFTP Credentials

Each partner has SFTP credentials with:

- **Password**: Securely hashed (not retrievable)
- **Last Rotation**: Random date within last 90 days
- **Method**: Mix of auto and manual rotations

## Error Handling

### Standard Error Response Format

All API errors return a consistent format:

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error description",
    "traceId": "unique-trace-identifier"
  }
}
```

### Common Error Codes

#### Authentication Errors

- `UNAUTHORIZED` (401): Missing or invalid session token
- `FORBIDDEN` (403): User doesn't have required permissions

#### Validation Errors

- `VALIDATION_FAILED` (400): Request data validation failed
- `NOT_FOUND` (404): Requested resource doesn't exist
- `CONFLICT` (409): Operation conflicts with current state (e.g., trying to revoke already revoked key)

#### Server Errors

- `INTERNAL_ERROR` (500): Unexpected server error

### Error Scenarios by Endpoint

#### Key Management

- **Upload Key**:
  - Invalid PGP format
  - Duplicate fingerprint
  - Key size too small (< 2048 bits)
- **Generate Key**:
  - Invalid date ranges
  - System unable to generate key
- **Revoke Key**:
  - Key not found
  - Key already revoked
  - Cannot revoke primary key without replacement

#### SFTP Operations

- **Rotate Password**:
  - Weak password (manual mode)
  - No existing credential found

## Examples & Sample Requests

### Complete Dashboard Data Flow

```javascript
// 1. Use predefined test token (no login required)
const sessionToken = 'admin-session-token'; // or any other predefined token

// 2. Get dashboard summary
const summaryResponse = await fetch('/api/dashboard/summary', {
  headers: { 'X-Session-Token': sessionToken }
});
const summary = await summaryResponse.json();

// 3. Get time series for last 24 hours
const now = new Date();
const yesterday = new Date(now.getTime() - 24 * 60 * 60 * 1000);
const timeSeriesResponse = await fetch(
  `/api/dashboard/timeseries?from=${yesterday.toISOString()}&to=${now.toISOString()}`,
  { headers: { 'X-Session-Token': sessionToken } }
);
const timeSeries = await timeSeriesResponse.json();

// 4. Get current connection status
const statusResponse = await fetch('/api/dashboard/connection/status', {
  headers: { 'X-Session-Token': sessionToken }
});
const connectionStatus = await statusResponse.json();
```

### Key Management Workflow

```javascript
// 1. List existing keys
const keysResponse = await fetch('/api/keys', {
  headers: { 'X-Session-Token': sessionToken }
});
const keys = await keysResponse.json();

// 2. Generate new key pair
const generateResponse = await fetch('/api/keys/generate', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-Session-Token': sessionToken
  },
  body: JSON.stringify({
    validFrom: new Date().toISOString(),
    validTo: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString(),
    makePrimary: false
  })
});
const { privateKeyArmored, key } = await generateResponse.json();

// ⚠️ IMPORTANT: Save the private key immediately - it's shown only once!
console.log('Save this private key:', privateKeyArmored);

// 3. Promote the new key to primary
const promoteResponse = await fetch(`/api/keys/${key.keyId}/promote`, {
  method: 'POST',
  headers: { 'X-Session-Token': sessionToken }
});
const promoteResult = await promoteResponse.json();
```

### Real-Time Notifications with SSE

```javascript
// Complete SSE implementation with error handling and reconnection
class EventStreamManager {
  constructor(sessionToken) {
    this.sessionToken = sessionToken;
    this.eventSource = null;
    this.reconnectAttempts = 0;
    this.maxReconnectAttempts = 10;
    this.reconnectDelay = 1000; // Start with 1 second
    this.lastEventId = localStorage.getItem('lastSSEEventId');
  }

  connect() {
    // Build URL with Last-Event-ID if available
    let url = '/api/events/stream';
    if (this.lastEventId) {
      url += `?lastEventId=${this.lastEventId}`;
    }

    this.eventSource = new EventSource(url, {
      headers: {
        'X-Session-Token': this.sessionToken
      }
    });

    // Set up event listeners
    this.setupEventListeners();

    console.log('SSE connection established');
  }

  setupEventListeners() {
    // Generic message handler to capture event IDs
    this.eventSource.onmessage = (event) => {
      if (event.lastEventId) {
        this.lastEventId = event.lastEventId;
        localStorage.setItem('lastSSEEventId', event.lastEventId);
      }
    };

    // Key management events
    this.eventSource.addEventListener('key.promoted', (event) => {
      const data = JSON.parse(event.data);
      this.handleKeyPromoted(data);
    });

    this.eventSource.addEventListener('key.revoked', (event) => {
      const data = JSON.parse(event.data);
      this.handleKeyRevoked(data);
    });

    // File transfer events
    this.eventSource.addEventListener('file.created', (event) => {
      const data = JSON.parse(event.data);
      this.handleFileCreated(data);
    });

    this.eventSource.addEventListener('file.statusChanged', (event) => {
      const data = JSON.parse(event.data);
      this.handleFileStatusChanged(data);
    });

    // Dashboard events
    this.eventSource.addEventListener('dashboard.metricsTick', (event) => {
      const data = JSON.parse(event.data);
      this.handleDashboardUpdate(data);
    });

    // Error handling
    this.eventSource.onerror = (error) => {
      console.error('SSE connection error:', error);
      this.handleConnectionError();
    };

    // Connection state monitoring
    this.eventSource.onopen = () => {
      console.log('SSE connection opened');
      this.reconnectAttempts = 0;
      this.reconnectDelay = 1000;
    };
  }

  handleKeyPromoted(data) {
    console.log('Key promoted:', data.keyId);
    // Update key management UI
    this.updateKeyInUI(data.keyId, { isPrimary: true });
    if (data.previousPrimaryKeyId) {
      this.updateKeyInUI(data.previousPrimaryKeyId, { isPrimary: false });
    }
    // Show notification
    this.showNotification('Key promoted to primary', 'info');
  }

  handleKeyRevoked(data) {
    console.log('Key revoked:', data.keyId);
    // Update key management UI
    this.updateKeyInUI(data.keyId, { status: 'Revoked' });
    // Show notification
    this.showNotification('Key has been revoked', 'warning');
  }

  handleFileCreated(data) {
    console.log('New file:', data.fileId, data.direction);
    // Update dashboard counters
    this.incrementFileCounter(data.direction);
    // Refresh file list if visible
    if (this.isFileListVisible()) {
      this.refreshFileList();
    }
    // Show notification for important files
    if (data.docType === '850') {
      this.showNotification(`New Purchase Order received`, 'success');
    }
  }

  handleFileStatusChanged(data) {
    console.log('File status changed:', data.fileId, data.newStatus);
    // Update file status in UI
    this.updateFileStatus(data.fileId, data.newStatus);
    // Show notification for failures
    if (data.newStatus === 'Failed') {
      this.showNotification(`File processing failed`, 'error');
    }
  }

  handleDashboardUpdate(data) {
    // Update dashboard metrics in real-time
    this.updateDashboardMetrics(data.summary);
  }

  handleConnectionError() {
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;
      console.log(`Attempting to reconnect (${this.reconnectAttempts}/${this.maxReconnectAttempts}) in ${this.reconnectDelay}ms`);
      
      setTimeout(() => {
        this.disconnect();
        this.connect();
      }, this.reconnectDelay);

      // Exponential backoff with jitter
      this.reconnectDelay = Math.min(this.reconnectDelay * 2, 30000);
    } else {
      console.error('Max reconnection attempts reached');
      this.showNotification('Lost connection to server', 'error');
    }
  }

  disconnect() {
    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
    }
  }

  // Helper methods (implement based on your UI framework)
  updateKeyInUI(keyId, updates) {
    // Update key in your key management table/list
    const keyElement = document.querySelector(`[data-key-id="${keyId}"]`);
    if (keyElement && updates.isPrimary !== undefined) {
      keyElement.classList.toggle('primary-key', updates.isPrimary);
    }
    if (keyElement && updates.status) {
      keyElement.setAttribute('data-status', updates.status);
    }
  }

  updateFileStatus(fileId, status) {
    // Update file status in your file list
    const fileElement = document.querySelector(`[data-file-id="${fileId}"]`);
    if (fileElement) {
      fileElement.setAttribute('data-status', status);
    }
  }

  incrementFileCounter(direction) {
    // Update dashboard counters
    const counterElement = document.querySelector(`#${direction.toLowerCase()}-count`);
    if (counterElement) {
      const currentCount = parseInt(counterElement.textContent) || 0;
      counterElement.textContent = currentCount + 1;
    }
  }

  updateDashboardMetrics(summary) {
    // Update dashboard summary widgets
    Object.keys(summary).forEach(key => {
      const element = document.querySelector(`[data-metric="${key}"]`);
      if (element) {
        element.textContent = summary[key];
      }
    });
  }

  showNotification(message, type) {
    // Implement your notification system
    console.log(`[${type.toUpperCase()}] ${message}`);
  }

  isFileListVisible() {
    // Check if file list is currently visible
    return document.querySelector('#file-list')?.offsetParent !== null;
  }

  refreshFileList() {
    // Trigger file list refresh
    // This could dispatch a custom event or call a refresh method
    window.dispatchEvent(new CustomEvent('refreshFileList'));
  }
}

// Usage
const sessionToken = 'admin-session-token';
const eventManager = new EventStreamManager(sessionToken);

// Start listening to events when page loads
document.addEventListener('DOMContentLoaded', () => {
  eventManager.connect();
});

// Clean up when page unloads
window.addEventListener('beforeunload', () => {
  eventManager.disconnect();
});

// Reconnect when page becomes visible again (handles tab switching)
document.addEventListener('visibilitychange', () => {
  if (!document.hidden && (!eventManager.eventSource || eventManager.eventSource.readyState === EventSource.CLOSED)) {
    eventManager.connect();
  }
});
```

### Error Handling Example

```javascript
async function fetchWithErrorHandling(url, options = {}) {
  try {
    const response = await fetch(url, options);
    
    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(`${errorData.error.code}: ${errorData.error.message}`);
    }
    
    return await response.json();
  } catch (error) {
    if (error.name === 'TypeError') {
      // Network error
      throw new Error('Network error - please check your connection');
    }
    throw error; // Re-throw API errors
  }
}

// Usage
try {
  const summary = await fetchWithErrorHandling('/api/dashboard/summary', {
    headers: { 'X-Session-Token': sessionToken }
  });
  console.log('Dashboard data:', summary);
} catch (error) {
  console.error('Failed to load dashboard:', error.message);
  // Handle error in UI
}
```

## Testing Tools

### REST Client (.http files)

The project includes comprehensive REST client files for testing:

- **Main test file**: `.vscode/api-tests.http`
- **Basic endpoints**: `TradingPartnerPortal.Api/TradingPartnerPortal.Api.http`

These files contain pre-configured requests for all endpoints with sample data.

### VS Code Tasks

Use these VS Code tasks for development:

- **Build Solution**: Compiles the project
- **Run Trading Partner Portal API**: Starts the API server (HTTP)
- **Run Trading Partner Portal API (HTTPS)**: Starts the API server (HTTPS)
- **Run Tests**: Executes unit tests
- **Validate API Health**: Quick health check

### Swagger UI

Access the interactive API documentation at:

- HTTP: `http://localhost:5096/swagger`
- HTTPS: `https://localhost:7096/swagger`

The Swagger UI provides:

- Interactive endpoint testing
- Request/response schema documentation
- Authentication testing
- Example requests and responses

### Sample Data Reset

The test environment automatically reseeds data on each API startup. To get fresh test data:

1. Stop the API (Ctrl+C in terminal)
2. Restart using the VS Code task "Run Trading Partner Portal API"
3. Wait for "Successfully seeded test data" log message

### Development Workflow

1. **Start the API**: Use VS Code task or run `dotnet run --project TradingPartnerPortal.Api`
2. **Verify health**: GET `/api/health` should return "healthy"
3. **Use test tokens**: Include predefined tokens like `admin-session-token` in `X-Session-Token` header
4. **Test endpoints**: All protected endpoints will work with the predefined test tokens
5. **Monitor logs**: Check console output for errors and audit events

### Performance Testing

For performance testing of dashboard endpoints:

- Time series data: Test with various date ranges (max 90 days)
- Large file queries: Test with different limit values (max 50)
- Connection metrics: Test during peak hours simulation
- Dashboard summary: Monitor response times for real-time data

The test environment includes simulated latency and realistic data volumes to help identify performance issues early in development.
