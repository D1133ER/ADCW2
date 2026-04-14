# Project Analysis

Date: 2026-04-13
Project: ApplicationDevelopmentCoursework / Weblog Application
Scope: Read-only codebase analysis of architecture, security, correctness, maintainability, and local runtime setup.

## Executive Summary

This project is an ASP.NET Core MVC blogging application targeting .NET 8. It uses Entity Framework Core with SQL Server, session-based authentication, BCrypt password hashing, image uploads, and a simple admin dashboard.

The application works locally, but it is not ready for production deployment in its current form. The main issues are:

- Authorization decisions are handled manually and inconsistently.
- Several write actions do not verify resource ownership.
- CSRF protection is missing.
- Password reset design is weak.
- Sensitive configuration is stored directly in appsettings.
- Controllers contain too much business logic.

The highest priority should be fixing authorization and request security before adding new features.

## Current Architecture

## Application Shape

- Framework: ASP.NET Core MVC
- Target framework: net8.0
- Startup model: classic Program + Startup pattern
- Data access: Entity Framework Core with SQL Server
- Authentication in practice: session variables
- Authentication configured but largely unused: JWT bearer
- Views: Razor views under Views/
- Static uploads: wwwroot/uploads
- Email: SMTP through System.Net.Mail

## Main Components

- Entry point: Program.cs
- Service and middleware configuration: Startup.cs
- Controllers:
  - BlogController.cs
  - UserController.cs
  - AdminController.cs
  - HomeController.cs
- EF Core DbContext: Models/DotNetCourse.cs
- Core entities:
  - UserModel.cs
  - BlogModel.cs
  - CommentModel.cs
  - NotificationModel.cs containing AlertModel
  - RankingModel.cs

## Authentication and Authorization Model

The app stores these values in session after login:

- UserId
- Username
- Email
- UserRole

Authorization is then enforced manually in controllers with checks like:

- if session user is missing, redirect to login
- if session role is not Admin, redirect to login

JWT bearer authentication is configured in Startup.cs, but the application behavior is clearly session-based. That creates unnecessary complexity and a false sense of security because the protected flows do not rely on claims or authorization policies.

## Data Layer

The DbContext is implemented in Models/DotNetCourse.cs under the WeblogApplication.Data namespace. Functionally this works, but the file name and location are misleading. The project uses EF Core migrations successfully and the database model is straightforward.

## Local Runtime Notes

For Linux development, LocalDB was replaced with SQL Server 2022 running in Podman.

Current local assumptions:

- SQL Server container listens on localhost:1433
- App runs on http://localhost:5155
- Connection string in appsettings.json points to the local SQL Server container

This works for development, but the connection string currently includes SA credentials directly in source code and should not remain that way.

## What Is Working Well

- Passwords are hashed with BCrypt instead of being stored in plaintext.
- EF Core migrations build the schema correctly.
- Session checks are present on many write endpoints.
- Razor rendering mostly relies on normal encoded output.
- The blog page includes an explicit JavaScript HTML escaping helper before injecting comment text.
- Images are compressed before being written to disk.
- The project structure is small enough to refactor without major architectural risk.

## Verified Findings

## Critical

### 1. Privilege escalation during signup

The signup POST action accepts a full UserModel from the client and trusts the submitted Role value.

Evidence:

- Views/User/Signup.cshtml shows a hidden Role input for normal signup.
- The same view exposes a selectable Role dropdown for admins.
- Controllers/UserController.cs accepts the posted model directly.

Why this matters:

- A client can tamper with the posted form and submit Role=Admin.
- The server does not force non-admin users to Blogger.
- That means admin account creation is effectively client-controlled.

Impact:

- Full privilege escalation.

## Critical

### 2. Blog edit does not verify ownership

BlogController.Edit checks only whether a user is logged in. It loads the blog by id and updates it without checking that the blog belongs to the current user.

Impact:

- Any authenticated user can edit any blog post.

## Critical

### 3. Blog delete does not verify ownership

BlogController.Delete has the same problem as Edit. It verifies authentication but not ownership.

Impact:

- Any authenticated user can delete any blog post.

## Critical

### 4. Comment edit does not verify ownership

BlogController.EditComment checks that the user is logged in, but it does not verify that the comment belongs to that user.

Impact:

- Any authenticated user can modify any comment.

## High

### 5. Comment delete authorization is incomplete

BlogController.DeleteComment removes the comment itself based on id, but it only filters related vote records by current username. It does not verify ownership of the comment before deletion.

Impact:

- Any authenticated user can delete comments they do not own.

## High

### 6. Missing CSRF protection across POST actions

I did not find anti-forgery tokens in forms or validation attributes on POST actions.

Examples of POST forms:

- Views/User/Login.cshtml
- Views/User/Signup.cshtml
- Views/User/EditProfile.cshtml
- Views/Shared/_Layout.cshtml delete profile form
- Views/User/PasswordEmailForm.cshtml
- Views/User/PasswordResetForm.cshtml
- AJAX POSTs from Views/Blog/ManageBlogs.cshtml and Views/Blog/Index.cshtml

Why this matters:

- A third-party site can potentially trigger authenticated actions on behalf of a logged-in user.

Impact:

- Forced blog deletion, profile changes, password reset submissions, comment actions, and other state changes.

## High

### 7. Password reset token has no expiration and enables user enumeration

The password reset flow generates a Guid token and stores it directly on the user record. There is no expiration timestamp, no single-purpose token store, and the forgot password endpoint reveals when an email is not registered.

Impact:

- A leaked token can remain valid indefinitely until used.
- Attackers can enumerate valid accounts by observing the response behavior.

## High

### 8. Sensitive database credentials are stored in source

The local SQL Server connection string in appsettings.json includes:

- localhost,1433
- User Id=sa
- Password=BlogApp@Strong123

Impact:

- Credentials are exposed to anyone with repository access.
- This is acceptable only as a temporary local development workaround, not as a committed configuration pattern.

## Medium

### 9. Session-based auth and JWT config are inconsistent

Startup.cs configures JWT bearer authentication, but the controllers rely on session values instead of claims-based authorization.

Impact:

- Security rules are duplicated manually.
- It is easy to forget a check on one endpoint.
- The code suggests stronger protection than is actually present.

## Medium

### 10. DeleteProfile likely fails for users with comments

The data model configures CommentModel -> UserModel with DeleteBehavior.Restrict. At the same time, DeleteProfile removes the user directly.

Impact:

- User deletion can fail once comments exist.
- The user-facing behavior becomes inconsistent and error-prone.

## Medium

### 11. Vote ownership is keyed by username instead of immutable user id

RankingModel stores User as a string, and the ranking logic compares it to the session username. Users can also change their username in EditProfile.

Impact:

- Vote ownership can break after a username change.
- Historical vote relationships are fragile.

## Medium

### 12. GET endpoint mutates state

GetUnreadAlertForUser can mark notifications as read during a GET request.

Impact:

- Side effects on GET make the endpoint harder to reason about.
- This is bad HTTP semantics and can create subtle bugs.

## Medium

### 13. Controllers are doing too much work

Business logic, authorization, image handling, ranking logic, and data access all live in controllers.

Impact:

- Harder to test.
- Harder to secure consistently.
- Harder to refactor without regressions.

## Medium

### 14. Query efficiency is weak in list and dashboard pages

BlogController.Index and AdminController.Index calculate multiple per-row counts and sums. BlogController.Index also uses OrderBy(Guid.NewGuid()) for random ordering.

Impact:

- Poor scaling as blog and comment counts grow.
- Unnecessary database load.

## Low

### 15. Naming and organization issues increase maintenance cost

Examples:

- DbContext is in Models/DotNetCourse.cs
- NotificationModel.cs actually contains AlertModel
- SignalR is configured but no hub mapping is present
- Identity-related packages and usings exist without a real Identity-based auth model
- MailKit is referenced, but the code uses System.Net.Mail instead

Impact:

- More confusion for future maintainers.

## Low

### 16. Nullable warnings indicate incomplete model design

The project compiles with many nullable warnings because many non-nullable properties are not initialized.

Impact:

- Higher risk of null-related runtime bugs.
- Lower code clarity.

## Additional Code Health Observations

- The admin dashboard view also uses a client-side JavaScript redirect if the user is not admin. That is not security and should never be relied on.
- Blog creation removes model state entries manually, which is a sign that view models are needed instead of binding entity models directly.
- Image upload currently assumes a valid uploaded image. There is no strong server-side validation of file type or extension.
- There is no dedicated service layer for email, blog operations, rankings, or notifications.
- There is no test project in the workspace.

## Testing Gaps

No automated tests were found.

The most important missing tests are:

- signup must not allow non-admin users to create admin accounts
- blog edit must reject non-owners
- blog delete must reject non-owners
- comment edit/delete must reject non-owners
- password reset token validation and expiry behavior
- profile delete behavior when the user has comments and blogs
- admin dashboard access control

## Recommended Priority Plan

## Immediate

- Force signup role on the server for public registration.
- Add ownership checks to blog edit/delete.
- Add ownership checks to comment edit/delete.
- Add anti-forgery protection to all state-changing endpoints.
- Move database credentials and SMTP secrets out of appsettings.json.
- Add expiry and better validation for password reset tokens.
- Stop revealing whether an email address exists during forgot password.

## Short Term

- Replace entity binding in forms with dedicated request/view models.
- Move blog, ranking, notification, and email logic into services.
- Standardize authorization with ASP.NET Core authorization attributes and policies.
- Replace ranking ownership by username with user id.
- Fix DeleteProfile to either cascade safely through a designed workflow or reject deletion with a clear message.

## Medium Term

- Remove unused JWT or fully adopt claims-based authentication.
- Reorganize the data layer so DbContext and persistence types live in a clear location.
- Optimize expensive queries in BlogController and AdminController.
- Introduce automated tests for critical flows.

## Suggested Refactoring Direction

If the project continues to grow, the best next architecture step is:

- Controllers handle HTTP only.
- Services handle business rules.
- EF entities remain persistence models.
- View models and request models are used for UI binding.
- Authorization moves to policies and attributes, not repeated string checks.

That would make the code much easier to secure and maintain.

## Final Assessment

This is a workable coursework/demo application with a clear feature set, but it has several real security and design issues that should be fixed before treating it as a serious deployed application.

If this project is going to be extended, the correct order is:

1. Fix authorization bugs.
2. Add CSRF protection and secret management.
3. Stabilize password reset and account management.
4. Refactor controllers into service-backed flows.
5. Add tests before making larger feature changes.