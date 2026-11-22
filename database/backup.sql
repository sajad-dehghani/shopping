-- نسخه پشتیبان کامل از دیتابیس Azma
-- اجرا: sqlcmd -S <server> -E -i database/backup.sql   (یا از طریق SQL Server Agent به صورت زمان‌بندی‌شده)
-- مسیر پوشه پشتیبان را متناسب با سرور خود تغییر دهید؛ سرویس SQL Server باید اجازه نوشتن در آن را داشته باشد.
DECLARE @dir NVARCHAR(400) = N'D:\Backups\Azma\';
DECLARE @file NVARCHAR(500) = @dir + N'AzmaDb_' + FORMAT(SYSDATETIME(), 'yyyyMMdd_HHmm') + N'.bak';

BACKUP DATABASE [AzmaDb]
TO DISK = @file
WITH COMPRESSION, CHECKSUM, INIT, STATS = 10,
     NAME = N'AzmaDb full backup';

-- بررسی سلامت فایل پشتیبان
RESTORE VERIFYONLY FROM DISK = @file WITH CHECKSUM;
