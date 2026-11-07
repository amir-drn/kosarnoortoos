using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace Ghaforv3.Models
{
    // EF Code First: ساختار دیتابیس از روی همین کلاس‌ها ساخته می‌شود (بدون edmx).
    // اولین باری که برنامه به دیتابیس وصل شود، اگر دیتابیس وجود نداشته باشد خودکار ساخته و پر می‌شود.
    public class Ghaforv3Db : DbContext
    {
        static Ghaforv3Db()
        {
            Database.SetInitializer(new Ghaforv3Initializer());
        }

        public Ghaforv3Db() : base("name=Ghaforv3Db") { }

        public DbSet<Address> Addresses { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }

        private static IndexAnnotation Unique(string name)
        {
            return new IndexAnnotation(new IndexAttribute(name) { IsUnique = true });
        }

        protected override void OnModelCreating(DbModelBuilder mb)
        {
            // حذف آبشاری فقط جاهایی که صریحاً می‌خواهیم (پایین)؛ جلوی «multiple cascade paths» در SQL Server هم گرفته می‌شود
            mb.Conventions.Remove<OneToManyCascadeDeleteConvention>();

            mb.Entity<User>().ToTable("Users");
            mb.Entity<User>().Property(x => x.FullName).IsRequired().HasMaxLength(100);
            mb.Entity<User>().Property(x => x.Mobile).IsRequired().HasMaxLength(15).IsUnicode(false)
                .HasColumnAnnotation("Index", Unique("UQ_Users_Mobile"));
            mb.Entity<User>().Property(x => x.Email).HasMaxLength(150);
            mb.Entity<User>().Property(x => x.PasswordHash).IsRequired().HasMaxLength(200);
            // علاقه‌مندی‌ها: جدول واسط Wishlists (حذف کاربر/محصول، ردیف‌های آن را هم پاک می‌کند)
            mb.Entity<User>().HasMany(u => u.Products).WithMany(p => p.Users)
                .Map(m => { m.ToTable("Wishlists"); m.MapLeftKey("UserId"); m.MapRightKey("ProductId"); });

            mb.Entity<Address>().ToTable("Addresses");
            mb.Entity<Address>().Property(x => x.Receiver).IsRequired().HasMaxLength(100);
            mb.Entity<Address>().Property(x => x.Phone).IsRequired().HasMaxLength(15).IsUnicode(false);
            mb.Entity<Address>().Property(x => x.Province).IsRequired().HasMaxLength(50);
            mb.Entity<Address>().Property(x => x.City).IsRequired().HasMaxLength(50);
            mb.Entity<Address>().Property(x => x.PostalCode).HasMaxLength(10).IsUnicode(false);
            mb.Entity<Address>().Property(x => x.Details).IsRequired().HasMaxLength(400);
            mb.Entity<Address>().HasRequired(a => a.User).WithMany(u => u.Addresses).HasForeignKey(a => a.UserId).WillCascadeOnDelete(true);

            mb.Entity<Brand>().ToTable("Brands");
            mb.Entity<Brand>().Property(x => x.Title).IsRequired().HasMaxLength(100);
            mb.Entity<Brand>().Property(x => x.Slug).IsRequired().HasMaxLength(120).IsUnicode(false)
                .HasColumnAnnotation("Index", Unique("UQ_Brands_Slug"));

            mb.Entity<Category>().ToTable("Categories");
            mb.Entity<Category>().Property(x => x.Title).IsRequired().HasMaxLength(100);
            mb.Entity<Category>().Property(x => x.Slug).IsRequired().HasMaxLength(120).IsUnicode(false)
                .HasColumnAnnotation("Index", Unique("UQ_Categories_Slug"));
            mb.Entity<Category>().Property(x => x.Icon).HasMaxLength(200);
            mb.Entity<Category>().HasOptional(c => c.Category1).WithMany(c => c.Categories1).HasForeignKey(c => c.ParentId).WillCascadeOnDelete(false);

            mb.Entity<Product>().ToTable("Products");
            mb.Entity<Product>().Property(x => x.Title).IsRequired().HasMaxLength(200);
            mb.Entity<Product>().Property(x => x.Slug).IsRequired().HasMaxLength(220).IsUnicode(false)
                .HasColumnAnnotation("Index", Unique("UQ_Products_Slug"));
            mb.Entity<Product>().Property(x => x.Price).HasPrecision(18, 0);
            mb.Entity<Product>().Property(x => x.OldPrice).HasPrecision(18, 0);
            mb.Entity<Product>().Property(x => x.Unit).IsRequired().HasMaxLength(20);
            mb.Entity<Product>().Property(x => x.ImageUrl).HasMaxLength(300);
            mb.Entity<Product>().Property(x => x.CreatedAt).HasColumnType("datetime2");
            mb.Entity<Product>().Property(x => x.OfferEndsAt).HasColumnType("datetime2");
            mb.Entity<Product>().HasRequired(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).WillCascadeOnDelete(false);
            mb.Entity<Product>().HasOptional(p => p.Brand).WithMany(b => b.Products).HasForeignKey(p => p.BrandId).WillCascadeOnDelete(false);

            mb.Entity<ProductImage>().ToTable("ProductImages");
            mb.Entity<ProductImage>().Property(x => x.ImageUrl).IsRequired().HasMaxLength(300);
            mb.Entity<ProductImage>().HasRequired(i => i.Product).WithMany(p => p.ProductImages).HasForeignKey(i => i.ProductId).WillCascadeOnDelete(true);

            mb.Entity<Order>().ToTable("Orders");
            mb.Entity<Order>().Property(x => x.TotalAmount).HasPrecision(18, 0);
            mb.Entity<Order>().Property(x => x.CreatedAt).HasColumnType("datetime2");
            mb.Entity<Order>().HasRequired(o => o.User).WithMany(u => u.Orders).HasForeignKey(o => o.UserId).WillCascadeOnDelete(false);
            mb.Entity<Order>().HasRequired(o => o.Address).WithMany(a => a.Orders).HasForeignKey(o => o.AddressId).WillCascadeOnDelete(false);

            mb.Entity<OrderItem>().ToTable("OrderItems");
            mb.Entity<OrderItem>().Property(x => x.Title).IsRequired().HasMaxLength(200);
            mb.Entity<OrderItem>().Property(x => x.UnitPrice).HasPrecision(18, 0);
            mb.Entity<OrderItem>().HasRequired(i => i.Order).WithMany(o => o.OrderItems).HasForeignKey(i => i.OrderId).WillCascadeOnDelete(true);
            mb.Entity<OrderItem>().HasRequired(i => i.Product).WithMany(p => p.OrderItems).HasForeignKey(i => i.ProductId).WillCascadeOnDelete(false);

            mb.Entity<Payment>().ToTable("Payments");
            mb.Entity<Payment>().Property(x => x.Amount).HasPrecision(18, 0);
            mb.Entity<Payment>().Property(x => x.Gateway).HasMaxLength(50);
            mb.Entity<Payment>().Property(x => x.RefNumber).HasMaxLength(100)
                .HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_Payments_Ref")));
            mb.Entity<Payment>().Property(x => x.CreatedAt).HasColumnType("datetime2");
            mb.Entity<Payment>().HasRequired(p => p.Order).WithMany(o => o.Payments).HasForeignKey(p => p.OrderId).WillCascadeOnDelete(false);

            mb.Entity<Banner>().ToTable("Banners");
            mb.Entity<Banner>().Property(x => x.Tag).HasMaxLength(100);
            mb.Entity<Banner>().Property(x => x.Title).IsRequired().HasMaxLength(200);
            mb.Entity<Banner>().Property(x => x.Highlight).HasMaxLength(200);
            mb.Entity<Banner>().Property(x => x.ButtonText).HasMaxLength(50);
            mb.Entity<Banner>().Property(x => x.LinkUrl).HasMaxLength(300);
            mb.Entity<Banner>().Property(x => x.ImageUrl).HasMaxLength(300);

            mb.Entity<User>().Property(x => x.CreatedAt).HasColumnType("datetime2");
        }

        // تاریخ ثبت را اگر کد فراموش کرده باشد خودکار پر می‌کند (در دیتابیس قبلی مقدار پیش‌فرض SQL این کار را می‌کرد)
        public override int SaveChanges()
        {
            var now = DateTime.Now;
            foreach (var e in ChangeTracker.Entries().Where(x => x.State == EntityState.Added))
            {
                var user = e.Entity as User; if (user != null && user.CreatedAt == default(DateTime)) user.CreatedAt = now;
                var prod = e.Entity as Product;
                if (prod != null)
                {
                    if (prod.CreatedAt == default(DateTime)) prod.CreatedAt = now;
                    if (string.IsNullOrWhiteSpace(prod.Unit)) prod.Unit = "عدد";
                }
                var ord = e.Entity as Order; if (ord != null && ord.CreatedAt == default(DateTime)) ord.CreatedAt = now;
                var pay = e.Entity as Payment; if (pay != null && pay.CreatedAt == default(DateTime)) pay.CreatedAt = now;
            }
            return base.SaveChanges();
        }
    }

    // اگر دیتابیس نبود می‌سازد، چند محدودیت درستی داده اضافه می‌کند و نمونه‌داده می‌ریزد.
    // نکته: بعد از ساخت، اگر کلاس‌های مدل را عوض کردی باید Migrations فعال کنی
    // (Package Manager Console:  Enable-Migrations  سپس  Add-Migration Name  و  Update-Database).
    public class Ghaforv3Initializer : CreateDatabaseIfNotExists<Ghaforv3Db>
    {
        protected override void Seed(Ghaforv3Db db)
        {
            db.Database.ExecuteSqlCommand("ALTER TABLE dbo.Products ADD CONSTRAINT CK_Products_Stock CHECK (Stock >= 0)");
            db.Database.ExecuteSqlCommand("ALTER TABLE dbo.Products ADD CONSTRAINT CK_Products_Price CHECK (Price >= 0)");
            db.Database.ExecuteSqlCommand("ALTER TABLE dbo.OrderItems ADD CONSTRAINT CK_OrderItems_Qty CHECK (Quantity > 0)");
            db.Database.ExecuteSqlCommand("ALTER TABLE dbo.Orders ADD CONSTRAINT CK_Orders_Status CHECK (Status BETWEEN 0 AND 4)");
            db.Database.ExecuteSqlCommand("ALTER TABLE dbo.Users ADD CONSTRAINT CK_Users_Role CHECK (Role BETWEEN 0 AND 2)");
            db.Database.ExecuteSqlCommand("CREATE INDEX IX_Products_Category ON dbo.Products (CategoryId, IsActive)");
            db.Database.ExecuteSqlCommand("CREATE INDEX IX_Orders_User ON dbo.Orders (UserId, CreatedAt DESC)");

            // ----- نمونه‌داده (از پنل مدیریت می‌توانی پاک یا ویرایش کنی) -----
            var led = new Category { Title = "لامپ LED", Slug = "led", Icon = "💡", SortOrder = 1, IsActive = true };
            var sw = new Category { Title = "کلید و پریز", Slug = "switch-socket", Icon = "🔌", SortOrder = 2, IsActive = true };
            var cb = new Category { Title = "سیم و کابل", Slug = "cable", Icon = "🧵", SortOrder = 3, IsActive = true };
            var outd = new Category { Title = "چراغ محوطه", Slug = "outdoor", Icon = "🏮", SortOrder = 4, IsActive = true };
            db.Categories.AddRange(new[] { led, sw, cb, outd });

            var brand = new Brand { Title = "غفوریان", Slug = "ghaforian" };
            db.Brands.Add(brand);

            db.Banners.Add(new Banner { Tag = "حراج ویژه", Title = "روشنایی برای خانهٔ شما", Highlight = "با بهترین قیمت", ButtonText = "مشاهده محصولات", LinkUrl = "/shop", SortOrder = 1, IsActive = true });

            Func<Category, string, string, string, decimal, decimal?, int, string, bool, bool, Product> p =
                (cat, title, slug, desc, price, old, stock, unit, flash, daily) => new Product
                {
                    Category = cat, Brand = brand, Title = title, Slug = slug, Description = desc, Price = price, OldPrice = old,
                    Stock = stock, Unit = unit, IsActive = true, IsFlashSale = flash, IsDailyOffer = daily,
                    OfferEndsAt = daily ? (DateTime?)DateTime.Now.AddDays(3) : null, CreatedAt = DateTime.Now
                };
            db.Products.AddRange(new[]
            {
                p(led, "لامپ LED حبابی ۱۰ وات سرپیچ E27", "led-10w-e27", "لامپ کم‌مصرف با نور مهتابی و عمر بالا.", 85000, 98000, 120, "عدد", true, true),
                p(led, "پنل LED سقفی ۲۴ وات", "panel-24w", "پنل روکار با نور یکنواخت.", 320000, 360000, 40, "عدد", true, false),
                p(sw, "پریز ارت‌دار سفید", "socket-earth", "پریز استاندارد با ارت.", 65000, null, 200, "عدد", false, false),
                p(sw, "کلید تک‌پل سفید", "switch-single", "کلید یک‌پل استاندارد.", 48000, 55000, 150, "عدد", true, false),
                p(cb, "سیم افشان ۳×۲٫۵ (۱۰۰ متر)", "cable-3x2-5", "سیم افشان مسی نیم‌پیچ.", 4200000, null, 15, "حلقه", false, false),
                p(cb, "کابل شبکه Cat6 (۳۰۵ متر)", "cat6-305", "کابل شبکه با روکش PVC.", 5600000, 6100000, 8, "حلقه", true, false)
            });
            db.SaveChanges();
        }
    }
}
