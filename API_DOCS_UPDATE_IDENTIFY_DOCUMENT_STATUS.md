# API Documentation: Update IdentifyDocument Status

## Endpoint
**PATCH** `/api/IdentifyDocument/{id}/status`

## Description
Updates the status of an IdentifyDocument. When status is set to APPROVED or REJECTED, the system automatically sets:
- `verifiedBy`: Current user's name
- `verifiedAt`: Current timestamp

When status is set back to PENDING, these fields are reset to null.

## Authorization
Requires valid JWT token.

## Request Parameters

### Path Parameters
- `id` (string, required): The ID of the IdentifyDocument to update

### Request Body
```json
{
  "status": "APPROVED", // PENDING, APPROVED, or REJECTED
  "note": "Document verification completed" // Optional
}
```

## Response

### Success Response (200 OK)
```json
{
  "statusCode": 200,
  "code": "SUCCESS",
  "data": {
    "id": "document-id",
    "user": {
      // User information
    },
    "frontImage": "image-url",
    "backImage": "image-url",
    "countryCode": "VN",
    "numberMasked": "123***789",
    "licenseClass": "B1",
    "expireAt": "2025-12-31T00:00:00Z",
    "status": "APPROVED",
    "verifiedBy": "admin@system.com", // Auto-generated
    "verifiedAt": "2025-10-13T10:30:00Z", // Auto-generated
    "note": "Document verification completed",
    "createdAt": "2025-10-10T08:00:00Z",
    "updatedAt": "2025-10-13T10:30:00Z",
    "createdBy": "user@example.com",
    "updatedBy": "admin@system.com",
    "isDeleted": false
  },
  "message": "Identify document status updated successfully."
}
```

### Error Responses

#### 400 Bad Request
```json
{
  "message": "Invalid data."
}
```

#### 401 Unauthorized
```json
{
  "message": "Unauthorized access."
}
```

#### 404 Not Found
```json
{
  "message": "Identify document not found."
}
```

## Status Values
- `PENDING`: Initial status when document is submitted
- `APPROVED`: Document has been verified and approved
- `REJECTED`: Document has been rejected

## Auto-Generated Fields
When status changes to `APPROVED` or `REJECTED`:
- `verifiedBy`: Set to current user's name
- `verifiedAt`: Set to current UTC timestamp

When status changes back to `PENDING`:
- `verifiedBy`: Reset to null
- `verifiedAt`: Reset to null

## Usage Examples

### Approve a document
```bash
curl -X PATCH "https://api.example.com/api/IdentifyDocument/123/status" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "APPROVED",
    "note": "All information verified successfully"
  }'
```

### Reject a document
```bash
curl -X PATCH "https://api.example.com/api/IdentifyDocument/123/status" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "REJECTED",
    "note": "Document image is unclear, please resubmit"
  }'
```