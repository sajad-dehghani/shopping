# راهنمای نصب و انتشار

## پیش‌نیازهای سرور

- **ویندوز:** IIS به همراه [ASP.NET Core Hosting Bundle 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- **لینوکس:** ASP.NET Core Runtime 10 به همراه Nginx به عنوان Reverse Proxy
- SQL Server نسخه 2019 یا بالاتر (Express هم کافی است)
- گواهی SSL برای دامنه (HTTPS برای پرداخت و ورود مدیر ضروری است)
- برای وب‌سوکت پنل مدیریت (Blazor Server)، **WebSocket** باید در IIS یا Nginx فعال باشد.

## ۱. ساخت خروجی

```bash
dotnet publish src/Azma.Web -c Release -o ./publish
```

## ۲. دیتابیس

دو روش برای ساخت دیتابیس وجود دارد:

- **خودکار (پیش‌فرض):** با `Database__AutoMigrate=true`، برنامه در هر اجرا Migrationهای جدید را اعمال می‌کند. کاربر دیتابیس باید دسترسی `db_owner` داشته باشد.
- **دستی:** فایل `database/AzmaDb.sql` را در SQL Server Management Studio اجرا کنید. این اسکریپت idempotent است و تکرار اجرای آن مشکلی ایجاد نمی‌کند. سپس `Database__AutoMigrate=false` قرار دهید و دسترسی کاربر برنامه را به `db_datareader` و `db_datawriter` محدود کنید.

اسکریپت Migration جدید پس از تغییر مدل‌ها با این دستور ساخته می‌شود:

```bash
dotnet ef migrations script --idempotent -p src/Azma.Infrastructure -s src/Azma.Web -o database/AzmaDb.sql
```

## ۳. تنظیمات محیط اجرا (متغیرهای محیطی)

هیچ اطلاعات محرمانه‌ای در `appsettings.json` قرار ندهید. این مقادیر را به صورت متغیر محیطی تنظیم کنید (`__` به جای `:`):

| متغیر | توضیح |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | رشته اتصال SQL Server |
| `ZarinPal__MerchantId` | مرچنت کد زرین‌پال ([راهنما](ZARINPAL.md)) |
| `Site__BaseUrl` | آدرس عمومی سایت، مثل `https://your-domain.ir` (برای آدرس بازگشت درگاه) |
| `AdminSeed__Email` / `AdminSeed__Password` | فقط برای اولین اجرا، جهت ساخت مدیر اول. پس از ورود، رمز را از پنل عوض کنید و این دو متغیر را حذف کنید. |
| `Storage__UploadsPath` | (اختیاری) مسیر ذخیره تصاویر عمومی (محصول، دوره، اسلایدر)؛ پیش‌فرض `uploads` کنار برنامه |
| `Storage__PrivatePath` | (اختیاری) مسیر ذخیره تصاویر رسید کارت به کارت (هرگز به‌صورت عمومی سرو نمی‌شود)؛ پیش‌فرض `private-uploads` کنار برنامه |
| `DataProtection__KeysPath` | (اختیاری) مسیر کلیدهای رمزنگاری کوکی‌ها؛ پیش‌فرض `keys` کنار برنامه |
| `ReverseProxy__KnownProxies__0` | (اختیاری) IP پراکسی، اگر روی سرور دیگری است |

پوشه‌های `uploads`، `private-uploads` و `keys` باید برای کاربر اجرای برنامه **قابل نوشتن** باشند و در **پشتیبان‌گیری** گنجانده شوند. با به‌روزرسانی برنامه نباید پاک شوند.

> مرچنت کد را در `appsettings.json` ننویسید؛ این فایل با هر انتشار جایگزین می‌شود و ممکن است در مخزن کد ثبت شود. آن را فقط در `web.config` (IIS) یا فایل secrets (لینوکس) قرار دهید.

## ۴-الف. انتشار روی IIS

1. محتوای پوشه `publish` را در مسیر سایت کپی کنید.
2. یک Application Pool با تنظیم **No Managed Code** بسازید.
3. در IIS، سرویس **WebSocket Protocol** را فعال کنید (Server Manager ← Web Server ← Application Development).
4. متغیرهای محیطی را در `web.config` اضافه کنید:

```xml
<aspNetCore processPath="dotnet" arguments=".\Azma.Web.dll" hostingModel="inprocess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
    <environmentVariable name="ConnectionStrings__DefaultConnection" value="Server=.;Database=AzmaDb;User Id=azma;Password=...;TrustServerCertificate=True" />
    <environmentVariable name="ZarinPal__MerchantId" value="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" />
    <environmentVariable name="Site__BaseUrl" value="https://your-domain.ir" />
  </environmentVariables>
</aspNetCore>
```

5. به کاربر Application Pool (`IIS AppPool\<نام>`) اجازه نوشتن روی پوشه‌های `uploads`، `private-uploads` و `keys` بدهید.
   در هاست‌های اشتراکی (Plesk و مشابه) معمولاً کافی است از پنل هاست، دسترسی Write این سه پوشه را برای کاربر سایت فعال کنید.
6. گواهی SSL را روی Binding پورت 443 تنظیم کنید.

## ۴-ب. انتشار روی لینوکس (systemd + Nginx)

فایل `/etc/systemd/system/azma.service`:

```ini
[Unit]
Description=Azma web
After=network.target

[Service]
WorkingDirectory=/var/www/azma
ExecStart=/usr/bin/dotnet /var/www/azma/Azma.Web.dll
Restart=always
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=Https__Redirect=false
EnvironmentFile=/etc/azma/secrets.env

[Install]
WantedBy=multi-user.target
```

اطلاعات محرمانه را در `/etc/azma/secrets.env` قرار دهید (دسترسی فایل `600`، مالک root):

```
ConnectionStrings__DefaultConnection=Server=...;Database=AzmaDb;User Id=azma;Password=...;TrustServerCertificate=True
ZarinPal__MerchantId=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Site__BaseUrl=https://your-domain.ir
```

بخش مربوط در پیکربندی Nginx (همراه با پشتیبانی WebSocket):

```nginx
server {
    listen 443 ssl http2;
    server_name your-domain.ir;
    # ssl_certificate ... ;
    client_max_body_size 10m;   # receipt images up to 6 MB

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection $http_connection;
        proxy_set_header   Host $host;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_read_timeout 300s;
    }
}
```

```bash
sudo systemctl enable --now azma
```

## ۵. پس از انتشار

1. حساب مدیر را بسازید. یکی از دو روش کافی است: متغیرهای `AdminSeed__*` در اولین اجرا، یا این دستور در پوشه انتشار (با همان متغیرهای محیطی و رشته اتصال برنامه):
   ```bash
   dotnet Azma.Web.dll admin you@your-domain.ir "YourStrong@Pass1"
   ```
   همین دستور برای بازنشانی رمز فراموش‌شده یا باز کردن قفل حساب هم کار می‌کند.
2. وارد `/admin` شوید. اگر از `AdminSeed__*` استفاده کرده‌اید، رمز را عوض کنید و این متغیرها را حذف کنید.
3. در «تنظیمات سایت»، اطلاعات مجموعه، لوگو و اطلاعات تماس را وارد کنید، **حالت آزمایشی** درگاه را خاموش کنید و یک خرید واقعی با مبلغ کم انجام دهید.
4. داده‌های نمونه در Production ساخته نمی‌شوند (`Database__SeedSampleData=false`).
5. آدرس `/health` برای مانیتورینگ در دسترس است.

## چک‌لیست پیش از راه‌اندازی

- [ ] `ASPNETCORE_ENVIRONMENT=Production` و رشته اتصال دیتابیس واقعی تنظیم شده است.
- [ ] `Site__BaseUrl` دقیقاً برابر آدرس HTTPS دامنه است (برای بازگشت از درگاه ضروری است).
- [ ] مرچنت کد در متغیر محیطی است و در «تنظیمات سایت» حالت آزمایشی (Sandbox) خاموش است.
- [ ] یک خرید واقعی با مبلغ کم انجام و کد رهگیری دریافت شد؛ یک بار هم مسیر کارت به کارت (ارسال رسید ← تأیید در پنل) آزمایش شد.
- [ ] شماره کارت، نام صاحب حساب و بانک برای کارت به کارت وارد شده است (بدون شماره کارت، این روش به مشتری نمایش داده نمی‌شود).
- [ ] اسلایدهای صفحه اصلی (تصویر افقی ۱۶۰۰×۷۰۰) در «اسلایدر صفحه اصلی» ثبت شده‌اند.
- [ ] بخش‌های دوره‌ها/فروشگاه مطابق نیاز در «تنظیمات سایت ← بخش‌های سایت» فعال یا غیرفعال شده‌اند.
- [ ] متن قوانین بررسی شد (در صورت خالی بودن، متن پیش‌فرض کامل نمایش داده می‌شود) و نماد اعتماد (اینماد) ثبت شده است.
- [ ] پوشه‌های `uploads`، `private-uploads` و `keys` قابل نوشتن هستند و پشتیبان‌گیری روزانه دیتابیس فعال است.
- [ ] WebSocket روی سرور فعال است (در غیر این صورت پنل مدیریت پیام «ارتباط موقتاً قطع شد» نشان می‌دهد).
- [ ] حساب مدیر با رمز قوی ساخته شده و متغیرهای `AdminSeed__*` حذف شده‌اند.

## ۶. پشتیبان‌گیری

- **دیتابیس:** اسکریپت `database/backup.sql` یک نسخه پشتیبان کامل و فشرده با تاریخ در نام فایل می‌سازد و سلامت آن را بررسی می‌کند. آن را با **SQL Server Agent** (یا Task Scheduler و `sqlcmd` در نسخه Express) روزانه اجرا کنید و نسخه‌ها را روی سرور یا فضای دیگری هم نگه دارید. برای پشتیبان‌گیری مداوم، Recovery Model را `FULL` کنید و پشتیبان Log هم بگیرید.
- **فایل‌ها:** پوشه‌های `uploads` (تصاویر)، `private-uploads` (رسیدهای پرداخت) و `keys` (کلیدهای کوکی) را هم در برنامه پشتیبان‌گیری بگذارید.
- **بازیابی را آزمایش کنید:** هر چند وقت یک بار، نسخه پشتیبان را روی یک دیتابیس آزمایشی بازیابی کنید.

## به‌روزرسانی نسخه

1. از دیتابیس و پوشه‌های `uploads`، `private-uploads` و `keys` پشتیبان بگیرید.
2. فایل‌های جدید را جایگزین کنید. پوشه‌های `uploads`، `private-uploads` و `keys` را حذف نکنید.
   در IIS، پیش از کپی یک فایل `app_offline.htm` در ریشه سایت قرار دهید تا فایل‌ها قفل نباشند و پس از کپی آن را حذف کنید.
3. برنامه را دوباره راه‌اندازی کنید. Migrationهای جدید به صورت خودکار یا با اسکریپت SQL اعمال می‌شوند.
