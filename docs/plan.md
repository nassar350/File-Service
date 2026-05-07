# File Service Plan

Goal: ship a staged upload file service backed by Cloudinary so users can replace files before commit and only the final asset is attached to domain records.

## Steps

1. Define the file domain model and metadata contract (file ID, owner, purpose, entity link, provider key, checksum, size, MIME type, status: staged/committed/deleted), aligned with existing Core schema notes.
2. Design the API surface: create upload session, get upload signature, commit upload, abort upload, replace file, read metadata, delete file; include permissions and tenant scoping.
3. Implement staged upload with Cloudinary: direct client upload into a staging folder, short TTL, explicit commit to promote the asset and link it to a domain record; abort deletes the staged asset.
4. Add a registry store (DB table) for file metadata and state transitions; ensure commits are atomic and replacements delete old assets after a successful swap.
5. Integrate auth and permissions (files.objects.upload/read/delete) using portal-gateway tokens or introspection, and add rate limits and size/type validation.
6. Wire core/portal-gateway to call the file service when creating or updating entities that use files (e.g., pharmacy logos, licenses), using the commit endpoint after business validation.
7. Document the flow and operational guidelines (staging TTL, cleanup job, failure modes), and add minimal smoke tests.

## Verification

1. Create an upload session, upload a staged file to Cloudinary, then replace it before commit and confirm the staged asset is removed.
2. Commit the final upload and verify the file metadata is persisted and linked to the target entity.
3. Replace a committed file and verify the old asset is deleted only after the new one is committed.
4. Confirm permission enforcement for upload/read/delete endpoints.

## Decisions

- Service location: new microservice.
- Storage provider: Cloudinary.
- Upload flow: staged upload + commit with direct client upload.

## Further Considerations

1. Metadata store location: new database for the file service vs. shared Core database. Recommendation: new database for clean service ownership.
2. Cleanup strategy: scheduled job to purge expired staged assets. Recommendation: nightly sweep + short TTL.
