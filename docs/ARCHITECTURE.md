# معماری، دیتابیس و امنیت

## لایه‌ها

```
Azma.Web  ──►  Azma.Infrastructure  ──►  Azma.Application  ──►  Azma.Domain
(Blazor UI)    (EF Core, ZarinPal,       (Services, DTOs,        (Entities,
                Identity, Files, Excel)    Interfaces)             business rules)
```

- **Domain:** موجودیت‌ها و قوانین خالص، مثل محاسبه قیمت نهایی (`Money`)، وضعیت ثبت‌نام دوره (`Course.GetRegistrationState`) و تغییر وضعیت سفارش همراه با ثبت تاریخچه (`Order.ChangeStatus`).
- **Application:** سرویس‌های کاربردی: `CatalogService`، `CheckoutService`، `PaymentService`، و سرویس‌های پنل مدیریت. این لایه فقط به اینترفیس‌ها وابسته است (`IAppDbContextFactory`، `IPaymentGateway`، `IFileStorage`، `IAuditLogger`، `IExcelExporter`، `ISiteClock`).
- **Infrastructure:** پیاده‌سازی این اینترفیس‌ها با EF Core و SQL Server، API زرین‌پال، ذخیره فایل روی دیسک، ClosedXML و منطقه زمانی تهران.
- **Web:** صفحات عمومی به صورت **Static SSR** رندر می‌شوند (سریع و بدون اتصال دائمی). فقط پنل مدیریت **Interactive Server** است.

سرویس‌ها برای هر عملیات یک DbContext کوتاه‌عمر می‌سازند (Factory). این کار در Blazor Server لازم است، چون یک DbContext مشترک در کل Circuit باعث داده‌های کهنه و خطای هم‌زمانی می‌شود.

## جداول دیتابیس

| جدول | توضیح |
|---|---|
| `AspNetUsers` و جداول Identity | کاربران مدیر (`ApplicationUsers`) و نقش‌ها |
| `Courses` | دوره‌ها: قیمت، تخفیف، ظرفیت، بازه ثبت‌نام، وضعیت انتشار، نوع برگزاری |
| `CourseSchedules` | روز و ساعت‌های برگزاری هر دوره |
| `CourseImages` | گالری تصاویر دوره |
| `CourseRegistrationFields` | فیلدهای اختصاصی فرم ثبت‌نام هر دوره |
| `CourseCategories` / `ProductCategories` | دسته‌بندی‌ها (اسلاگ یکتا) |
| `Instructors` | مدرسین |
| `Products` / `ProductImages` | محصولات فیزیکی/دیجیتال، کد محصول یکتا، موجودی |
| `Orders` | سفارش: شماره سفارش یکتا، شناسه عمومی (GUID)، مشتری، مبلغ کل، وضعیت، کد پیگیری |
| `OrderItems` | اقلام سفارش، با نگهداری عنوان و قیمت در لحظه خرید |
| `OrderStatusHistories` | تاریخچه کامل تغییرات وضعیت سفارش (چه کسی، چه زمانی، به چه دلیل) |
| `Enrollments` | ثبت‌نام‌ها؛ مرتبط با دوره، سفارش و قلم سفارش؛ مقادیر فیلدهای اختصاصی به صورت JSON |
| `PaymentTransactions` | تمام تلاش‌های پرداخت: Authority (یکتا)، مبلغ تومان و ریال، RefID، کارت، کد و پیام درگاه |
| `SiteSettings` | تنظیمات سایت (یک ردیف با محدودیت `Id = 1`) |
| `FaqItems` / `FeatureItems` | پرسش‌های متداول و مزیت‌ها |
| `ContactMessages` | پیام‌های فرم تماس |
| `AdminAuditLogs` | گزارش عملیات مدیریتی |

**مبالغ:** همه قیمت‌ها به صورت عدد صحیح (`bigint`) و به **تومان** ذخیره می‌شوند. ضریب تبدیل به ریال فقط در `Money.TomanToRial` اعمال می‌شود (×۱۰ با `checked`). محدودیت‌های `CHECK` مقادیر منفی را رد می‌کنند.

**ظرفیت:** یک ثبت‌نام جا اشغال می‌کند اگر «قطعی» باشد، یا «در انتظار پرداخت» باشد و زمان رزروش (`ReservedUntil`، پیش‌فرض ۳۰ دقیقه) هنوز تمام نشده باشد. بررسی ظرفیت و ثبت سفارش داخل یک تراکنش `Serializable` انجام می‌شود تا دو نفر هم‌زمان آخرین جا را نگیرند.

**تاریخ‌ها:** زمان‌ها به UTC ذخیره و در منطقه زمانی تهران نمایش داده می‌شوند. ورود و نمایش تاریخ‌ها به صورت شمسی است.

## وضعیت‌ها

```
سفارش:     PendingPayment ──verify OK──► Paid
                 │  ▲                      
         verify  │  │ retry               
         failed  ▼  │                     
            PaymentFailed                 
   خطای شبکه ──► PaymentUnknown ──استعلام مجدد──► Paid / PaymentFailed
   مدیر (با دلیل) ──► Cancelled

تراکنش:    Pending ──► Verifying (قفل) ──► Succeeded | Failed | Cancelled | Unknown
           RequestFailed (درگاه Authority نداد)
```

## امنیت

- **احراز هویت:** ASP.NET Core Identity. فقط کاربران نقش `Admin` وارد پنل می‌شوند. قفل حساب بعد از ۵ تلاش ناموفق (۱۵ دقیقه). کوکی HttpOnly با انقضای ۸ ساعت. با غیرفعال‌سازی یک مدیر، Security Stamp عوض می‌شود و نشست‌های او باطل می‌شود.
- **کنترل دسترسی سمت سرور:** هر صفحه پنل و endpointهای خروجی Excel با Policy `Admin` محافظت می‌شوند.
- **CSRF:** همه فرم‌ها توکن Antiforgery دارند.
- **XSS:** Razor همه خروجی‌ها را Encode می‌کند. توضیحات به صورت متن ساده با `white-space: pre-line` نمایش داده می‌شوند، نه HTML. CSP سخت‌گیرانه (`script-src 'self'`) فعال است. تنها استثنا کد نماد اعتماد است که فقط مدیر وارد می‌کند.
- **SQL Injection:** همه کوئری‌ها با EF Core پارامتری اجرا می‌شوند.
- **محدودسازی درخواست:** درخواست‌های POST هر IP (پیش‌فرض ۲۰ در دقیقه) و آدرس بازگشت درگاه محدود شده‌اند. فرم تماس فیلد Honeypot و سقف ۵ پیام در ساعت برای هر IP دارد.
- **فایل‌های آپلودی:** فقط JPG، PNG و WebP، حداکثر ۳ مگابایت. نوع فایل با امضای محتوا (Magic Bytes) بررسی می‌شود، نام فایل تصادفی است و فایل‌ها بیرون از `wwwroot` و با `nosniff` سرو می‌شوند.
- **پرداخت:** مبلغ فقط در سرور محاسبه می‌شود، Verify سمت سرور الزامی است، قفل اتمیک جلوی پرداخت تکراری را می‌گیرد، Authority ایندکس یکتا دارد و مرچنت کد در تنظیمات محیط نگهداری می‌شود. جزئیات در [ZARINPAL.md](ZARINPAL.md).
- **خطاها:** در Production صفحه خطای عمومی با کد پیگیری نمایش داده می‌شود و جزئیات فقط در لاگ ثبت می‌شود.
- **لاگ و ممیزی:** عملیات مهم (پرداخت، لغو، تغییر قیمت، تنظیمات، کاربران) هم در لاگ برنامه و هم در جدول `AdminAuditLogs` ثبت می‌شوند.
- **هدرهای امنیتی:** `X-Frame-Options`، `X-Content-Type-Options`، `Referrer-Policy`، `Permissions-Policy`، و HSTS در Production.

## توسعه‌های آینده

- **سبد خرید چندقلمی:** ساختار `Order` و `OrderItems` از چند قلم پشتیبانی می‌کند. فقط رابط کاربری سبد خرید باید اضافه شود.
- **انبارداری و ارسال:** فیلدهای آدرس و کد پستی روی سفارش وجود دارد. ماژول ارسال (وضعیت ارسال و کد رهگیری پستی) می‌تواند جدول جداگانه‌ای مرتبط با `Orders` باشد.
- **محصولات دیجیتال:** برای تحویل خودکار، جدولی برای فایل یا لینک دانلود مرتبط با `Products` اضافه شود و پس از تأیید پرداخت در `PaymentService` فعال شود.
- **پیامک:** با یک پیاده‌سازی `ISmsSender`، پس از تأیید پرداخت (در `PaymentService.ApplySuccessAsync`) پیامک تأیید ارسال شود.
