# Complete Deployment Guide for EVSRS on VPS

## 📋 Overview
This guide walks you through deploying the EVSRS (Electric Vehicle Self-Rental System) on a production VPS with Docker, Portainer, and Nginx reverse proxy.

## 🏗️ Architecture

```
Internet → Nginx (SSL/Reverse Proxy) → Docker Containers
                                      ├── EVSRS API
                                      ├── PostgreSQL Database  
                                      ├── Redis Cache
                                      ├── Elasticsearch (Logging)
                                      ├── Kibana (Log Viewer)
                                      └── Filebeat (Log Shipper)
```

## 🚀 Deployment Steps

### 1. Prepare VPS

**Requirements:**
- Ubuntu 20.04+ or similar Linux distribution
- Minimum 4GB RAM, 2 CPU cores
- 50GB+ storage
- Root access

**Run VPS setup:**
```bash
# Download and run setup script
wget https://raw.githubusercontent.com/HungUniverse/evsrs-be/main/scripts/vps-setup.sh
chmod +x vps-setup.sh
sudo ./vps-setup.sh
```

### 2. Configure Domain & DNS

**Set up A records:**
```
api.your-domain.com     → VPS_IP
admin.your-domain.com   → VPS_IP  
logs.your-domain.com    → VPS_IP
```

### 3. Clone Repository

```bash
cd /opt/evsrs
git clone https://github.com/HungUniverse/evsrs-be.git .
```

### 4. Configure Environment

```bash
# Copy and edit production environment
cp .env.production.example .env.production
nano .env.production

# Update these critical values:
# - Database passwords
# - JWT secret keys  
# - API keys for external services
# - Domain names
```

### 5. Setup SSL Certificates

```bash
# Edit domains in script first
nano scripts/ssl-setup.sh

# Run SSL setup
chmod +x scripts/ssl-setup.sh
sudo ./ssl-setup.sh
```

### 6. Deploy Application

```bash
# Make scripts executable
chmod +x scripts/*.sh

# Deploy production environment
./scripts/deploy.sh
```

### 7. Access Services

- **API**: https://api.your-domain.com
- **Swagger**: https://api.your-domain.com/swagger  
- **Portainer**: https://admin.your-domain.com
- **Logs**: https://logs.your-domain.com

## 🔧 Configuration Details

### Nginx Configuration
The Nginx configuration provides:
- SSL termination with Let's Encrypt
- Rate limiting for security
- Reverse proxy to containers
- Security headers
- Static file serving
- Compression

### Docker Compose Production
The production compose file includes:
- Resource limits for containers
- Health checks
- Automatic restarts
- Volume mounts for persistence
- Network isolation
- Environment variable management

### Monitoring & Alerts
The monitoring script checks:
- Container health
- API response times
- Database connectivity
- System resources
- SSL certificate expiry
- Application error rates

## 🔐 Security Features

### Implemented Security:
- **Firewall (UFW)**: Only necessary ports open
- **Fail2ban**: Automatic IP blocking for brute force
- **SSL/TLS**: HTTPS enforced with security headers
- **Rate Limiting**: API endpoint protection
- **Container Isolation**: Docker network security
- **Regular Updates**: Automatic security patches
- **Backup Strategy**: Automated database backups

### Security Checklist:
- [ ] Change default passwords
- [ ] Configure IP whitelist for admin access
- [ ] Set up monitoring alerts
- [ ] Regular security audits
- [ ] Keep Docker images updated
- [ ] Monitor logs for anomalies

## 🎯 Portainer Setup

### First Time Access:
1. Go to https://admin.your-domain.com
2. Create admin user (first visit only)
3. Select "Docker Standalone"
4. Connect to local Docker socket

### Managing Containers:
- **View Logs**: Containers → Select container → Logs
- **Scale Services**: Stacks → evsrs → Scale
- **Update Images**: Containers → Select → Recreate
- **Monitor Resources**: Dashboard → Statistics

### Creating Stacks in Portainer:
1. Go to Stacks → Add Stack
2. Name: "evsrs-production"
3. Upload docker-compose.production.yml
4. Add environment file
5. Deploy

## 📊 Monitoring & Maintenance

### Daily Tasks (Automated):
- Database backups
- Log rotation
- Container health checks
- Resource monitoring
- SSL certificate checks

### Weekly Tasks:
- Review monitoring reports
- Check backup integrity
- Update Docker images
- Security patch review

### Monthly Tasks:
- Full system backup
- Performance optimization
- Security audit
- Capacity planning review

## 🔄 CI/CD Pipeline

### Setup GitHub Secrets:
```
VPS_HOST          → your.vps.ip.address
VPS_USER          → deployment_user
VPS_SSH_KEY       → private_ssh_key
SLACK_WEBHOOK     → slack_webhook_url (optional)
```

### Pipeline Features:
- **Automated Testing**: Unit tests, security scans
- **Build & Push**: Docker image to GitHub Registry
- **Zero-downtime Deployment**: Rolling updates
- **Health Checks**: Post-deployment verification
- **Rollback**: Automatic on failure
- **Notifications**: Slack/Discord alerts

## 🛠️ Troubleshooting

### Common Issues:

**Container Won't Start:**
```bash
# Check logs
docker-compose -f docker-compose.production.yml logs [service_name]

# Check configuration
docker-compose -f docker-compose.production.yml config

# Restart service
./scripts/deploy.sh restart [service_name]
```

**SSL Certificate Issues:**
```bash
# Renew certificates
sudo certbot renew

# Test nginx config
sudo nginx -t

# Reload nginx
sudo systemctl reload nginx
```

**Database Connection:**
```bash
# Test database
docker-compose -f docker-compose.production.yml exec postgres pg_isready

# Access database
docker-compose -f docker-compose.production.yml exec postgres psql -U evsrs_user evsrs_production
```

**Performance Issues:**
```bash
# Check resources
./scripts/monitoring.sh

# Scale API containers
docker-compose -f docker-compose.production.yml up -d --scale evsrs-api=3
```

### Emergency Procedures:

**Complete Rollback:**
```bash
# Stop current version
docker-compose -f docker-compose.production.yml down

# Restore from backup
./scripts/backup.sh restore [backup_file]

# Deploy previous version
git checkout [previous_commit]
./scripts/deploy.sh
```

**Database Recovery:**
```bash
# Stop application
docker-compose -f docker-compose.production.yml stop evsrs-api

# Restore database
gunzip -c /opt/evsrs/backups/backup_YYYYMMDD_HHMMSS.sql.gz | \
docker-compose -f docker-compose.production.yml exec -T postgres \
psql -U evsrs_user evsrs_production

# Start application
docker-compose -f docker-compose.production.yml start evsrs-api
```

## 📞 Support & Maintenance

### Log Locations:
- **Application**: `/opt/evsrs/logs/`
- **Nginx**: `/var/log/nginx/`
- **System**: `/var/log/syslog`
- **Docker**: `docker-compose logs`

### Useful Commands:
```bash
# View all services
docker-compose -f docker-compose.production.yml ps

# Follow API logs
docker-compose -f docker-compose.production.yml logs -f evsrs-api

# Monitor system resources
htop

# Check disk space
df -h

# Network connections
netstat -tulpn
```

### Maintenance Windows:
- **Daily**: 2:00 AM - 3:00 AM (UTC) - Backups
- **Weekly**: Sunday 3:00 AM - 4:00 AM (UTC) - Updates
- **Monthly**: First Sunday 2:00 AM - 6:00 AM (UTC) - Full maintenance

---

**🎉 Deployment Complete!**

Your EVSRS application is now running in production with:
- ✅ High availability setup
- ✅ SSL encryption
- ✅ Automated monitoring
- ✅ Backup strategy
- ✅ CI/CD pipeline
- ✅ Security hardening

For support, check the logs and monitoring dashboard first, then refer to this guide for troubleshooting steps.