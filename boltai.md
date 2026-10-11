Most urgent fixes:

1. Hardcoded admin password (admin123) in the database seeder — anyone familiar with the codebase can log in as admin on a fresh deployment
2. Session cookies don't rotate after role changes or deactivation — a demoted or deactivated user keeps their old privileges for up to 8 hours

Admin/role recommendations:

1. Add audit logging for all administrative actions (who changed what, when)
2. Validate that submitted Role IDs actually exist in the database before saving — right now an invalid role ID shows a misleading "username already taken" error
3. Add the self-deactivation guard to the Edit action (it's only on the dedicated Deactivate action currently)
4. Consider a "super admin" tier so one admin can't demote or deactivate peer admins
5. Auto-deactivate accounts that haven't logged in for a long period (relevant for graduated students)


Bugs found: Synchronous DB calls in an async method (IsRemovingLastAdmin), a two-step delete in Reject that can orphan a Member row, and DateTime.Now used everywhere instead of DateTime.UtcNow.