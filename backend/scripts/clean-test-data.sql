-- Duman testinin bıraktığı kayıtları veritabanından tamamen temizler.
-- Mantıksal silme (soft delete) uygulanan kayıtlar API'de görünmez ama tabloda kalır;
-- geliştirme ortamında tabloları tertemiz tutmak için bu betik kullanılır.
--
-- Kullanım:
--   psql -h localhost -p 5434 -U gameflow -d gameflow -f scripts/clean-test-data.sql
--
-- DİKKAT: Yönetici hesabı ve sistem lider sohbet odası dışındaki
-- TÜM verileri siler. Üretim veritabanında çalıştırmayın.

BEGIN;

DELETE FROM "MessageReads";
DELETE FROM "MessageAttachments";
DELETE FROM "Messages";
DELETE FROM "ChatRooms" WHERE "Type" <> 2;          -- 2 = Lider Sohbeti (sistem odası)

DELETE FROM "TaskLabels";
DELETE FROM "TaskAttachments";
DELETE FROM "TaskChecklistItems";
DELETE FROM "TaskComments";
DELETE FROM "Tasks";
DELETE FROM "Labels";
DELETE FROM "Sprints";

DELETE FROM "MeetingAttendees";
DELETE FROM "Meetings";
DELETE FROM "CalendarEvents";
DELETE FROM "Announcements";
DELETE FROM "Notifications";
DELETE FROM "ActivityLogs";

DELETE FROM "ProjectMembers";
DELETE FROM "Projects";
DELETE FROM "TeamMembers";
DELETE FROM "Teams";

DELETE FROM "RefreshTokens"
WHERE "UserId" IN (SELECT "Id" FROM "Users" WHERE "Email" <> 'admin@gameflow.dev');

DELETE FROM "Users" WHERE "Email" <> 'admin@gameflow.dev';

COMMIT;

SELECT 'Kullanıcı'   AS tablo, count(*) AS kayit FROM "Users"
UNION ALL SELECT 'Takım',      count(*) FROM "Teams"
UNION ALL SELECT 'Proje',      count(*) FROM "Projects"
UNION ALL SELECT 'Görev',      count(*) FROM "Tasks"
UNION ALL SELECT 'Sohbet odası', count(*) FROM "ChatRooms";
