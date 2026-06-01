# File Service

File Service manages the full lifecycle of uploaded files. It supports staged uploads, commit/replace/abort actions, storage-provider promotion, and background cleanup of expired staged files.

The service stores only the important file summary in its HTTP responses. It intentionally avoids returning the full domain graph so clients do not receive internal metadata collections or circular navigation data.

## What This Service Does

- Accepts an uploaded file and stages it in object storage.
- Creates a durable file record with status tracking.
- Commits a staged file into its final storage location.
- Replaces or aborts a staged file when the user changes their mind.
- Deletes expired staged files automatically through Hangfire.
- Supports multiple storage backends through a provider abstraction.

## File Lifecycle

### 1. Stage a file

`POST /v1/uploads/sessions`

The client sends the binary file plus the file context:

- `tenantId`
- `ownerId`
- `purpose`
- `entityType`
- `entityId`
- `metadata`
- `provider` (optional)
- `stagingTtlMinutes` (optional)

The service uploads the file into the configured staging folder, creates a `FileRecord`, and stores a `FileEvent` with the `Staged` event type.

### 2. Commit a file

`POST /v1/uploads/sessions/{fileId}/commit`

Commit promotes the staged object to its final storage path and updates the file status to `Committed`.

The commit request accepts `isPrivate`:

- `false` stores the file under `Public/`
- `true` stores the file under `Private/`

If the request does not provide a `destinationFolder`, the service uses a default path suffix built from the file context:

`/{entityType}/{purpose}/{entityId}`

If a `destinationFolder` is provided, it is treated as the base folder and the same suffix is appended to it.

### 3. Replace a staged file

`POST /v1/uploads/sessions/{fileId}/replace`

Replace deletes the current staged object, marks the old file record as deleted, and creates a new staged file record using the same business context.

### 4. Abort a staged file

`POST /v1/uploads/sessions/{fileId}/abort`

Abort deletes the staged object, marks the file as deleted, and records the operation as an event.

### 5. Get or delete a file

`GET /v1/files/{fileId}`

Returns the trimmed file summary for the requested file, including `fileLink`.

Public files return a direct link built from `Storage__S3__ServiceUrl` and the stored `providerKey`.
Private files return a pre-signed URL that expires after the dedicated private download TTL.

`DELETE /v1/files/{fileId}`

Deletes the file and records the operation.

## Storage Behavior

The service uses a storage-provider abstraction so the same workflow can target different backends.

Supported providers in the codebase:

- `Cloudinary`
- `AmazonS3`

The default provider is configured through `Storage__DefaultProvider`.

Each provider keeps two folders by default:

- `staging`
- `files`

Committed files are now split into `Public/` and `Private/` paths based on the commit request.

Staging uploads go to the staging folder first. Commit promotes the object into the final folder path. The final path is what gets persisted in `ProviderKey`.

## Cleanup of Expired Staged Files

Staged files have a TTL controlled by `Staging__TtlMinutes`.

- Default TTL: `15` minutes
- Cleanup schedule: once per day via Hangfire

This means a staged file is eligible for cleanup when its TTL expires, but actual deletion happens on the next scheduled cleanup run.

## Response Shape

API responses return a trimmed file summary. They include the file identity and storage details, but not:

- `metadata`
- `events`
- `cleanupLogs`

This keeps the API payload small and avoids exposing internal navigation data.

## Configuration

Configure the service with environment variables or `appsettings.json`.

### Storage

- `Storage__DefaultProvider` - default storage provider

### Cloudinary

- `Storage__Cloudinary__CloudName`
- `Storage__Cloudinary__ApiKey`
- `Storage__Cloudinary__ApiSecret`
- `Storage__Cloudinary__UploadPreset` - optional
- `Storage__Cloudinary__StagingFolder` - default: `staging`
- `Storage__Cloudinary__PublicFolder` - default: `files`

### Amazon S3

- `Storage__S3__BucketName`
- `Storage__S3__Region` - optional
- `Storage__S3__AccessKey` - optional
- `Storage__S3__SecretKey` - optional
- `Storage__S3__ServiceUrl` - optional
- `Storage__S3__ForcePathStyle` - default: `false`
- `Storage__S3__StagingFolder` - default: `staging`
- `Storage__S3__PublicFolder` - default: `files`
- `Storage__S3__PrivateDownloadUrlTtlMinutes` - default: `15`

`Storage__S3__ServiceUrl` is used when building public file links.
`Storage__S3__PrivateDownloadUrlTtlMinutes` controls the expiry for private download links.

### Staging

- `Staging__TtlMinutes` - default: `15`

## API Documentation

See [docs/api-examples.md](docs/api-examples.md) for request and response examples.

## Runtime Behavior

- Swagger is enabled in development.
- Controllers are exposed through ASP.NET Core MVC.
- Health checks are available at `/health`.
- Hangfire runs the cleanup worker in the background.

## Operational Notes

- Staged files are not permanent until commit succeeds.
- The service records file lifecycle events in the database.
- Responses are intentionally limited to the public file summary.
- The storage backend can be switched without changing the API contract.
