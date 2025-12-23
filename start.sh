#!/bin/bash

# Soundmates Backend - Startup Script

echo "=========================================="
echo "  Soundmates Backend - Docker Setup"
echo "=========================================="
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "? Error: Docker is not running. Please start Docker Desktop and try again."
    exit 1
fi

echo "? Docker is running"
echo ""

# Build and start services
echo "?? Building and starting services..."
echo ""
docker-compose up -d --build

echo ""
echo "? Waiting for services to be ready..."
sleep 10

# Check service status
echo ""
echo "?? Service Status:"
docker-compose ps

echo ""
echo "=========================================="
echo "  ? Setup Complete!"
echo "=========================================="
echo ""
echo "?? Service Endpoints:"
echo "   - API Gateway:        http://localhost:8000"
echo "   - Auth Service:       http://localhost:8001"
echo "   - Auth Query Service: http://localhost:8002"
echo "   - PostgreSQL:         localhost:5432"
echo "   - RabbitMQ UI:        http://localhost:15672"
echo ""
echo "?? Useful Commands:"
echo "   - View logs:          docker-compose logs -f"
echo "   - Stop services:      docker-compose down"
echo "   - Restart services:   docker-compose restart"
echo ""
echo "?? For more information, see DOCKER-README.md"
echo ""
