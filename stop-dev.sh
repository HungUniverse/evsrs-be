#!/bin/bash

# Stop Development Environment Script

echo "🛑 Stopping EVSRS Development Environment..."

# Stop and remove containers
docker-compose down

echo "✅ All services stopped!"

# Optional: Remove volumes (uncomment if you want to clear data)
# echo "🗑️  Removing volumes..."
# docker-compose down -v

echo "💡 To start again, run: ./start-dev.sh"