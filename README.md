# File Service

This service provides a staged file upload flow backed by Cloudinary. Clients upload directly to Cloudinary using a signed request, then commit the upload to make it an active file record. If a user changes their mind before commit, they can replace or abort the staging session and the staged asset is deleted.

## Staged Upload Flow

1. `POST /v1/uploads/sessions` -> returns a staging `public_id` and a signature for direct upload.
2. Client uploads to Cloudinary using the returned parameters.
3. `POST /v1/uploads/sessions/{sessionId}/commit` -> promotes the staged asset and creates a file record.
4. `POST /v1/uploads/sessions/{sessionId}/replace` -> aborts the old session and creates a new staging session.
5. `POST /v1/uploads/sessions/{sessionId}/abort` -> deletes the staged asset.

## Configuration

Set these environment variables or `appsettings.json` values:

- `Cloudinary__CloudName`
- `Cloudinary__ApiKey`
- `Cloudinary__ApiSecret`
- `Cloudinary__UploadPreset` (optional)
- `Staging__Folder` (default: `staging`)
- `Staging__TtlMinutes` (default: 30)
- `FileService__PublicFolder` (default: `files`)

## Headers

Use these headers to associate a user with an upload session:

- `X-User-Id` (required for ownership)

## Notes

This implementation uses in-memory stores. Replace with a database-backed store for production.
