/* ─────────────────────────────────────────────────────────────────────────────────────────
   ClaimStatusHistory_AppendOnly.sql

   What:  a trigger that makes the claim history table APPEND-ONLY. rows can be inserted, but any
          UPDATE or DELETE is refused by SQL Server itself.
   Why:   the audit trail must never be rewritten. in C#, ClaimStatusHistory uses `init` properties so
          code can't change a row after it's created. this trigger enforces the SAME rule at the
          database level, so it also holds for anything that bypasses the app (SSMS, a script, a bug).
          that's "defense in depth".
   How:   an INSTEAD OF trigger runs *instead of* the UPDATE/DELETE, and THROW cancels the statement
          with an error. INSERTs aren't listed, so they work normally.

   Run:   once, in SSMS, against the OneCover database (after `dotnet ef database update`).
          if you ever drop and recreate the database, run it again.
   Note:  AppDbContext declares this trigger with HasTrigger(...) so EF Core saves to this table in a
          trigger-safe way.
   ───────────────────────────────────────────────────────────────────────────────────────── */

USE OneCover;
GO

CREATE OR ALTER TRIGGER dbo.TR_ClaimStatusHistories_AppendOnly
ON dbo.ClaimStatusHistories
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- 50001 = a custom error number (user errors start at 50000). state 1 = a generic location marker
    THROW 50001, 'Claim status history is append-only: rows cannot be updated or deleted.', 1;
END;
GO

/* ── try it (optional): both of these should FAIL with the message above ──────────────────
   UPDATE dbo.ClaimStatusHistories SET Note = 'tampered' WHERE Id = 1;
   DELETE FROM dbo.ClaimStatusHistories WHERE Id = 1;
   ───────────────────────────────────────────────────────────────────────────────────────── */
