#!/bin/bash

# Advanced Monitoring Script for EVSRS Production
# This script monitors application health, performance, and sends alerts

set -e

# Configuration
PROJECT_DIR="/opt/evsrs"
LOG_FILE="$PROJECT_DIR/logs/monitor.log"
ALERT_LOG="$PROJECT_DIR/logs/alerts.log"
COMPOSE_FILE="docker-compose.production.yml"

# Thresholds
CPU_THRESHOLD=80
MEMORY_THRESHOLD=85
DISK_THRESHOLD=80
RESPONSE_TIME_THRESHOLD=5000  # milliseconds
ERROR_RATE_THRESHOLD=5        # percentage

# Webhook URLs (configure these for your notification services)
SLACK_WEBHOOK=""
DISCORD_WEBHOOK=""
EMAIL_TO=""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Logging function
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

# Alert function
alert() {
    local message="$1"
    local severity="$2"
    
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] ALERT [$severity]: $message" | tee -a "$ALERT_LOG"
    
    # Send notifications
    send_slack_alert "$message" "$severity"
    send_email_alert "$message" "$severity"
}

# Send Slack notification
send_slack_alert() {
    local message="$1"
    local severity="$2"
    
    if [ -n "$SLACK_WEBHOOK" ]; then
        local color="good"
        [ "$severity" = "WARNING" ] && color="warning"
        [ "$severity" = "CRITICAL" ] && color="danger"
        
        curl -X POST -H 'Content-type: application/json' \
            --data "{\"attachments\":[{\"color\":\"$color\",\"title\":\"EVSRS $severity Alert\",\"text\":\"$message\",\"ts\":$(date +%s)}]}" \
            "$SLACK_WEBHOOK" 2>/dev/null || true
    fi
}

# Send email notification
send_email_alert() {
    local message="$1"
    local severity="$2"
    
    if [ -n "$EMAIL_TO" ]; then
        echo "$message" | mail -s "EVSRS $severity Alert" "$EMAIL_TO" 2>/dev/null || true
    fi
}

# Check container health
check_containers() {
    log "Checking container health..."
    
    local containers=("evsrs-api-prod" "evsrs-postgres-prod" "evsrs-redis-prod")
    local failed_containers=()
    
    for container in "${containers[@]}"; do
        if ! docker ps | grep -q "$container"; then
            failed_containers+=("$container")
        elif ! docker inspect "$container" --format='{{.State.Health.Status}}' 2>/dev/null | grep -q "healthy"; then
            # Check if container has health checks configured
            if docker inspect "$container" --format='{{.Config.Healthcheck}}' 2>/dev/null | grep -q "null"; then
                # No health check configured, just check if running
                if ! docker ps | grep -q "$container.*Up"; then
                    failed_containers+=("$container")
                fi
            else
                failed_containers+=("$container")
            fi
        fi
    done
    
    if [ ${#failed_containers[@]} -gt 0 ]; then
        alert "Containers not healthy: ${failed_containers[*]}" "CRITICAL"
        
        # Attempt to restart failed containers
        for container in "${failed_containers[@]}"; do
            log "Attempting to restart $container..."
            docker-compose -f "$PROJECT_DIR/$COMPOSE_FILE" restart "${container##*-}" || true
        done
    else
        log "✅ All containers are healthy"
    fi
}

# Check API response time and availability
check_api_health() {
    log "Checking API health..."
    
    local api_url="https://api.your-domain.com/health"
    local response_time
    local http_code
    
    # Get response time and HTTP code
    response_time=$(curl -w "%{time_total}" -s -o /dev/null "$api_url" 2>/dev/null || echo "999")
    http_code=$(curl -w "%{http_code}" -s -o /dev/null "$api_url" 2>/dev/null || echo "000")
    
    # Convert to milliseconds
    response_time_ms=$(echo "$response_time * 1000" | bc -l | cut -d. -f1)
    
    if [ "$http_code" != "200" ]; then
        alert "API health check failed - HTTP $http_code" "CRITICAL"
    elif [ "$response_time_ms" -gt "$RESPONSE_TIME_THRESHOLD" ]; then
        alert "API response time high: ${response_time_ms}ms" "WARNING"
    else
        log "✅ API health check passed (${response_time_ms}ms)"
    fi
}

# Check database connectivity
check_database() {
    log "Checking database connectivity..."
    
    if docker-compose -f "$PROJECT_DIR/$COMPOSE_FILE" exec -T postgres pg_isready -U evsrs_user -d evsrs_production >/dev/null 2>&1; then
        log "✅ Database is accessible"
        
        # Check database size and connections
        local db_size=$(docker-compose -f "$PROJECT_DIR/$COMPOSE_FILE" exec -T postgres psql -U evsrs_user -d evsrs_production -t -c "SELECT pg_size_pretty(pg_database_size('evsrs_production'));" 2>/dev/null | xargs)
        local active_connections=$(docker-compose -f "$PROJECT_DIR/$COMPOSE_FILE" exec -T postgres psql -U evsrs_user -d evsrs_production -t -c "SELECT count(*) FROM pg_stat_activity WHERE state = 'active';" 2>/dev/null | xargs)
        
        log "Database size: $db_size, Active connections: $active_connections"
        
        if [ "$active_connections" -gt 40 ]; then
            alert "High number of database connections: $active_connections" "WARNING"
        fi
    else
        alert "Database connectivity check failed" "CRITICAL"
    fi
}

# Check Redis connectivity
check_redis() {
    log "Checking Redis connectivity..."
    
    if docker-compose -f "$PROJECT_DIR/$COMPOSE_FILE" exec -T redis redis-cli ping 2>/dev/null | grep -q "PONG"; then
        log "✅ Redis is accessible"
        
        # Check Redis memory usage
        local redis_memory=$(docker-compose -f "$PROJECT_DIR/$COMPOSE_FILE" exec -T redis redis-cli info memory 2>/dev/null | grep "used_memory_human" | cut -d: -f2 | tr -d '\r')
        log "Redis memory usage: $redis_memory"
    else
        alert "Redis connectivity check failed" "CRITICAL"
    fi
}

# Check system resources
check_system_resources() {
    log "Checking system resources..."
    
    # CPU usage
    local cpu_usage=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | awk -F'%' '{print $1}')
    cpu_usage=${cpu_usage%.*}  # Remove decimal part
    
    # Memory usage
    local memory_usage=$(free | awk '/^Mem/ {printf "%.0f", $3/$2 * 100}')
    
    # Disk usage
    local disk_usage=$(df "$PROJECT_DIR" | awk 'NR==2 {print $5}' | sed 's/%//')
    
    log "System resources - CPU: ${cpu_usage}%, Memory: ${memory_usage}%, Disk: ${disk_usage}%"
    
    # Check thresholds
    if [ "$cpu_usage" -gt "$CPU_THRESHOLD" ]; then
        alert "High CPU usage: ${cpu_usage}%" "WARNING"
    fi
    
    if [ "$memory_usage" -gt "$MEMORY_THRESHOLD" ]; then
        alert "High memory usage: ${memory_usage}%" "WARNING"
    fi
    
    if [ "$disk_usage" -gt "$DISK_THRESHOLD" ]; then
        alert "High disk usage: ${disk_usage}%" "WARNING"
    fi
}

# Check application logs for errors
check_application_logs() {
    log "Checking application logs for errors..."
    
    # Get recent error logs (last 5 minutes)
    local error_count=$(docker-compose -f "$PROJECT_DIR/$COMPOSE_FILE" logs --since "5m" evsrs-api 2>/dev/null | grep -i "error\|exception\|fatal" | wc -l)
    local total_logs=$(docker-compose -f "$PROJECT_DIR/$COMPOSE_FILE" logs --since "5m" evsrs-api 2>/dev/null | wc -l)
    
    if [ "$total_logs" -gt 0 ]; then
        local error_rate=$((error_count * 100 / total_logs))
        
        if [ "$error_rate" -gt "$ERROR_RATE_THRESHOLD" ]; then
            alert "High error rate in application logs: ${error_rate}% (${error_count}/${total_logs})" "WARNING"
        else
            log "✅ Application error rate: ${error_rate}%"
        fi
    fi
}

# Check SSL certificate expiry
check_ssl_certificates() {
    log "Checking SSL certificate expiry..."
    
    local domains=("api.your-domain.com" "admin.your-domain.com" "logs.your-domain.com")
    
    for domain in "${domains[@]}"; do
        local expiry_date=$(echo | openssl s_client -servername "$domain" -connect "$domain:443" 2>/dev/null | openssl x509 -noout -dates 2>/dev/null | grep "notAfter" | cut -d= -f2)
        
        if [ -n "$expiry_date" ]; then
            local expiry_timestamp=$(date -d "$expiry_date" +%s)
            local current_timestamp=$(date +%s)
            local days_until_expiry=$(( (expiry_timestamp - current_timestamp) / 86400 ))
            
            if [ "$days_until_expiry" -lt 30 ]; then
                alert "SSL certificate for $domain expires in $days_until_expiry days" "WARNING"
            else
                log "✅ SSL certificate for $domain expires in $days_until_expiry days"
            fi
        else
            alert "Could not check SSL certificate for $domain" "WARNING"
        fi
    done
}

# Generate monitoring report
generate_report() {
    local report_file="$PROJECT_DIR/logs/monitoring_report_$(date +%Y%m%d).log"
    
    cat > "$report_file" << EOF
EVSRS Monitoring Report - $(date)
================================================

Container Status:
$(docker-compose -f "$PROJECT_DIR/$COMPOSE_FILE" ps)

System Resources:
CPU Usage: $(top -bn1 | grep "Cpu(s)" | awk '{print $2}')
Memory Usage: $(free -h | awk '/^Mem/ {print $3 "/" $2}')
Disk Usage: $(df -h "$PROJECT_DIR" | awk 'NR==2 {print $5 " of " $2}')

Recent Alerts (last 24 hours):
$(tail -n 50 "$ALERT_LOG" | grep "$(date +%Y-%m-%d)" || echo "No alerts")

API Performance:
$(curl -w "Response Time: %{time_total}s\nHTTP Code: %{http_code}\n" -s -o /dev/null https://api.your-domain.com/health)

EOF

    log "Monitoring report generated: $report_file"
}

# Main monitoring function
main() {
    log "🔍 Starting EVSRS monitoring check..."
    
    check_containers
    check_api_health
    check_database
    check_redis
    check_system_resources
    check_application_logs
    check_ssl_certificates
    
    log "✅ Monitoring check completed"
    
    # Generate daily report (run once per day)
    if [ "$(date +%H:%M)" = "06:00" ]; then
        generate_report
    fi
}

# Handle script arguments
case "${1:-monitor}" in
    "monitor")
        main
        ;;
    "report")
        generate_report
        ;;
    "test-alerts")
        alert "This is a test alert" "INFO"
        ;;
    *)
        echo "Usage: $0 {monitor|report|test-alerts}"
        exit 1
        ;;
esac