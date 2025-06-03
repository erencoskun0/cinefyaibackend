# CinefyAI Backend API Dokümantasyonu - Frontend Rehberi

Bu dokümantasyon, CinefyAI sinema rezervasyon sistemi backend API'sinin Next.js frontend ile entegrasyonu için hazırlanmıştır.

## 📋 Genel Bilgiler

### Base URL

- **Development:** `https://localhost:7243`
- **API Base Path:** `/api`

### Teknoloji Stack

- **Backend:** ASP.NET Core 9.0
- **Database:** SQL Server
- **Authentication:** JWT Bearer Token
- **File Upload:** Multipart/Form-Data

---

## 🔐 Authentication Sistemi

### JWT Token Yapısı

- **Token Type:** Bearer Token
- **Expiration:** 120 dakika (development: 60 dakika)
- **Refresh Token:** 7 gün geçerli
- **Header Format:** `Authorization: Bearer {token}`

### User Roles

```typescript
// Role object from API
interface Role {
  id: string; // GUID
  name: string; // "User" | "CinemaOwner"
}

// Available roles (Admin excluded)
const AVAILABLE_ROLES = {
  User: "User",
  CinemaOwner: "CinemaOwner",
} as const;
```

---

## 🎯 API Endpoints

### Authentication Endpoints

#### 0. Get Available Roles

```typescript
GET / api / auth / roles;

// Response
interface RoleInfo {
  id: string; // GUID
  name: string; // "User" | "CinemaOwner"
}
[][
  // Example Response:
  ({ id: "550e8400-e29b-41d4-a716-446655440000", name: "User" },
  { id: "6ba7b810-9dad-11d1-80b4-00c04fd430c8", name: "CinemaOwner" })
];
```

#### 1. User Registration

```typescript
POST / api / auth / register;

// Request Body
interface RegisterRequest {
  email: string; // email@example.com
  password: string; // min 6 karakter
  confirmPassword: string; // password ile aynı
  fullName: string; // max 100 karakter
  roleId: string; // Role GUID (Admin role excluded)
}

// Response
interface AuthResponse {
  token: string;
  refreshToken: string;
  expiresAt: string; // ISO date
  user: {
    id: string; // GUID
    email: string;
    fullName: string;
    roles: string[]; // ["User"] veya ["CinemaOwner"]
  };
}
```

#### 2. User Login

```typescript
POST / api / auth / login;

// Request Body
interface LoginRequest {
  email: string;
  password: string;
}

// Response: AuthResponse (yukarıdaki gibi)
```

#### 3. Get Current User

```typescript
GET /api/auth/me
Headers: Authorization: Bearer {token}

// Response
interface UserInfo {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
}
```

#### 4. Refresh Token

```typescript
POST / api / auth / refresh - token;

// Request Body
interface RefreshTokenRequest {
  token: string; // Eski token
  refreshToken: string; // Refresh token
}

// Response: AuthResponse
```

#### 5. Logout

```typescript
POST /api/auth/logout
Headers: Authorization: Bearer {token}

// Response
{ message: "Başarıyla çıkış yapıldı" }
```

---

### Movies Endpoints

#### 1. Get Movies List

```typescript
GET /api/movies?page=1&pageSize=10&genre=Aksiyon&search=avengers

// Query Parameters
interface MovieListParams {
  page?: number;      // default: 1
  pageSize?: number;  // default: 10, max: 50
  genre?: string;     // optional filter
  search?: string;    // title, description, director, cast search
}

// Response
interface MovieListResponse {
  data: MovieListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

interface MovieListItem {
  id: string;              // GUID
  title: string;
  genre: string;
  duration: number;        // dakika
  description: string;
  releaseDate?: string;    // ISO date, nullable
  director: string;
  cast: string;           // virgül ile ayrılmış
  language: string;
  country: string;
  ageRestriction: string; // "13+", "18+" vs.
  trailerUrl: string;
  imageUrl: string;       // "/uploads/movies/filename.jpg"
  aiTags: string;
  isActive: boolean;
  createdAt: string;      // ISO date
}
```

#### 2. Get Movie Detail

```typescript
GET / api / movies / { id };

// Response
interface MovieDetail extends MovieListItem {
  shows: ShowInfo[]; // Sadece gelecek seanslar
  reviews: ReviewInfo[];
}

interface ShowInfo {
  id: string;
  showDate: string; // "2024-06-15" format
  startTime: string; // "14:30" format
  endTime: string; // "16:30" format
  hallName: string;
  cinemaName: string;
}

interface ReviewInfo {
  id: number;
  userName: string;
  rating: number; // 1-5
  comment: string;
  createdAt: string; // ISO date
}
```

#### 3. Create Movie

```typescript
POST /api/movies
Headers: Authorization: Bearer {token}
Content-Type: multipart/form-data

// Form Data
interface CreateMovieForm {
  title: string;          // required, max 200
  genre: string;          // max 100
  duration: number;       // required, 1-600 dakika
  description: string;    // max 2000
  releaseDate?: Date;     // optional
  director: string;       // max 100
  cast: string;          // max 500
  language: string;      // max 50
  country: string;       // max 50
  ageRestriction: string; // max 10
  trailerUrl: string;    // max 500
  aiTags: string;        // max 500
  image?: File;          // optional, JPG/PNG/GIF/WEBP, max 5MB
}

// Response: MovieListItem
```

#### 4. Update Movie

```typescript
PUT /api/movies/{id}
Headers: Authorization: Bearer {token}
Content-Type: multipart/form-data

// Form Data - Tüm alanlar optional
interface UpdateMovieForm {
  title?: string;
  genre?: string;
  duration?: number;
  description?: string;
  releaseDate?: Date;
  director?: string;
  cast?: string;
  language?: string;
  country?: string;
  ageRestriction?: string;
  trailerUrl?: string;
  aiTags?: string;
  isActive?: boolean;
  image?: File;          // Yeni resim yüklemek için
}

// Response: MovieListItem
```

#### 5. Delete Movie

```typescript
DELETE /api/movies/{id}
Headers: Authorization: Bearer {token}
// Sadece Admin rolü

// Response
{ message: "Film başarıyla silindi" }
```

---

## 🎨 Frontend Implementation Örnekleri

### 1. Authentication Hook (Next.js)

```typescript
// hooks/useAuth.ts
import { useState, useEffect, createContext, useContext } from "react";

interface AuthContextType {
  user: UserInfo | null;
  token: string | null;
  roles: RoleInfo[];
  login: (email: string, password: string) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
  isLoading: boolean;
  fetchRoles: () => Promise<RoleInfo[]>;
}

export const AuthContext = createContext<AuthContextType | null>(null);

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}

// API client example
const apiClient = {
  async get(endpoint: string, token?: string) {
    const headers: HeadersInit = {};

    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    const response = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}${endpoint}`,
      {
        method: "GET",
        headers,
      }
    );

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return response.json();
  },

  async post(endpoint: string, data: any, token?: string) {
    const headers: HeadersInit = {
      "Content-Type": "application/json",
    };

    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    const response = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}${endpoint}`,
      {
        method: "POST",
        headers,
        body: JSON.stringify(data),
      }
    );

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return response.json();
  },
};

// Auth service example
export class AuthService {
  async getRoles(): Promise<RoleInfo[]> {
    return apiClient.get("/api/auth/roles");
  }

  async register(data: RegisterRequest): Promise<AuthResponse> {
    return apiClient.post("/api/auth/register", data);
  }

  async login(email: string, password: string): Promise<AuthResponse> {
    return apiClient.post("/api/auth/login", { email, password });
  }
}
```

### 2. Movie Service

```typescript
// services/movieService.ts
export class MovieService {
  private baseUrl = process.env.NEXT_PUBLIC_API_URL + "/api/movies";

  async getMovies(params: MovieListParams = {}): Promise<MovieListResponse> {
    const searchParams = new URLSearchParams();

    if (params.page) searchParams.set("page", params.page.toString());
    if (params.pageSize)
      searchParams.set("pageSize", params.pageSize.toString());
    if (params.genre) searchParams.set("genre", params.genre);
    if (params.search) searchParams.set("search", params.search);

    const response = await fetch(`${this.baseUrl}?${searchParams}`);
    return response.json();
  }

  async getMovie(id: string): Promise<MovieDetail> {
    const response = await fetch(`${this.baseUrl}/${id}`);
    return response.json();
  }

  async createMovie(
    data: CreateMovieForm,
    token: string
  ): Promise<MovieListItem> {
    const formData = new FormData();

    Object.entries(data).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        if (key === "image" && value instanceof File) {
          formData.append(key, value);
        } else {
          formData.append(key, value.toString());
        }
      }
    });

    const response = await fetch(this.baseUrl, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
      },
      body: formData,
    });

    return response.json();
  }
}
```

### 3. Image Display Component

```tsx
// components/MovieImage.tsx
interface MovieImageProps {
  imageUrl: string;
  title: string;
  className?: string;
}

export function MovieImage({ imageUrl, title, className }: MovieImageProps) {
  const getImageSrc = (url: string) => {
    if (!url) return "/placeholder-movie.jpg";

    // Backend'den gelen URL'ler "/uploads/movies/..." formatında
    if (url.startsWith("/uploads/")) {
      return `${process.env.NEXT_PUBLIC_API_URL}${url}`;
    }

    return url;
  };

  return (
    <img
      src={getImageSrc(imageUrl)}
      alt={title}
      className={className}
      onError={(e) => {
        e.currentTarget.src = "/placeholder-movie.jpg";
      }}
    />
  );
}
```

### 4. File Upload Component

```tsx
// components/MovieForm.tsx
import { useState } from "react";

export function MovieForm() {
  const [formData, setFormData] = useState<CreateMovieForm>({
    title: "",
    duration: 0,
    // ... other fields
  });
  const [imagePreview, setImagePreview] = useState<string>("");

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      // Validation
      const maxSize = 5 * 1024 * 1024; // 5MB
      const allowedTypes = [
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
        "image/webp",
      ];

      if (file.size > maxSize) {
        alert("Dosya boyutu 5MB'dan küçük olmalıdır");
        return;
      }

      if (!allowedTypes.includes(file.type)) {
        alert("Sadece JPG, PNG, GIF, WEBP formatları desteklenir");
        return;
      }

      setFormData((prev) => ({ ...prev, image: file }));

      // Preview
      const reader = new FileReader();
      reader.onload = (e) => setImagePreview(e.target?.result as string);
      reader.readAsDataURL(file);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const movieService = new MovieService();
      await movieService.createMovie(formData, token);
      // Success handling
    } catch (error) {
      // Error handling
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <input type="file" accept="image/*" onChange={handleImageChange} />
      {imagePreview && (
        <img src={imagePreview} alt="Preview" style={{ maxWidth: "200px" }} />
      )}
      {/* Other form fields */}
    </form>
  );
}
```

### 5. Register Form Component

```tsx
// components/auth/RegisterForm.tsx
import { useState, useEffect } from "react";

export function RegisterForm() {
  const [formData, setFormData] = useState<RegisterRequest>({
    email: "",
    password: "",
    confirmPassword: "",
    fullName: "",
    roleId: "",
  });
  const [roles, setRoles] = useState<RoleInfo[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    const fetchRoles = async () => {
      try {
        const authService = new AuthService();
        const availableRoles = await authService.getRoles();
        setRoles(availableRoles);

        // Default olarak "User" rolünü seç
        const userRole = availableRoles.find((r) => r.name === "User");
        if (userRole) {
          setFormData((prev) => ({ ...prev, roleId: userRole.id }));
        }
      } catch (error) {
        console.error("Roller yüklenirken hata:", error);
      }
    };

    fetchRoles();
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);

    try {
      const authService = new AuthService();
      const response = await authService.register(formData);

      // Success - token'ı local storage'a kaydet
      localStorage.setItem("token", response.token);
      localStorage.setItem("refreshToken", response.refreshToken);

      // Redirect to dashboard
      router.push("/dashboard");
    } catch (error) {
      console.error("Kayıt hatası:", error);
      toast.error("Kayıt sırasında bir hata oluştu");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label htmlFor="fullName">Ad Soyad</label>
        <input
          type="text"
          id="fullName"
          value={formData.fullName}
          onChange={(e) =>
            setFormData((prev) => ({ ...prev, fullName: e.target.value }))
          }
          required
          maxLength={100}
        />
      </div>

      <div>
        <label htmlFor="email">Email</label>
        <input
          type="email"
          id="email"
          value={formData.email}
          onChange={(e) =>
            setFormData((prev) => ({ ...prev, email: e.target.value }))
          }
          required
        />
      </div>

      <div>
        <label htmlFor="password">Şifre</label>
        <input
          type="password"
          id="password"
          value={formData.password}
          onChange={(e) =>
            setFormData((prev) => ({ ...prev, password: e.target.value }))
          }
          required
          minLength={6}
        />
      </div>

      <div>
        <label htmlFor="confirmPassword">Şifre Tekrar</label>
        <input
          type="password"
          id="confirmPassword"
          value={formData.confirmPassword}
          onChange={(e) =>
            setFormData((prev) => ({
              ...prev,
              confirmPassword: e.target.value,
            }))
          }
          required
        />
      </div>

      <div>
        <label htmlFor="roleId">Kullanıcı Tipi</label>
        <select
          id="roleId"
          value={formData.roleId}
          onChange={(e) =>
            setFormData((prev) => ({ ...prev, roleId: e.target.value }))
          }
          required>
          <option value="">Seçiniz</option>
          {roles.map((role) => (
            <option key={role.id} value={role.id}>
              {role.name === "User" ? "Film İzleyicisi" : "Sinema Sahibi"}
            </option>
          ))}
        </select>
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="w-full py-2 px-4 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
        {isLoading ? "Kayıt Yapılıyor..." : "Kayıt Ol"}
      </button>
    </form>
  );
}
```

---

## 🛡️ Error Handling

### Standard Error Response

```typescript
interface ApiError {
  message: string;
  status?: number;
}

// Common error status codes
// 400: Bad Request (validation errors)
// 401: Unauthorized (no token/invalid token)
// 403: Forbidden (insufficient permissions)
// 404: Not Found
// 500: Internal Server Error
```

### Error Handling Example

```typescript
const handleApiError = (error: any) => {
  if (error.status === 401) {
    // Token expired, redirect to login
    router.push("/login");
  } else if (error.status === 403) {
    // Insufficient permissions
    toast.error("Bu işlem için yetkiniz bulunmuyor");
  } else {
    // General error
    toast.error(error.message || "Bir hata oluştu");
  }
};
```

---

## 🎪 Environment Variables

```bash
# .env.local
NEXT_PUBLIC_API_URL=https://localhost:7243
```

---

## 📱 Recommended Frontend Structure

```
src/
├── components/
│   ├── auth/
│   │   ├── LoginForm.tsx
│   │   ├── RegisterForm.tsx
│   │   └── ProtectedRoute.tsx
│   ├── movies/
│   │   ├── MovieCard.tsx
│   │   ├── MovieDetail.tsx
│   │   ├── MovieForm.tsx
│   │   └── MovieList.tsx
│   └── common/
│       ├── Header.tsx
│       ├── Layout.tsx
│       └── LoadingSpinner.tsx
├── hooks/
│   ├── useAuth.ts
│   ├── useMovies.ts
│   └── useApi.ts
├── services/
│   ├── authService.ts
│   ├── movieService.ts
│   └── apiClient.ts
├── types/
│   ├── auth.ts
│   ├── movie.ts
│   └── api.ts
└── pages/
    ├── login.tsx
    ├── register.tsx
    ├── movies/
    │   ├── index.tsx
    │   ├── [id].tsx
    │   └── create.tsx
    └── dashboard/
        └── index.tsx
```

---

## 🎯 Key Points

1. **Authentication:** Tüm korumalı endpoint'ler için Bearer token gerekli
2. **File Upload:** Multipart/form-data kullanın, max 5MB
3. **Image URLs:** Backend'den gelen URL'ler relative, tam URL oluşturun
4. **Pagination:** Sayfa bazlı, totalPages hesaplayın
5. **Error Handling:** HTTP status kodlarına göre uygun mesajlar gösterin
6. **Role Management:** Admin/CinemaOwner/User yetki kontrolü yapın

Bu dokümantasyon, Next.js frontend'inizin backend ile sorunsuz entegrasyonu için gerekli tüm bilgileri içermektedir.
