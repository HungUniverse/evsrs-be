# Hướng dẫn Deploy EVSRS trên VPS đơn giản

## 📋 Tổng quan
Hướng dẫn này giúp bạn deploy EVSRS trên VPS sử dụng:
- **Docker** (theo tài liệu chính thức)
- **Portainer** để quản lý containers
## Services Included

- **EVSRS API**: ASP.NET Core 8.0 application (built from source)
- **PostgreSQL 15**: Database with health checks
- **Nginx Proxy Manager**: Reverse proxy with SSL automation
- **Cloudflare** để quản lý DNS

## 🚀 Hướng dẫn từng bước

### Bước 1: Chuẩn bị VPS

**Yêu cầu VPS:**
- Ubuntu 20.04+ hoặc Debian 11+
- Tối thiểu: 2GB RAM, 2 CPU cores, 20GB storage
- Quyền root hoặc sudo

**Setup VPS:**
```bash
# Tải và chạy script setup
wget https://raw.githubusercontent.com/HungUniverse/evsrs-be/main/scripts/vps-simple-setup.sh
chmod +x vps-simple-setup.sh
sudo ./vps-simple-setup.sh

# Reboot VPS sau khi setup
sudo reboot
```

### Bước 2: Truy cập Portainer

1. Mở trình duyệt: `https://YOUR_VPS_IP:9443`
2. Tạo tài khoản admin (lần đầu tiên)
3. Chọn "Get Started" → "Docker Standalone"

### Bước 3: Clone repository và cấu hình

```bash
# SSH vào VPS
ssh your_user@YOUR_VPS_IP

# Clone repository
cd /opt/evsrs
git clone https://github.com/HungUniverse/evsrs-be.git .

# Cấu hình environment
cp .env.vps.example .env
nano .env
```

**Các giá trị quan trọng cần thay đổi trong `.env`:**
```bash
# Database
POSTGRES_PASSWORD=your_secure_password_here

# Redis
REDIS_PASSWORD=your_redis_password_here

# JWT (tạo key dài ít nhất 32 ký tự)
JWT_SECRET_KEY=your_very_long_jwt_secret_key_here

# API keys của các dịch vụ external
CLOUDINARY_API_KEY=your_key
MAILGUN_API_KEY=your_key
# ... các keys khác
```

### Bước 4: Deploy ứng dụng

```bash
# Làm script có thể thực thi
chmod +x scripts/deploy-vps.sh

# Deploy
./scripts/deploy-vps.sh
```

### Bước 5: Kiểm tra deployment

```bash
# Xem status các services
./scripts/deploy-vps.sh status

# Xem logs
./scripts/deploy-vps.sh logs
```

## 🎯 Cấu hình Nginx Proxy Manager

### Truy cập NPM:
- URL: `http://YOUR_VPS_IP:81`
- Login mặc định: `admin@example.com` / `changeme`
- **Đổi password ngay lập tức!**

### Tạo Proxy Host:

1. **Hosts → Proxy Hosts → Add Proxy Host**

2. **Details Tab:**
   - Domain Names: `api.yourdomain.com`
   - Scheme: `http`
   - Forward Hostname/IP: `evsrs-api`
   - Forward Port: `8080`
   - ✅ Cache Assets
   - ✅ Block Common Exploits
   - ✅ Websockets Support

3. **SSL Tab:**
   - SSL Certificate: `Request a new SSL Certificate`
   - ✅ Force SSL
   - ✅ HTTP/2 Support
   - ✅ Use a DNS Challenge
   - DNS Provider: `Cloudflare`
   - Credentials File Content:
   ```
   dns_cloudflare_api_token = YOUR_CLOUDFLARE_API_TOKEN
   ```

4. **Advanced Tab (optional):**
   ```nginx
   # Rate limiting
   limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;
   limit_req zone=api burst=20 nodelay;
   
   # Security headers
   add_header X-Frame-Options DENY;
   add_header X-Content-Type-Options nosniff;
   add_header X-XSS-Protection "1; mode=block";
   ```

### Tạo thêm proxy hosts cho admin:

**Admin Panel (Portainer):**
- Domain: `admin.yourdomain.com`
- Forward to: `YOUR_VPS_IP:9443`
- Scheme: `https`
- ✅ Use SSL + Force SSL

**Logs (Kibana - nếu enable monitoring):**
- Domain: `logs.yourdomain.com`
- Forward to: `evsrs-kibana:5601`
- Scheme: `http`

## ☁️ Cấu hình Cloudflare DNS

### Trong Cloudflare Dashboard:

1. **DNS Records:**
   ```
   Type: A
   Name: api
   Content: YOUR_VPS_IP
   Proxy status: 🟠 DNS only (initially)
   
   Type: A  
   Name: admin
   Content: YOUR_VPS_IP
   Proxy status: 🟠 DNS only
   
   Type: A
   Name: logs  
   Content: YOUR_VPS_IP
   Proxy status: 🟠 DNS only
   ```

2. **Sau khi SSL hoạt động ổn định:**
   - Chuyển sang Proxy status: 🟡 Proxied
   - Kích hoạt các tính năng bảo mật của Cloudflare

### Tạo API Token cho DNS Challenge:
1. **My Profile → API Tokens → Create Token**
2. **Custom token:**
   - Token name: `EVSRS-DNS-Challenge`
   - Permissions:
     - Zone:Zone:Read
     - Zone:DNS:Edit
   - Zone Resources:
     - Include:Specific zone:yourdomain.com

## 🎛️ Quản lý qua Portainer

### Truy cập Portainer:
- URL: `https://YOUR_VPS_IP:9443` hoặc `https://admin.yourdomain.com`

### Các tác vụ thường dùng:

**Xem logs:**
1. Containers → Chọn container → Logs

**Restart service:**
1. Containers → Chọn container → Restart

**Scale service:**
1. Stacks → evsrs → Editor → Chỉnh sửa scale
2. Update the stack

**Update image:**
1. Images → Pull new image
2. Containers → Recreate với image mới

**Backup database:**
```bash
# SSH vào VPS
./scripts/deploy-vps.sh backup
```

## 📊 Monitoring (Optional)

### Enable monitoring services:
```bash
./scripts/deploy-vps.sh monitoring
```

### Truy cập Kibana:
- URL: `http://YOUR_VPS_IP:5601` hoặc qua NPM
- Xem logs ứng dụng trong real-time

## 🔧 Troubleshooting

### Kiểm tra status:
```bash
./scripts/deploy-vps.sh status
```

### Xem logs:
```bash
# Logs của API
./scripts/deploy-vps.sh logs evsrs-api

# Logs của database
./scripts/deploy-vps.sh logs postgres

# Logs của tất cả services
docker compose -f docker-compose.vps.yml logs
```

### Restart services:
```bash
# Restart API
./scripts/deploy-vps.sh restart evsrs-api

# Restart all
./scripts/deploy-vps.sh stop
./scripts/deploy-vps.sh start
```

### Container không start được:
```bash
# Xem lỗi chi tiết
docker compose -f docker-compose.vps.yml logs [service_name]

# Kiểm tra resource
docker stats

# Kiểm tra disk space
df -h
```

### Database connection error:
```bash
# Kiểm tra database
docker compose -f docker-compose.vps.yml exec postgres pg_isready -U evsrs_user

# Connect vào database để debug
docker compose -f docker-compose.vps.yml exec postgres psql -U evsrs_user evsrs_production
```

## 🔒 Security Checklist

### Sau khi deploy:
- [ ] Đổi password mặc định của Nginx Proxy Manager
- [ ] Tạo user mới cho Portainer (xóa admin mặc định)
- [ ] Cấu hình Cloudflare security rules
- [ ] Enable 2FA cho Cloudflare account
- [ ] Backup định kỳ database
- [ ] Monitor logs thường xuyên
- [ ] Update containers định kỳ

### Firewall VPS:
```bash
# Kiểm tra firewall status
sudo ufw status

# Chỉ cho phép cần thiết
sudo ufw allow ssh
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 9443/tcp  # Portainer
sudo ufw allow 81/tcp    # NPM
```

## 🎉 Hoàn thành!

Sau khi hoàn thành các bước trên, bạn sẽ có:

✅ **API**: `https://api.yourdomain.com`
✅ **Swagger**: `https://api.yourdomain.com/swagger`
✅ **Admin**: `https://admin.yourdomain.com` (Portainer)
✅ **Logs**: `https://logs.yourdomain.com` (Kibana)

### Các URLs quan trọng:
- **Health Check**: `https://api.yourdomain.com/health`
- **API Documentation**: `https://api.yourdomain.com/swagger`
- **Database**: Chỉ accessible từ containers (bảo mật)
- **Redis**: Chỉ accessible từ containers (bảo mật)

### Backup và Maintenance:
```bash
# Backup hàng ngày (có thể setup cron)
./scripts/deploy-vps.sh backup

# Update containers
docker compose -f docker-compose.vps.yml pull
docker compose -f docker-compose.vps.yml up -d

# Clean up
./scripts/deploy-vps.sh cleanup
```

**🎊 EVSRS của bạn đã sẵn sàng phục vụ production!**