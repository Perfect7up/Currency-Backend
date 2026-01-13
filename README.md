# Cryptocurrency Project - Backend

This is the backend API repository for a cryptocurrency trading/management platform built with .NET 10 and PostgreSQL.

## Tech Stack

- **Framework**: .NET 10 (ASP.NET Core Web API)
- **Database**: PostgreSQL
- **Authentication**: JWT (JSON Web Tokens)
- **ORM**: Entity Framework Core with Npgsql
- **API Documentation**: OpenAPI/Swagger
- **Frontend Repository**: [Currency-Frontend](https://github.com/Perfect7up/Currency-Frontend)

## Features

- **JWT Authentication**: Secure user authentication with Bearer token
- **Cryptocurrency Data Integration**: Real-time crypto data via CoinGecko and CryptoCompare APIs
- **Market Analysis**: Market data and analytics services
- **News Service**: Cryptocurrency news aggregation
- **Chart Service**: Historical price charts and technical analysis
- **Watchlist Management**: User-specific cryptocurrency watchlists
- **Email Notifications**: SMTP email service for user communications
- **CORS Enabled**: Configured for React frontend integration

## Architecture

This application follows a layered architecture:
- **Controllers**: API endpoints and request handling
- **Services**: Business logic and external API integration
- **Data Layer**: Entity Framework Core with PostgreSQL
- **Authentication**: JWT-based security middleware

## Getting Started

### Prerequisites

- .NET 10 SDK
- PostgreSQL 14+
- Frontend application running (see [frontend repo](https://github.com/Perfect7up/Currency-Frontend))

### Installation

```bash
# Clone the repository
git clone https://github.com/Perfect7up/Currency-Backend.git
cd Currency-Backend

# Restore dependencies
dotnet restore

# Update database
dotnet ef database update

# Run the application
dotnet run
```

## Configuration

### appsettings.json

Create an `appsettings.json` file in the project root with the following structure:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=cryptocdb;Username=your_username;Password=your_password"
  },
  "Jwt": {
    "Key": "your-super-secret-jwt-key-min-32-characters",
    "Issuer": "CryptocAPI",
    "Audience": "CryptocClient",
    "ExpiryInMinutes": 60
  },
  "CryptoCompare": {
    "ApiKey": "your_cryptocompare_api_key"
  },
  "Mailtrap": {
    "Username": "your_mailtrap_username",
    "Password": "your_mailtrap_password",
    "SenderEmail": "hello@demomailtrap.com",
    "SenderName": "CryptoC Terminal"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your_email@gmail.com",
    "Password": "your_app_password",
    "SenderEmail": "your_email@gmail.com",
    "SenderName": "CryptoC Terminal"
  },
  "FrontendUrl": "http://localhost:5173"
}
```

### Environment Variables (Production)

For production deployment, use environment variables instead of storing sensitive data in appsettings.json:

```bash
ConnectionStrings__DefaultConnection="Host=prod-host;Database=cryptocdb;Username=user;Password=pass"
Jwt__Key="your-production-jwt-key"
CryptoCompare__ApiKey="your_api_key"
Smtp__Username="your_email"
Smtp__Password="your_password"
```

### Database Setup

1. Install PostgreSQL and create a database:

```sql
CREATE DATABASE cryptocdb;
```

2. Update the connection string in `appsettings.json`

3. Run migrations:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## API Documentation

The API documentation is available via Swagger UI when running in development mode:

- **Full API Documentation**: `http://localhost:5000/swagger`
- **Individual Controller Docs**: Available per controller in Swagger dropdown

### Authentication

All protected endpoints require a JWT token in the Authorization header:

```
Authorization: Bearer <your_jwt_token>
```

To obtain a token:
1. Register a new user via `/api/auth/register`
2. Login via `/api/auth/login` to receive your JWT token
3. Include the token in subsequent requests

## Services

### Core Services

- **ICoinService**: Cryptocurrency data from CoinGecko
- **IMarketService**: Market analysis and statistics
- **INewsService**: Cryptocurrency news aggregation
- **IChartService**: Historical price data and charting
- **IToolsService**: Utility tools and calculators
- **IAuthService**: User authentication and authorization
- **IEmailService**: Email notifications (supports both SMTP and Mailtrap)
- **IWatchlistService**: User watchlist management

## Project Structure

```
Backend/
├── Controllers/         # API Controllers
├── Services/           # Business logic services
├── Data/              # Database context and migrations
├── Models/            # Entity models and DTOs
├── Middleware/        # Custom middleware
├── Program.cs         # Application entry point
└── appsettings.json   # Configuration file
```

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - User login
- `POST /api/auth/forgot-password` - Password recovery

### Cryptocurrency
- `GET /api/coin` - List cryptocurrencies
- `GET /api/coin/{id}` - Get specific coin details

### Market
- `GET /api/market` - Market overview
- `GET /api/market/trending` - Trending coins

### Watchlist
- `GET /api/watchlist` - Get user watchlist
- `POST /api/watchlist` - Add to watchlist
- `DELETE /api/watchlist/{id}` - Remove from watchlist

### Charts
- `GET /api/chart/{id}` - Get historical price data

### News
- `GET /api/news` - Latest cryptocurrency news

## CORS Configuration

The API is configured to accept requests from the React frontend:

```csharp
options.AddPolicy("AllowReact", policy => 
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());
```

For production, update this to restrict origins:

```csharp
policy.WithOrigins("https://your-production-domain.com")
```

## External API Integration

### CoinGecko API
Free tier provides cryptocurrency data. No API key required for basic usage.

### CryptoCompare API
Requires API key. Get yours at: https://www.cryptocompare.com/cryptopian/api-keys

## Email Configuration

### Development (Mailtrap)
Use Mailtrap for testing emails without sending real emails:
1. Sign up at https://mailtrap.io
2. Add credentials to `appsettings.json`

### Production (SMTP)
For production, use a real SMTP service:
- Gmail: Enable 2FA and create an App Password
- SendGrid, AWS SES, or other email services

## Security Best Practices

⚠️ **Important**: Never commit sensitive data to version control!

- Store secrets in User Secrets for development
- Use environment variables in production
- Rotate API keys and passwords regularly
- Use strong JWT secret keys (minimum 32 characters)
- Enable HTTPS in production
- Implement rate limiting for API endpoints

## Development

```bash
# Watch mode for development
dotnet watch run

# Run tests
dotnet test

# Create new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update
```

## Deployment

### Prerequisites for Production
- PostgreSQL database instance
- SSL certificate for HTTPS
- Environment variables configured
- CORS policy updated with production domain

### Deploy to Cloud
The application can be deployed to:
- Azure App Service
- AWS Elastic Beanstalk
- Docker containers
- Any platform supporting .NET 10

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Troubleshooting

### Common Issues

**Database Connection Failed**
- Verify PostgreSQL is running
- Check connection string in appsettings.json
- Ensure database exists

**JWT Authentication Failed**
- Verify JWT configuration in appsettings.json
- Check token expiry
- Ensure Authorization header is correctly formatted

**CORS Errors**
- Update CORS policy with correct frontend URL
- Check that frontend is running on the configured port

## License

This project is licensed under the MIT License.

## Support

For issues and questions:
- Open an issue on GitHub
- Check existing documentation
- Review API documentation in Swagger

---

**Note**: Remember to update your `appsettings.json` with your own credentials and never commit sensitive information to version control. Use User Secrets for development and environment variables for production.
