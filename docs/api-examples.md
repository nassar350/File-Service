# File Service API Examples

## Enum values

Enums are serialized as strings:

- FileProvider: "Unknown", "Cloudinary", "AmazonS3", "Local"
- FileStatus: "Unknown", "Staged", "Committed", "Deleted", "Failed"
- FileEventType: "Unknown", "Staged", "Committed", "Replaced", "Aborted", "Deleted", "MetadataUpdated"
- FileCleanupLogStatus: "Unknown", "Pending", "InProgress", "Succeeded", "Failed", "Skipped"

## Metadata shape

The `metadata` payload must be a JSON object. Use it to carry important details not stored in relational columns.
Example:

```json
{
  "source": "mobile",
  "tags": ["license", "pharmacy"],
  "notes": "Uploaded during verification"
}
```

## Response shape

File responses are trimmed summaries. They do not include `metadata`, `events`, or `cleanupLogs`.

Common response fields:

- id
- purpose
- entityType
- entityId
- provider
- providerKey
- storageRegion
- originalFilename
- mimeType
- size
- checksum
- status
- stagingExpiresAt
- committedAt
- deletedAt
- createdAt
- updatedAt

## Stage upload

POST /v1/uploads/sessions

Request (multipart/form-data fields):

- tenantId: "3f2c8f42-7f7f-4e3f-8d2a-6f4a3a7c1a33"
- ownerId: "5aa3cf75-4c8b-4e9b-8b8b-8a00a7df9f10"
- purpose: "license"
- entityType: "pharmacy"
- entityId: "9f2c0af0-2b1c-44b3-b0c1-8d2e86d8b8cc"
- metadata: {see Metadata shape above}
- provider: 1
- stagingTtlMinutes: 15
- file: <binary>

Response:

```json
{
  "id": "8d4b2e52-8b6e-4f1e-8c9c-4fe2b6a7f4e1",
  "purpose": "license",
  "entityType": "pharmacy",
  "entityId": "9f2c0af0-2b1c-44b3-b0c1-8d2e86d8b8cc",
  "provider": "Cloudinary",
  "providerKey": "staging/8d4b2e52",
  "storageRegion": "",
  "originalFilename": "license.pdf",
  "mimeType": "application/pdf",
  "size": 483920,
  "checksum": "",
  "status": "Staged",
  "stagingExpiresAt": "2026-05-08T12:15:00Z",
  "committedAt": null,
  "deletedAt": null,
  "createdAt": "2026-05-08T12:00:00Z",
  "updatedAt": "2026-05-08T12:00:00Z"
}
```

## Commit upload

POST /v1/uploads/sessions/{fileId}/commit

Request:

```json
{
  "destinationFolder": null,
  "replacesFileId": null
}
```

If `destinationFolder` is omitted or null, the service promotes the file to the default path ending with `/entityType/purpose/entityId`.

Response:

```json
{
  "id": "8d4b2e52-8b6e-4f1e-8c9c-4fe2b6a7f4e1",
  "purpose": "license",
  "entityType": "pharmacy",
  "entityId": "9f2c0af0-2b1c-44b3-b0c1-8d2e86d8b8cc",
  "provider": "Cloudinary",
  "providerKey": "files/pharmacy/license/9f2c0af0-2b1c-44b3-b0c1-8d2e86d8b8cc",
  "storageRegion": "",
  "originalFilename": "license.pdf",
  "mimeType": "application/pdf",
  "size": 483920,
  "checksum": "",
  "status": "Committed",
  "committedAt": "2026-05-08T12:02:00Z",
  "stagingExpiresAt": null,
  "deletedAt": null,
  "createdAt": "2026-05-08T12:00:00Z",
  "updatedAt": "2026-05-08T12:02:00Z"
}
```

## Replace upload

POST /v1/uploads/sessions/{fileId}/replace

Request (multipart/form-data fields):

- metadata: {see Metadata shape above}
- file: <binary>

Response: same trimmed shape as stage upload, with a new staged file record.

## Abort upload

POST /v1/uploads/sessions/{fileId}/abort

Response: 204 No Content

## Get file

GET /v1/files/{fileId}

Response: same trimmed shape as stage upload.

## Delete file

DELETE /v1/files/{fileId}

Response: 204 No Content
