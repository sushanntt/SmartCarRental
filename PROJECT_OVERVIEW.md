# HajurKoCarRental Project Overview

Based on the existing codebase and documentation, here is a comprehensive analysis of the HajurKoCarRental project covering all key aspects.

## 1. Backend & Technology Stack
- **Framework:** ASP.NET Core MVC (.NET 7)
- **Language:** C#
- **Database:** MySQL (interfaced via Entity Framework Core & Pomelo)
- **Authentication:** ASP.NET Core Identity (with Roles: `Admin` and `Customer`) + Google OAuth Login.
- **Payment Gateway:** Khalti (Nepal digital wallet).
- **Email Service:** Gmail SMTP integration for sending notifications.

## 2. File & Folder Structure
The project follows a standard MVC (Model-View-Controller) pattern:
- `Controllers/`: Contains 12 controllers managing routing and business logic (e.g., `HomeController`, `RentController`, `OfferController`, `CarsController`).
- `Models/`: Contains 11 data models representing database tables (`Car`, `CarBrand`, `Rent`, `Offer`, `CarDamage`, etc.).
- `Views/`: Contains Razor `.cshtml` templates for the UI, separated by controller names.
- `Services/`: Contains business logic services (e.g., `FestivalService.cs` for pricing logic).
- `Areas/Identity/`: Contains authentication logic, extended user models (`HajurKoCarRentalUser`), and DbContext (`HajurKoCarRentalContext`).
- `wwwroot/`: Static web assets (CSS, JS, Images).
- `appsettings.json`: Configuration file storing connection strings and API keys.
- `Program.cs`: The modern .NET application entry point setting up Dependency Injection (DI) and middleware.

## 3. Key Features & Workflow
- **User Roles:** 
  - **Admin:** Full access to manage cars, brands, offers, damage reports, view sales reports, and approve/reject rentals.
  - **Customer:** Can browse cars, apply for rentals, view their rental history, and report car damages.
  - *(Note: A "Staff" role was planned but abandoned. Only Admin and Customer roles exist).*
- **Car Booking Flow:**
  1. Customer browses cars/offers on the home page.
  2. Profile completion is enforced before booking.
  3. User selects payment (Cash or Khalti).
  4. Once payment initiates/succeeds, a Rent request is created (Pending Approval).
  5. Admin approves the request, triggering an email notification to the user.
- **Dynamic Pricing:** Integrates a `FestivalService` that applies price multipliers based on active festivals, and gives a 10% discount to `RegularCustomer`s.
- **Car Damage System:** Customers can report damage, blocking them from booking again until the damage charge is paid.

## 4. Connections & Integrations
- **MySQL Database:** Connected via the string `HajurKoCarRentalContextConnection` in `appsettings.json`. It connects to `localhost:3306` with database name `HajurkoCarRental`.
- **Khalti Payment:** The Khalti API key is hardcoded in controller files (e.g., `RentController.cs`) for handling online payments.
- **Gmail SMTP:** Hardcoded app passwords exist in the controllers for sending automated approval/rejection emails.
- **Google Login:** OAuth is configured via `Authentication:Google` in `appsettings.json`.

## 5. How to Run the Project Successfully
To get this project running locally on your machine, you need to ensure the following steps are taken:

> [!IMPORTANT]
> **Prerequisites Checklist:**
> 1. Install **.NET 7 SDK**.
> 2. Install **MySQL Server** (e.g., XAMPP, WAMP, or standalone MySQL) and ensure it's running on port `3306`.
> 3. Ensure you have an IDE like **Visual Studio 2022** or **VS Code**.

### Steps to Run:
1. **Database Setup:** 
   - Open MySQL/phpMyAdmin and ensure a database can be created. The app uses EF Core Code-First migrations.
   - Run the command `dotnet ef database update` in the terminal from the `HajurKoCarRental` folder to create the database tables.
2. **Configuration:**
   - Verify `appsettings.json` connection string matches your MySQL root credentials. Default is `Uid=root;Pwd=;`.
3. **Run the App:**
   - Run `dotnet run` from the `HajurKoCarRental` directory or start it via Visual Studio.
4. **Seed Data:** 
   - You may need to register a user and manually promote them to the `Admin` role in the `AspNetRoles` and `AspNetUserRoles` database tables to access the admin dashboard.

> [!WARNING]
> **Known Critical Bugs to Fix Before Production:**
> - `FestivalService.cs` line 57: A logic error prevents festivals from being deleted (`if (festival != null)` instead of `== null`).
> - `RentController.cs` line 457: A substring error on Khalti callback crashes the app if the URL query doesn't contain a `?`.
> - *Credentials (Email/Khalti)* are hardcoded in the controllers and should be moved to `appsettings.json`.
