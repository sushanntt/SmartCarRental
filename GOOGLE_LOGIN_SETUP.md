# Google Login Setup Instructions

## Steps to Enable Google Login

### 1. Create a Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Enable the Google+ API for your project

### 2. Create OAuth 2.0 Credentials

1. In the Google Cloud Console, go to **APIs & Services** > **Credentials**
2. Click **Create Credentials** > **OAuth client ID**
3. If prompted, configure the OAuth consent screen:
   - Choose **External** user type
   - Fill in the required fields (App name, User support email, Developer contact)
   - Add scopes: `email`, `profile`, `openid`
   - Add test users if needed
4. For Application type, select **Web application**
5. Add Authorized redirect URIs:
   - For local development: `https://localhost:5001/signin-google`
   - For production: `https://yourdomain.com/signin-google`
6. Click **Create**
7. Copy the **Client ID** and **Client Secret**

### 3. Configure Your Application

1. Open `appsettings.json` in your project
2. Replace the placeholder values with your actual credentials:

```json
"Authentication": {
  "Google": {
    "ClientId": "YOUR_ACTUAL_CLIENT_ID_HERE",
    "ClientSecret": "YOUR_ACTUAL_CLIENT_SECRET_HERE"
  }
}
```

### 4. Install Required Package

The Google authentication package has already been added to your project:
- `Microsoft.AspNetCore.Authentication.Google` (Version 6.0.15)

### 5. Run the Application

1. Restore packages: `dotnet restore`
2. Build the project: `dotnet build`
3. Run the application: `dotnet run`

### 6. Test Google Login

1. Navigate to the Login or Register page
2. You should see a "Google" button
3. Click it to authenticate with your Google account
4. On first login, you'll be asked to provide additional information (First Name, Last Name, Address, Phone Number)
5. After completing the form, you'll be logged in

## Important Notes

- **Security**: Never commit your actual Client ID and Client Secret to version control
- **HTTPS Required**: Google OAuth requires HTTPS. Make sure your application runs on HTTPS
- **Redirect URI**: The redirect URI in Google Console must exactly match your application's URL
- **Email Confirmation**: The application requires email confirmation. Make sure to configure an email service or disable this requirement in `Program.cs`

## Troubleshooting

### "redirect_uri_mismatch" Error
- Ensure the redirect URI in Google Console matches exactly: `https://localhost:5001/signin-google`
- Check that you're using HTTPS, not HTTP

### "Error loading external login information"
- Clear your browser cookies and try again
- Check that your Client ID and Client Secret are correct
- Verify that the Google+ API is enabled in your Google Cloud project

### Phone Number Validation
- Phone numbers must be exactly 10 digits
- Only numeric characters are allowed
- This field is optional for Google login users

## Features Implemented

✅ Google OAuth 2.0 authentication
✅ Automatic user registration on first Google login
✅ Role assignment (Admin for first user, Customer for others)
✅ Phone number validation (exactly 10 digits)
✅ Integration with existing Identity system
✅ Support for both Login and Register pages
