# Copilot Instructions

## Project Guidelines
- User prefers domain rules/validation to be implemented in Domain model methods, and UnitOfWork SaveChanges should be committed in handlers (application layer) instead of repositories.
- User expects claim parsing for current user id to be centralized in a reusable shared method/extension instead of duplicated local helper methods in controllers.
- Do not access Infrastructure directly from API controllers; controllers must go through the Application layer (dispatcher/commands/handlers) similar to the login/register pattern. Always scan and align source architecture before implementing.