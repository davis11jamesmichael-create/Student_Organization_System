# Product Overview — Student Organization System

## Purpose
A web-based management system for a student organization, built with ASP.NET Core MVC (.NET 8) and MySQL. It centralizes member records, activity scheduling, attendance tracking, participation monitoring, membership standing, announcements, and reporting behind a role-gated interface.

## Value Proposition
- Replaces manual spreadsheets with a structured, auditable database
- Enforces data integrity at the database level (unique indexes, FK constraints)
- Provides role-based access so Officers can operate the system without touching admin controls
- Gives members a self-service portal to view their own history

## Key Features

### Member Management
- CRUD for member records with Student ID format validation (`YYYY-NNNNN`)
- Activate / deactivate members (soft delete)
- Admin-only create, edit, activate/deactivate; Officers can read

### Activity Management
- Create and manage organization activities with schedule, instructor, and status fields
- Activity statuses: Upcoming, Ongoing, Completed, Cancelled
- Admin-only create/edit; Officers can read

### Attendance Management
- Record per-activity attendance for all active members
- Tri-state status: Present / Absent / Excused
- Prevents duplicate attendance records via unique index
- Attendance for cancelled activities is blocked

### Participation Monitoring
- Track member roles/contributions per activity (free-text type with datalist suggestions)
- Upsert pattern: blank submission clears the row
- Unique index on (ActivityID, MemberID)

### Membership Management
- One membership row per member (1-to-1 unique index)
- Standing: Active / Suspended (Inactive mirrors Member.IsActive)
- History visible on Member Details page

### Announcements
- Create and publish announcements with audience targeting
- Linked to the creating user (author)

### Reports
- Per-activity attendance summary
- Member activity reports

### Member Self-Service Portal
- Members log in and view their own attendance and participation history
- Portal shows "Not Linked" state when no Member row is associated

### User & Account Management
- Admin manages user accounts (create, edit, activate/deactivate)
- Role-based: Admin, Officer, Member
- Security stamp invalidates old cookies on role change or deactivation
- MustChangePassword flag forces password change on first login
- Login attempt throttling (brute-force guard via IMemoryCache)
- Avatar/profile picture upload

### Search
- Global search across members and activities

### Dashboard
- Stat cards: total members, active members, total activities, recent attendance
- Quick-access cards to main modules

## Target Users
| Role | Capabilities |
|------|-------------|
| Admin | Full access: all CRUD, user management, reports, announcements |
| Officer | Read members/activities, record attendance/participation, view reports |
| Member | Self-service portal: own attendance and participation history |

## Use Cases
- Student organization officers tracking meeting and event attendance
- Administrators managing membership standing and generating reports
- Members checking their own participation records
