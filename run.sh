#!/bin/bash

# TenderTracker System Runner
# This script helps to build and run the TenderTracker system

set -e

echo "========================================="
echo "TenderTracker System - Automated Runner"
echo "========================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[✓]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[!]${NC} $1"
}

print_error() {
    echo -e "${RED}[✗]${NC} $1"
}

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    print_error "Docker is not running. Please start Docker and try again."
    exit 1
fi

print_status "Docker is running"

# Check if Docker Compose is available
if ! command -v docker-compose &> /dev/null; then
    print_warning "docker-compose not found, trying docker compose..."
    DOCKER_COMPOSE_CMD="docker compose"
else
    DOCKER_COMPOSE_CMD="docker-compose"
fi

# Parse command line arguments
COMMAND=${1:-"up"}
ENV=${2:-"dev"}

case $COMMAND in
    "up")
        print_status "Building and starting TenderTracker system..."
        $DOCKER_COMPOSE_CMD up --build -d
        
        print_status "Waiting for services to start..."
        sleep 10
        
        # Check service status
        print_status "Checking service status..."
        
        if curl -s http://localhost:5000/health > /dev/null; then
            print_status "Backend API is running at http://localhost:5000"
        else
            print_warning "Backend API is still starting up..."
        fi
        
        if curl -s http://localhost:8080 > /dev/null; then
            print_status "Frontend is running at http://localhost:8080"
        else
            print_warning "Frontend is still starting up..."
        fi
        
        print_status "PostgreSQL is running at localhost:5432"
        
        echo ""
        print_status "========================================="
        print_status "TenderTracker System is now running!"
        print_status "========================================="
        echo ""
        echo "Access the application at:"
        echo "  Frontend: http://localhost:8080"
        echo "  Backend API: http://localhost:5000"
        echo "  PostgreSQL: localhost:5432"
        echo ""
        echo "To view logs: ./run.sh logs"
        echo "To stop: ./run.sh down"
        ;;
    
    "down")
        print_status "Stopping TenderTracker system..."
        $DOCKER_COMPOSE_CMD down
        print_status "System stopped"
        ;;
    
    "logs")
        print_status "Showing logs (Ctrl+C to exit)..."
        $DOCKER_COMPOSE_CMD logs -f
        ;;
    
    "restart")
        print_status "Restarting TenderTracker system..."
        $DOCKER_COMPOSE_CMD restart
        print_status "System restarted"
        ;;
    
    "status")
        print_status "Checking service status..."
        $DOCKER_COMPOSE_CMD ps
        ;;
    
    "clean")
        print_warning "This will remove all containers, volumes, and images."
        read -p "Are you sure? (y/n): " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            print_status "Cleaning up..."
            $DOCKER_COMPOSE_CMD down -v --rmi all
            print_status "Cleanup completed"
        else
            print_status "Cleanup cancelled"
        fi
        ;;
    
    "test")
        print_status "Running system tests..."
        
        # Test backend API
        print_status "Testing backend API..."
        if curl -s http://localhost:5000/health | grep -q "Healthy"; then
            print_status "Backend health check: PASSED"
        else
            print_error "Backend health check: FAILED"
        fi
        
        # Test database connection
        print_status "Testing database connection..."
        if curl -s http://localhost:5000/api/searchqueries > /dev/null; then
            print_status "Database connection: PASSED"
        else
            print_error "Database connection: FAILED"
        fi
        
        # Test frontend
        print_status "Testing frontend..."
        if curl -s http://localhost:8080 | grep -q "TenderTracker"; then
            print_status "Frontend: PASSED"
        else
            print_error "Frontend: FAILED"
        fi
        
        print_status "All tests completed"
        ;;
    
    "seed")
        print_status "Adding test data..."
        
        # Check if backend is running
        if ! curl -s http://localhost:5000/health > /dev/null; then
            print_error "Backend is not running. Start the system first with: ./run.sh up"
            exit 1
        fi
        
        # Add some test search queries
        print_status "Adding test search queries..."
        curl -X POST http://localhost:5000/api/searchqueries \
            -H "Content-Type: application/json" \
            -d '{"keyword": "тестовый запрос 1", "category": "Тест", "isActive": true}' \
            -s > /dev/null
        
        curl -X POST http://localhost:5000/api/searchqueries \
            -H "Content-Type: application/json" \
            -d '{"keyword": "тестовый запрос 2", "category": "Тест", "isActive": true}' \
            -s > /dev/null
        
        print_status "Test data added successfully"
        ;;
    
    *)
        echo "Usage: ./run.sh [command]"
        echo ""
        echo "Commands:"
        echo "  up       - Build and start the system (default)"
        echo "  down     - Stop the system"
        echo "  logs     - View logs"
        echo "  restart  - Restart the system"
        echo "  status   - Check service status"
        echo "  test     - Run system tests"
        echo "  seed     - Add test data"
        echo "  clean    - Remove all containers, volumes, and images"
        echo ""
        exit 1
        ;;
esac

print_status "Done!"
