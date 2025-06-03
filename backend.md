# CinefyAI - Backend Gereksinimleri ve Sistem Mimarisi

## 📋 Proje Genel Bakış

CinefyAI, modern bir sinema rezervasyon ve yönetim platformudur. AI destekli müşteri asistanı, gelişmiş analitik raporlar ve kullanıcı dostu arayüz sunar.

---

## 🏗️ Backend Mimarisi Gereksinimi

### Database Schema

#### 1. **Users Table**

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    firstName VARCHAR(100) NOT NULL,
    lastName VARCHAR(100) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    avatar TEXT,
    role ENUM('user', 'owner', 'admin') DEFAULT 'user',
    companyName VARCHAR(255),
    position VARCHAR(100),
    phone VARCHAR(20),
    address TEXT,
    city VARCHAR(100),
    country VARCHAR(100) DEFAULT 'Türkiye',
    isActive BOOLEAN DEFAULT true,
    emailVerified BOOLEAN DEFAULT false,
    lastLogin TIMESTAMP,
    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

#### 2. **Cinemas Table**

```sql
CREATE TABLE cinemas (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    brand VARCHAR(100) NOT NULL,
    address TEXT NOT NULL,
    city VARCHAR(100) NOT NULL,
    district VARCHAR(100) NOT NULL,
    phone VARCHAR(20),
    email VARCHAR(255),
    ownerId UUID REFERENCES users(id),
    description TEXT,
    facilities JSON, -- ["Otopark", "Restoran", "Wi-Fi", "Alışveriş"]
    features JSON, -- ["IMAX", "4DX", "VIP", "Dolby Atmos"]
    rating DECIMAL(2,1) DEFAULT 0,
    reviewCount INTEGER DEFAULT 0,
    capacity INTEGER DEFAULT 0,
    isActive BOOLEAN DEFAULT true,
    latitude DECIMAL(10, 8),
    longitude DECIMAL(11, 8),
    openingHours JSON,
    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

#### 3. **Movies Table**

```sql
CREATE TABLE movies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    poster TEXT,
    backdrop TEXT,
    trailer_url TEXT,
    genre JSON, -- ["Aksiyon", "Drama", "Bilim Kurgu"]
    duration INTEGER NOT NULL, -- dakika cinsinden
    rating DECIMAL(2,1) DEFAULT 0,
    releaseDate DATE NOT NULL,
    director VARCHAR(255),
    cast JSON, -- ["Actor 1", "Actor 2"]
    ageRating VARCHAR(10), -- "13+", "18+"
    isPopular BOOLEAN DEFAULT false,
    isNew BOOLEAN DEFAULT false,
    isActive BOOLEAN DEFAULT true,
    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

#### 4. **Halls Table**

```sql
CREATE TABLE halls (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cinemaId UUID REFERENCES cinemas(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    capacity INTEGER NOT NULL,
    seatLayout JSON, -- Koltuk düzeni
    features JSON, -- ["IMAX", "4DX", "VIP", "Premium Sound"]
    isActive BOOLEAN DEFAULT true,
    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

#### 5. **Sessions Table**

```sql
CREATE TABLE sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    movieId UUID REFERENCES movies(id) ON DELETE CASCADE,
    hallId UUID REFERENCES halls(id) ON DELETE CASCADE,
    cinemaId UUID REFERENCES cinemas(id) ON DELETE CASCADE,
    sessionDate DATE NOT NULL,
    sessionTime TIME NOT NULL,
    endTime TIME NOT NULL,
    standardPrice DECIMAL(10,2) NOT NULL,
    vipPrice DECIMAL(10,2) NOT NULL,
    availableSeats INTEGER NOT NULL,
    totalSeats INTEGER NOT NULL,
    occupancyStatus ENUM('Müsait', 'Dolmak Üzere', 'Az Koltuk') DEFAULT 'Müsait',
    isActive BOOLEAN DEFAULT true,
    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

#### 6. **Bookings Table**

```sql
CREATE TABLE bookings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sessionId UUID REFERENCES sessions(id) ON DELETE CASCADE,
    userId UUID REFERENCES users(id),
    customerName VARCHAR(255) NOT NULL,
    customerEmail VARCHAR(255) NOT NULL,
    customerPhone VARCHAR(20),
    selectedSeats JSON NOT NULL, -- [{"row": "A", "number": 1, "type": "Standard"}]
    totalAmount DECIMAL(10,2) NOT NULL,
    discountAmount DECIMAL(10,2) DEFAULT 0,
    finalAmount DECIMAL(10,2) NOT NULL,
    discountType VARCHAR(50), -- "Öğrenci", "65+ yaş", "Çarşamba"
    paymentStatus ENUM('pending', 'completed', 'failed', 'refunded') DEFAULT 'pending',
    paymentMethod VARCHAR(50),
    transactionId VARCHAR(255),
    bookingCode VARCHAR(20) UNIQUE NOT NULL,
    qrCode TEXT,
    status ENUM('confirmed', 'cancelled', 'completed') DEFAULT 'confirmed',
    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

#### 7. **Reviews Table**

```sql
CREATE TABLE reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cinemaId UUID REFERENCES cinemas(id) ON DELETE CASCADE,
    movieId UUID REFERENCES movies(id) ON DELETE CASCADE,
    userId UUID REFERENCES users(id),
    rating INTEGER CHECK (rating >= 1 AND rating <= 5),
    comment TEXT,
    isApproved BOOLEAN DEFAULT false,
    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

#### 8. **Analytics Table**

```sql
CREATE TABLE analytics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cinemaId UUID REFERENCES cinemas(id) ON DELETE CASCADE,
    sessionId UUID REFERENCES sessions(id) ON DELETE CASCADE,
    date DATE NOT NULL,
    ticketsSold INTEGER DEFAULT 0,
    revenue DECIMAL(10,2) DEFAULT 0,
    occupancyRate DECIMAL(5,2) DEFAULT 0,
    metrics JSON, -- Detaylı metrikler
    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### 9. **Chat_Messages Table** (AI Chatbot)

```sql
CREATE TABLE chat_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sessionId VARCHAR(255) NOT NULL,
    userId UUID REFERENCES users(id),
    message TEXT NOT NULL,
    response TEXT NOT NULL,
    intent VARCHAR(100), -- "film_onerisi", "yakın_sinemalar", "fiyat_bilgisi"
    confidence DECIMAL(3,2),
    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

---

## 🔌 API Endpoints

### **Authentication & User Management**

#### POST `/api/auth/register`

```typescript
// Request Body
interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  role?: "user" | "owner";
  companyName?: string;
}

// Response
interface AuthResponse {
  success: boolean;
  user: User;
  token: string;
  refreshToken: string;
}
```

#### POST `/api/auth/login`

```typescript
interface LoginRequest {
  email: string;
  password: string;
}
```

#### GET `/api/user/profile`

#### PUT `/api/user/profile`

#### POST `/api/auth/logout`

#### POST `/api/auth/refresh-token`

### **Movies API**

#### GET `/api/movies`

```typescript
interface MoviesResponse {
  movies: Movie[];
  pagination: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
  filters: {
    genres: string[];
    cities: string[];
    cinemas: string[];
  };
}

// Query Parameters
interface MoviesQuery {
  page?: number;
  limit?: number;
  genre?: string;
  city?: string;
  search?: string;
  sortBy?: "popularity" | "rating" | "title" | "releaseDate";
  sortOrder?: "asc" | "desc";
}
```

#### GET `/api/movies/:id`

#### POST `/api/movies` (Owner only)

#### PUT `/api/movies/:id` (Owner only)

#### DELETE `/api/movies/:id` (Owner only)

### **Cinemas API**

#### GET `/api/cinemas`

```typescript
interface CinemasResponse {
  cinemas: Cinema[];
  pagination: PaginationInfo;
  filters: {
    cities: string[];
    brands: string[];
    features: string[];
  };
}

interface CinemasQuery {
  page?: number;
  limit?: number;
  city?: string;
  brand?: string;
  features?: string[]; // ["IMAX", "4DX", "VIP"]
  search?: string;
  sortBy?: "distance" | "rating" | "name";
  userLat?: number;
  userLng?: number;
}
```

#### GET `/api/cinemas/:id`

#### GET `/api/cinemas/:id/movies`

#### GET `/api/cinemas/:id/sessions`

### **Sessions API**

#### GET `/api/sessions`

```typescript
interface SessionsQuery {
  cinemaId?: string;
  movieId?: string;
  date?: string; // YYYY-MM-DD
  hallType?: string; // "IMAX", "4DX", "VIP"
}

interface SessionsResponse {
  sessions: Session[];
  groupedByHall: {
    [hallType: string]: Session[];
  };
}
```

#### GET `/api/sessions/:id/seats`

```typescript
interface SeatsResponse {
  layout: SeatRow[];
  occupiedSeats: string[]; // ["A1", "A2", "B5"]
  prices: {
    standard: number;
    vip: number;
  };
}
```

### **Booking API**

#### POST `/api/bookings`

```typescript
interface BookingRequest {
  sessionId: string;
  selectedSeats: SeatSelection[];
  customerInfo: {
    name: string;
    email: string;
    phone: string;
  };
  discountType?: string;
  paymentMethod: string;
}

interface BookingResponse {
  booking: Booking;
  paymentUrl?: string;
  qrCode: string;
  bookingCode: string;
}
```

#### GET `/api/bookings/:id`

#### GET `/api/bookings/user/:userId`

#### PUT `/api/bookings/:id/cancel`

### **Dashboard APIs (Owner Only)**

#### GET `/api/dashboard/stats`

```typescript
interface DashboardStats {
  totalRevenue: number;
  totalTickets: number;
  averageOccupancy: number;
  activeCinemas: number;
  recentBookings: Booking[];
  topMovies: MovieStats[];
  revenueChart: ChartData[];
  occupancyChart: ChartData[];
}
```

#### GET `/api/dashboard/analytics`

```typescript
interface AnalyticsQuery {
  period: "today" | "week" | "month" | "year";
  cinemaId?: string;
  startDate?: string;
  endDate?: string;
}
```

#### GET `/api/dashboard/movies`

#### POST `/api/dashboard/movies`

#### GET `/api/dashboard/sessions`

#### POST `/api/dashboard/sessions`

#### GET `/api/dashboard/halls`

#### POST `/api/dashboard/halls`

### **AI Chatbot API**

#### POST `/api/chatbot/message`

```typescript
interface ChatRequest {
  message: string;
  sessionId: string;
  userId?: string;
  context?: {
    currentPage?: string;
    userLocation?: {
      lat: number;
      lng: number;
    };
  };
}

interface ChatResponse {
  response: string;
  intent: string;
  confidence: number;
  suggestions?: string[];
  data?: {
    movies?: Movie[];
    cinemas?: Cinema[];
    sessions?: Session[];
  };
}
```

#### GET `/api/chatbot/suggestions`

---

## 🎯 Frontend'de Mevcut Özellikler

### **🎬 Film Yönetimi**

- ✅ Film listesi görüntüleme (poster, rating, süre, tür)
- ✅ Film detay sayfaları
- ✅ Film arama ve filtreleme (tür, şehir, popülerlik)
- ✅ Film sıralama (popülerlik, rating, alfabetik)
- ✅ Responsive tasarım
- ✅ Film-sinema eşleştirme

### **🏢 Sinema Yönetimi**

- ✅ Sinema listesi (marka, şehir, mesafe, rating)
- ✅ Sinema detay sayfaları
- ✅ Sinema özellikleri (IMAX, 4DX, VIP, Dolby Atmos)
- ✅ Sinema olanakları (Otopark, Restoran, Wi-Fi)
- ✅ Mesafe hesaplama simülasyonu
- ✅ Sinema filtreleme ve arama

### **🎭 Seans Yönetimi**

- ✅ Salon türüne göre seans gruplandırma
- ✅ Seans saatleri ve fiyat gösterimi
- ✅ Doluluk durumu gösterimi
- ✅ Responsive seans kartları
- ✅ Direkt bilet alma linkleri

### **👤 Kullanıcı Yönetimi**

- ✅ User Context sistemi
- ✅ Login/Logout işlevselliği
- ✅ Kullanıcı profil dropdown'ı
- ✅ Role-based yetkilendirme (user, owner, admin)
- ✅ LocalStorage entegrasyonu

### **📊 Dashboard (Owner)**

- ✅ Ana dashboard sayfası
- ✅ Profil ayarları sayfası
- ✅ Film yönetimi sayfası
- ✅ Seans yönetimi
- ✅ Salon yönetimi
- ✅ Analytics ve raporlar
- ✅ Responsive dashboard layout

### **🤖 AI Chatbot**

- ✅ Gerçek zamanlı chat arayüzü
- ✅ Film önerisi sistemi
- ✅ Yakın sinema arama
- ✅ Fiyat bilgisi sorgulama
- ✅ Premium deneyim önerileri
- ✅ Typing indicator animasyonu
- ✅ Öneri butonları
- ✅ Glass morphism tasarım

### **🎨 UI/UX Özellikleri**

- ✅ Modern glassmorphism tasarım
- ✅ Smooth animasyonlar (Framer Motion)
- ✅ Responsive grid sistemler
- ✅ Gradient renk şemaları
- ✅ TypeScript tip güvenliği
- ✅ Component tabanlı mimari

---

## 🔧 Backend'den Gelmesi Gereken Veriler

### **Real-time Data Requirements**

#### 1. **Film Verileri**

- Film posteri ve backdrop görselleri (CDN URL'leri)
- Güncel rating ve yorum sayıları
- Dinamik popülerlik durumu
- Süreli kampanya bilgileri

#### 2. **Sinema Verileri**

- Gerçek konum koordinatları (Google Maps API)
- Güncel müsaitlik durumu
- Dinamik mesafe hesaplama
- Gerçek zamanlı olanaklar durumu

#### 3. **Seans Verileri**

- Gerçek zamanlı koltuk durumu
- Dinamik fiyatlandırma
- Occupancy rate hesaplama
- Session availability status

#### 4. **Rezervasyon Sistemi**

- Koltuk seçimi ve rezervasyon
- Ödeme entegrasyonu (İyzico, PayTR)
- QR kod oluşturma
- E-bilet gönderimi

#### 5. **Analytics Data**

- Gerçek zamanlı satış verileri
- Occupancy rate hesaplamaları
- Revenue tracking
- Customer behavior analytics

### **AI Chatbot Backend Requirements**

#### 1. **Natural Language Processing**

- Intent recognition
- Entity extraction
- Context awareness
- Multi-turn conversation

#### 2. **Recommendation Engine**

- User behavior analysis
- Content-based filtering
- Collaborative filtering
- Location-based recommendations

#### 3. **Real-time Data Integration**

- Live cinema availability
- Dynamic pricing information
- Session recommendations
- Personalized suggestions

---

## 🚀 Backend Technology Stack Önerileri

### **Core Technologies**

- **Runtime:** Node.js + TypeScript
- **Framework:** Express.js veya Fastify
- **Database:** PostgreSQL (Primary) + Redis (Cache)
- **ORM:** Prisma veya TypeORM
- **Authentication:** JWT + Refresh Tokens

### **Additional Services**

- **File Storage:** AWS S3 / CloudFlare R2
- **CDN:** CloudFlare
- **Email Service:** SendGrid / Amazon SES
- **SMS Service:** Twilio
- **Payment:** İyzico / PayTR
- **Maps:** Google Maps API
- **Push Notifications:** Firebase Cloud Messaging

### **AI/ML Services**

- **NLP:** OpenAI GPT-4 / Google Dialogflow
- **Recommendation:** Custom ML pipeline
- **Analytics:** Google Analytics + Custom tracking

### **DevOps & Monitoring**

- **Deployment:** Docker + Kubernetes
- **CI/CD:** GitHub Actions
- **Monitoring:** DataDog / New Relic
- **Logging:** Winston + ELK Stack
- **Error Tracking:** Sentry

---

## 📈 Performance & Scaling Considerations

### **Database Optimization**

- Connection pooling
- Query optimization
- Indexing strategies
- Read replicas for analytics

### **Caching Strategy**

- Redis for session data
- CDN for static assets
- API response caching
- Database query caching

### **Security Measures**

- Rate limiting
- Input validation
- SQL injection prevention
- XSS protection
- CORS configuration
- Helmet.js security headers

---

## 🎯 Phase-wise Implementation

### **Phase 1: Core Backend (4-6 weeks)**

- Authentication system
- Basic CRUD operations
- Database setup
- Core API endpoints

### **Phase 2: Advanced Features (3-4 weeks)**

- Booking system
- Payment integration
- Email/SMS notifications
- Basic analytics

### **Phase 3: AI & ML (4-5 weeks)**

- Chatbot implementation
- Recommendation engine
- Advanced analytics
- Personalization

### **Phase 4: Production Ready (2-3 weeks)**

- Performance optimization
- Security hardening
- Monitoring setup
- Deployment automation

---

## 📝 API Documentation

Bu proje için **Swagger/OpenAPI 3.0** dokümantasyonu oluşturulmalı ve tüm endpoint'ler detaylı olarak dokümante edilmelidir.

### **Development Tools**

- Postman Collection
- Insomnia Workspace
- API Testing Suite
- Mock Data Generators

---

**Son Güncelleme:** 2024-01-XX  
**Versiyon:** 1.0  
**Proje:** CinefyAI Cinema Management System
