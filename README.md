# 🚗 Smart Car Rental System

A web-based car rental management system built with ASP.NET Core MVC, allowing customers to browse, book, and manage car rentals with admin oversight.

## Features

- Customer registration with document verification (profile photo, citizenship, license)
- Car browsing with filtering and comparison
- Online booking with Khalti payment integration
- Cash and Khalti payment support with advance payment option
- Festival-based dynamic pricing
- Special offers and discounts
- Admin dashboard for managing cars, bookings, users, and refunds
- Car damage reporting and charge management
- Late fee management
- Car tracking with GPS coordinates
- Car recommendation using Cosine Similarity algorithm
- Customer churn prediction
- Real-time chat between customer and admin using SignalR
- Sales, analytics, and churn reports with PDF export
- Google OAuth login
- Email notifications

## Tech Stack

- **Framework:** ASP.NET Core MVC (.NET 6)
- **Database:** MySQL 8.0
- **ORM:** Entity Framework Core (Pomelo)
- **Authentication:** ASP.NET Identity + Google OAuth
- **Real-time:** SignalR
- **Payment:** Khalti
- **Email:** MailKit (Gmail SMTP)
- **PDF Export:** PuppeteerSharp

## Setup Instructions

### Prerequisites
- .NET 6.0 SDK
- MySQL Server 8.0+
- Visual Studio 2022 or VS Code

### Steps

1. Clone the repository


Copy
markdown
git clone https://github.com/sushanntt/SmartCarRental.git


2. Navigate to the project folder

Copy
cd SmartCarRental/HajurKoCarRental


3. Copy the example appsettings files and rename them

Copy
copy appsettings.example.json appsettings.json
copy appsettings.Development.example.json appsettings.Development.json


4. Fill in your credentials in `appsettings.json`:
- MySQL connection string
- Google OAuth Client ID and Secret
- Gmail address

5. Fill in your Gmail app password in `appsettings.Development.json`

6. Run the project (migrations run automatically on startup)

Copy
dotnet run


## Contributors

- Sushant Thapa
