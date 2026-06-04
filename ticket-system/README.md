# 🎫 DestekHub — Teknik Destek Yönetim Sistemi

Tam kapsamlı, saf HTML/CSS/JavaScript ile geliştirilmiş Teknik Destek Ticket Sistemi.

---

## 🚀 Projeyi Çalıştırma

Projeyi çalıştırmak için herhangi bir kurulum gerekmez. Tarayıcıdan doğrudan açabilirsiniz:

```
index.html  →  Tarayıcıda aç
```

> **Not:** Bazı tarayıcılar `localStorage` için yerel dosyalarda kısıtlama uygulayabilir.
> En iyi deneyim için projeyi bir HTTP sunucusu üzerinden çalıştırın:
>
> ```bash
> python3 -m http.server 8080
> # Ardından: http://localhost:8080
> ```

---

## 🔑 Demo Hesaplar

| Rol | E-posta | Şifre |
|---|---|---|
| **Yönetici (Admin)** | admin@destek.com | Admin123! |
| **Destek Ekibi** | destek@destek.com | Support123! |
| **Kullanıcı** | kullanici@ornek.com | User123! |

---

## 📁 Dosya Yapısı

```
ticket-system/
├── index.html              # Landing page (giriş yönlendirmesi)
├── login.html              # Giriş sayfası
├── register.html           # Kayıt sayfası
├── user-dashboard.html     # Kullanıcı ana paneli
├── user-tickets.html       # Kullanıcı ticket listesi
├── new-ticket.html         # Yeni ticket oluşturma
├── ticket-detail.html      # Ticket detay & yorum sayfası
├── admin-dashboard.html    # Admin ana paneli (istatistikler)
├── admin-tickets.html      # Admin ticket yönetimi
├── admin-users.html        # Kullanıcı yönetimi (CRUD)
├── profile.html            # Profil düzenleme
├── css/
│   └── style.css           # Tüm global stiller
├── js/
│   └── app.js              # Veri katmanı, Auth, CRUD, Bileşenler
└── data/
    └── db.json             # Başlangıç verisi (localStorage'a kopyalanır)
```

---

## ✅ Karşılanan Teknik Gereksinimler

### 1. Veritabanı (JSON + localStorage)
- `DataStore` sınıfı ile tam CRUD işlemleri
- `localStorage` üzerinde JSON tabanlı kalıcı veri saklama
- `db.json` başlangıç verisi ile otomatik seed işlemi
- `DataStore.Users`, `DataStore.Tickets`, `DataStore.Categories` modülleri

### 2. Authentication & Authorization
- `Auth` sınıfı ile session tabanlı kimlik doğrulama
- **3 farklı rol:** Admin, Destek Ekibi (Support), Kullanıcı (User)
- `Auth.requireAuth()` ve `Auth.requireRole()` ile sayfa koruma
- Şifreli giriş, "Beni hatırla" özelliği (localStorage)
- Kayıt sırasında e-posta ve kullanıcı adı benzersizlik kontrolü

### 3. UI & Layout
- Her sayfada ortak Navbar ve Footer bileşenleri (`NavbarComponent`, `FooterComponent`)
- `ViewBag` benzeri `ViewModel` pattern — JS nesneleri ile veri taşıma
- Responsive tasarım (mobil uyumlu)
- Sidebar navigasyonu (admin sayfaları için)

### 4. Validasyonlar
- `Validator` sınıfı ile gerçek zamanlı form doğrulama
- `Required`, `MinLength`, `MaxLength`, `Email`, `Pattern`, `Match` kuralları
- Şifre güç göstergesi (register sayfası)
- Görsel hata mesajları ve başarı bildirimleri

### 5. Ticket Sistemi
- **Enum benzeri sabit değerler:** `TicketStatus`, `TicketPriority` nesneleri
- **Durum yönetimi:** Açık → İşlemde → Çözüldü → Kapandı
- **LINQ benzeri filtreleme:** `DataStore.Tickets.filter()` metodu
- Kategori, öncelik, durum, metin araması ile çok boyutlu filtreleme
- Yorum sistemi (müşteri ve destek ekibi)
- Talep atama (destek ekibine)
- CSV export

---

## 🎯 Özellikler

### Kullanıcı (User) Paneli
- Kişisel dashboard (istatistikler, son talepler)
- Yeni destek talebi oluşturma
- Kendi taleplerini listeleme ve filtreleme
- Talep detayını görüntüleme ve yorum ekleme
- Profil bilgilerini güncelleme, şifre değiştirme

### Destek Ekibi (Support) Paneli
- Tüm talepleri görme ve filtreleme
- Talep üstlenme (atama)
- Durum güncelleme
- Yanıt/yorum yazma
- İstatistik dashboard

### Yönetici (Admin) Paneli
- Tüm destek ekibi özelliklerine ek olarak:
- Kullanıcı CRUD (ekleme, düzenleme, silme, aktif/pasif)
- Gelişmiş istatistik grafikleri (donut chart, bar chart)
- CSV export
- Atanmamış talep takibi

---

## 🛠 Teknik Detaylar

| Özellik | Detay |
|---|---|
| **Dil** | Saf HTML5, CSS3, JavaScript (ES6+) |
| **Veri Saklama** | localStorage (JSON) |
| **Kimlik Doğrulama** | sessionStorage tabanlı session |
| **Font** | Inter (Google Fonts) |
| **Bağımlılık** | Sıfır — hiçbir harici kütüphane yok |
| **Tarayıcı Desteği** | Chrome, Firefox, Safari, Edge |

---

## 📝 Notlar

- Veriler `localStorage`'da saklanır; tarayıcı geçmişini temizlerseniz veriler sıfırlanır.
- İlk açılışta `db.json` içindeki örnek veriler otomatik yüklenir.
- Tüm şifreler düz metin olarak saklanır (gerçek projede hash kullanılmalıdır).

---

## .NET Entegrasyonu

Projeye bozucu bir geçiş yapmadan ASP.NET Core tabanlı bir API eklendi.

- Backend proje dosyası: `../backend/TicketSystem.Api.csproj`
- Statik ön yüz dosyaları aynı yerinde kaldı
- API uçları: `/api/*`
- Şifreler backend tarafında PBKDF2 ile hashlenir ve istemciye düz metin olarak dönmez
- Çalıştırma: repo kökünden `dotnet run --project backend/TicketSystem.Api.csproj`
- `ticket-system/` klasörünün içindeyseniz aynı komut `dotnet run --project ../backend/TicketSystem.Api.csproj` olur

Uygulama bir web sunucusu üzerinden açılırsa `js/app.js` otomatik olarak API modunu dener.
Tarayıcıdan doğrudan dosya olarak açılırsa mevcut `localStorage` modu çalışmaya devam eder.
