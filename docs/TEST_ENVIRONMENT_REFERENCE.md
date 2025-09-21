# Test Environment Quick Reference

## Test Partner Credentials

### Primary Test Partner: Acme Corporation
- **Partner ID**: `11111111-1111-1111-1111-111111111111`
- **Status**: Active
- **Test User**: `test-user@acme.com`
- **Role**: PartnerAdmin

### Additional Test Partners
- **Global Logistics Inc**: `22222222-2222-2222-2222-222222222222`
- **TechFlow Systems**: `33333333-3333-3333-3333-333333333333`  
- **MegaTrade Ltd**: `44444444-4444-4444-4444-444444444444`
- **DataSync Partners**: `55555555-5555-5555-5555-555555555555` (Suspended)

## Predefined Test Tokens

### PartnerAdmin Tokens
- `admin-session-token`
- `test-admin-user1`
- `test-admin-manager`

### PartnerUser Tokens
- `user-session-token`
- `test-session-token`
- `test-user-john`
- `test-user-jane`

### Usage
Simply include any predefined token in the `X-Session-Token` header:

```bash
curl -H "X-Session-Token: admin-session-token" \
  http://localhost:5096/api/dashboard/summary
```

## Test Data Characteristics

### File Transfer Events (30 days)
- **Volume**: 5-25 files/day per partner
- **Success Rate**: ~80%
- **File Sizes**: 1KB - 10MB
- **Document Types**: 850, 810, 997, 856, 832, 204

### Connection Events (7 days)  
- **Volume**: 20-100 connections/day per partner
- **Success Rate**: ~80%
- **Response Times**: 50-2000ms (success), 5-30s (failure)

### PGP Keys per Partner
- **Primary Key**: RSA 4096, Active, created 30 days ago
- **Secondary Key**: RSA 4096, Active, created 10 days ago  
- **Revoked Key**: RSA 2048, Revoked, created 120 days ago

## Sample API Calls

### Get Dashboard Summary
```bash
curl -H "X-Session-Token: admin-session-token" \
  http://localhost:5096/api/dashboard/summary
```

### List Keys
```bash
curl -H "X-Session-Token: admin-session-token" \
  http://localhost:5096/api/keys
```

### Get Time Series (Last 24 Hours)
```bash
curl -H "X-Session-Token: admin-session-token" \
  "http://localhost:5096/api/dashboard/timeseries?from=$(date -u -d '24 hours ago' +%Y-%m-%dT%H:%M:%S.%3NZ)&to=$(date -u +%Y-%m-%dT%H:%M:%S.%3NZ)"
```

## Expected Response Patterns

### Dashboard Summary Response
```json
{
  "inboundFiles24h": 30-60,
  "outboundFiles24h": 20-50, 
  "successRate": 75-95,
  "avgProcessingTime": 5-25,
  "openErrors": 0-10,
  "totalBytes24h": 1000000-20000000,
  "connectionSuccessRate24h": 80-98
}
```

### Key Listing Response
```json
[
  {
    "keyId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "status": "Active",
    "isPrimary": true,
    "algorithm": "RSA",
    "keySize": 2048
  },
  {
    "keyId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", 
    "status": "Active",
    "isPrimary": false,
    "algorithm": "RSA",
    "keySize": 4096
  }
]
```

## Error Scenarios to Test

### Authentication Errors
- Missing session token → 401 Unauthorized
- Invalid session token → 401 Unauthorized  
- Insufficient permissions → 403 Forbidden

### Validation Errors
- Invalid partner ID → 404 Not Found
- Invalid key format → 400 Validation Failed
- Weak password → 400 Validation Failed

### Business Logic Errors
- Revoke already revoked key → 409 Conflict
- Upload duplicate key fingerprint → 409 Conflict
- Promote non-existent key → 404 Not Found

## Data Refresh

The test environment automatically seeds fresh data on each API restart. To reset test data:

1. Stop the API server (Ctrl+C)
2. Restart using VS Code task "Run Trading Partner Portal API"
3. Wait for "Successfully seeded test data" log message
4. Continue using the same predefined tokens (no re-login needed)

## Performance Expectations

### Response Times (Test Environment)
- Health/Version endpoints: < 50ms
- Dashboard summary: < 200ms
- Time series data: < 500ms  
- Key operations: < 300ms
- Large file queries: < 1s

### Data Volumes
- File events: ~5,000 records across all partners
- Connection events: ~2,000 records across all partners
- Audit events: ~500 records across all partners
- Active keys: 2 per partner (10 total)

## Development Tips

1. **Use the .http files** in `.vscode/api-tests.http` for quick testing
2. **Monitor logs** in the API console for detailed error information
3. **Check Swagger UI** at `http://localhost:5096/swagger` for interactive testing
4. **Use predefined tokens** - no login required, tokens never expire
5. **Data is consistent** - same seed data is generated each restart for predictable testing