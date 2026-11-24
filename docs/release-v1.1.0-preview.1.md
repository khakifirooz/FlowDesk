# FlowDesk 1.1.0 Preview 1

- Added current workspace screenshots, the live demo link, and setup instructions for both SQL Server migration sets.
- Added xUnit coverage for task transitions, archived tasks, invitation approval/expiry, and review validation.
- Added a disabled-by-default account deletion preview: password confirmation, project dependency checks, transactional cleanup of Identity and invitations, and retention of shared work history.
- Removed the manually generated deployment archive from the current source tree; create fresh IIS output with `dotnet publish`.

No migration is required for the preview changes themselves. Keep account deletion disabled on the public demo until the flow is confirmed against a separate SQL Server database. Deletion is blocked for project managers and linked members. Existing deployments are not updated by this release.
