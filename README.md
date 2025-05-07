# AuctionBay Backend

**AuctionBay** is a robust, secure ASP.NET Core Web API backend powering a full-featured auction platform. It provides user authentication, auction management, real-time bidding, notifications, and image uploads — all exposed via clean RESTful endpoints.

---

## Table of Contents

1. **Features**  
2. **Tech Stack**  
3. **Getting Started**  
   - **Prerequisites**  
   - **Installation**  
   - **Configuration**  
   - **Running the App**  
4. **API Endpoints**  
   - **Authentication**  
   - **Auctions**  
   - **Profile & Notifications**  
   - **Image Upload**  
5. **Database & Migrations**  

---

## Features

- **User Authentication & Authorization**  
  - JWT-based login/register  
  - Password reset (token generation + email)  
- **Auction Management**  
  - Create, read, update, delete (CRUD) auctions  
  - Pagination and filtering of “active” auctions  
  - Detail views with bid history and bid-status per user  
- **Bidding Engine**  
  - Enforce minimum bid increments  
  - Notify out-bid users in real time  
- **User Profile**  
  - View & update personal info  
  - Change password flow  
  - View my auctions, bids, and wins  
- **Notifications**  
  - Create, list, and mark notifications as read  
- **Image Uploads**  
  - Secure multipart/form-data upload  
  - Magic-byte & content-type validation  
  - Auto-generated thumbnails  

---

## Tech Stack

- **Framework:** ASP.NET Core 7  
- **ORM:** Entity Framework Core  
- **Auth:** ASP.NET Core Identity + JWT  
- **Database:** SQL Server / PostgreSQL (configurable)  
- **Hosting:** IIS / Kestrel / Docker  
- **Notifications:** Custom EF-backed notification service  
- **Storage:** Local file system under `wwwroot/images`  
- **Logging:** `Microsoft.Extensions.Logging`  

---

## Getting Started

### Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/download)  
- SQL Server, PostgreSQL, or SQLite  
- (Optional) Docker & Docker Compose  

### Installation

1. **Clone the repo**  
   ```bash
   git clone https://github.com/your-org/auctionbay-backend.git
   cd auctionbay-backend
   ```

2. **Restore packages**  
   ```bash
   dotnet restore
   ```

3. **Apply migrations**  
   ```bash
   dotnet ef database update
   ```

### Configuration

1. **appsettings.json**  
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=.;Database=AuctionBay;Trusted_Connection=True;"
     },
     "Jwt": {
       "Key": "YOUR_SECRET_KEY_HERE",
       "Issuer": "AuctionBayAPI",
       "Audience": "AuctionBayClient",
       "ExpiresInMinutes": 60
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft": "Warning"
       }
     }
   }
   ```
2. **Environment Variables**  
   - `ASPNETCORE_ENVIRONMENT=Development|Production`  
   - Override `Jwt__Key`, `ConnectionStrings__DefaultConnection`, etc.

### Running the App

```bash
dotnet run --project auctionbay_backend/auctionbay_backend.csproj
```

By default, the API will listen on `https://localhost:5001` and `http://localhost:5000`.

---

## API Endpoints

### **Authentication**

| Method | Endpoint                  | Description                          |
| ------ | ------------------------- | ------------------------------------ |
| POST   | `/api/Auth/register`      | Register a new user                  |
| POST   | `/api/Auth/login`         | Login and receive JWT                |
| POST   | `/api/Auth/forgot-password` | Generate password reset token      |
| POST   | `/api/Auth/reset-password`  | Reset password with token          |

### **Auctions**

| Method | Endpoint                          | Description                                |
| ------ | --------------------------------- | ------------------------------------------ |
| GET    | `/api/Auctions?page=&pageSize=`   | List active auctions (paginated)           |
| GET    | `/api/Auctions/{id}`              | Get full auction detail (with user context)|
| POST   | `/api/Auctions`                   | Create a new auction (multipart/form-data) |
| POST   | `/api/Auctions/{id}/bid`          | Place a bid on an auction                  |

### **Profile & Notifications**

| Method | Endpoint                                  | Description                                       |
| ------ | ----------------------------------------- | ------------------------------------------------- |
| GET    | `/api/Profile/me`                         | Get current user profile                          |
| PUT    | `/api/Profile/me`                         | Update current user profile                       |
| PUT    | `/api/Profile/update-password`            | Change password                                   |
| GET    | `/api/Profile/notifications`              | List user notifications                           |
| PUT    | `/api/Profile/notifications/{id}/read`    | Mark single notification as read                  |
| PUT    | `/api/Profile/notifications/markAllRead`  | Mark all notifications as read                    |
| GET    | `/api/Profile/auctions`                   | Get my auctions                                   |
| GET    | `/api/Profile/bidding`                   | Auctions I’m currently bidding on                 |
| GET    | `/api/Profile/won`                       | Auctions I have won                                |
| DELETE | `/api/Profile/auction/{id}`               | Delete one of my auctions                         |
| PUT    | `/api/Profile/auction/{id}`               | Update my auction (multipart/form-data)           |
| POST   | `/api/Profile/auction`                    | Create auction under my profile                   |

### **Image Upload**

| Method | Endpoint             | Description                                      |
| ------ | -------------------- | ------------------------------------------------ |
| POST   | `/api/ImageUpload`   | Upload an image (multipart/form-data, secure)    |

---

## Database & Migrations

1. **Add a migration**  
   ```bash
   dotnet ef migrations add <MigrationName>
   ```
2. **Update database**  
   ```bash
   dotnet ef database update
   ```

---



