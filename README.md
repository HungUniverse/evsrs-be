# EVSRS Backend - Development Setup

## 🚀 Quick Start

### Prerequisites
- Docker & Docker Compose
- Git

### Setup Instructions

1. **Clone the repository**
   ```bash
   git clone <your-repo-url>
   cd evsrs-be
   ```

2. **Setup environment variables**
   ```bash
   # Copy the example environment file
   cp .env.example .env
   
   # Edit .env file with your actual values
   nano .env  # or use your preferred editor
   ```

3. **Start the development environment**
   ```bash
   # Make scripts executable
   chmod +x start-dev.sh stop-dev.sh
   
   # Start all services
   ./start-dev.sh
   ```

4. **Access the application**
   - API: http://localhost:5000
   - Swagger: http://localhost:5000/swagger
   - Database: localhost:5432 (postgres/postgres123)
   - Redis: localhost:6379

## 🔧 Manual Setup

### Start services
```bash
docker-compose up -d
```

### Stop services
```bash
docker-compose down
```

### View logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f evsrs-api
```

### Reset database (remove all data)
```bash
docker-compose down -v
docker-compose up -d
```

## 📁 Project Structure

```
evsrs-be/
├── EVSRS.API/              # Web API Layer
├── EVSRS.BusinessObjects/  # Entities & DTOs
├── EVSRS.Repositories/     # Data Access Layer
├── EVSRS.Services/         # Business Logic Layer
├── .env.example            # Template for environment variables
├── .env                    # Your environment variables (DO NOT COMMIT)
├── docker-compose.yml      # Docker services configuration
├── start-dev.sh           # Start development environment
└── stop-dev.sh            # Stop development environment
```

## 🔐 Security

### Environment Variables
- **NEVER** commit `.env` file to version control
- Use `.env.example` as a template for other developers
- Store production secrets in secure vault services

### Files excluded from Git:
- `.env` - Environment variables with sensitive data
- `logs/` - Application logs
- `appsettings.Development.json` - Development settings with secrets

## 🐳 Docker Services

### evsrs-api
- **Port**: 5000 → 8080
- **Environment**: Development
- **Dependencies**: PostgreSQL, Redis

### postgres
- **Port**: 5432
- **Database**: evsrs_dev
- **User**: postgres
- **Password**: postgres123

### redis
- **Port**: 6379
- **Password**: None (development only)

## 🔧 Troubleshooting

### Port already in use
```bash
# Check what's using the port
lsof -i :5000

# Stop existing containers
docker-compose down
```

### Database connection issues
```bash
# Restart database
docker-compose restart postgres

# Check database logs
docker-compose logs postgres
```

### Clear all Docker data
```bash
# ⚠️  This will remove ALL Docker data
docker system prune -a --volumes
```

## 📝 Development Tips

1. **Hot Reload**: Code changes require rebuilding the Docker image
   ```bash
   docker-compose up --build
   ```

2. **Database Migrations**: Run inside the container
   ```bash
   docker-compose exec evsrs-api dotnet ef database update
   ```

3. **Install packages**: Rebuild after adding NuGet packages
   ```bash
   docker-compose up --build evsrs-api
   ```

## 🌍 Environment Configuration

Edit `.env` file to configure:
- Database credentials
- JWT settings
- Third-party API keys (Cloudinary, Mailgun, etc.)
- External service URLs

## 🔍 Health Checks

- API Health: http://localhost:5000/health
- Database: Automatic health checks in Docker
- Redis: Automatic health checks in Docker

---

## ⚠️ Important Notes

1. **Never commit sensitive data** - Always use `.env` files
2. **Use different secrets** for production environments
3. **Regularly update dependencies** and base images
4. **Monitor logs** for security issues

## 🆘 Support

If you encounter issues:
1. Check the logs: `docker-compose logs -f`
2. Verify environment variables in `.env`
3. Ensure Docker daemon is running
4. Try restarting services: `./stop-dev.sh && ./start-dev.sh`