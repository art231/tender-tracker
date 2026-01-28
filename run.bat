@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo =========================================
echo TenderTracker System - Automated Runner
echo =========================================

REM Check if Docker is running
docker info >nul 2>&1
if errorlevel 1 (
    echo [✗] Docker is not running. Please start Docker and try again.
    exit /b 1
)

echo [✓] Docker is running

REM Check if Docker Compose is available
where docker-compose >nul 2>&1
if errorlevel 1 (
    echo [!] docker-compose not found, trying docker compose...
    set DOCKER_COMPOSE_CMD=docker compose
) else (
    set DOCKER_COMPOSE_CMD=docker-compose
)

REM Parse command line arguments
if "%~1"=="" (
    set COMMAND=up
) else (
    set COMMAND=%~1
)

if "%COMMAND%"=="up" (
    echo [✓] Building and starting TenderTracker system...
    %DOCKER_COMPOSE_CMD% up --build -d
    
    echo [✓] Waiting for services to start...
    timeout /t 10 /nobreak >nul
    
    echo [✓] Checking service status...
    
    curl -s http://localhost:5000/health >nul 2>&1
    if errorlevel 1 (
        echo [!] Backend API is still starting up...
    ) else (
        echo [✓] Backend API is running at http://localhost:5000
    )
    
    curl -s http://localhost:4300 >nul 2>&1
    if errorlevel 1 (
        echo [!] Frontend is still starting up...
    ) else (
        echo [✓] Frontend is running at http://localhost:4300
    )
    
    echo [✓] PostgreSQL is running at localhost:5432
    
    echo.
    echo [✓] =========================================
    echo [✓] TenderTracker System is now running!
    echo [✓] =========================================
    echo.
    echo Access the application at:
    echo   Frontend: http://localhost:4300
    echo   Backend API: http://localhost:5000
    echo   PostgreSQL: localhost:5432
    echo.
    echo To view logs: run.bat logs
    echo To stop: run.bat down
)

if "%COMMAND%"=="down" (
    echo [✓] Stopping TenderTracker system...
    %DOCKER_COMPOSE_CMD% down
    echo [✓] System stopped
)

if "%COMMAND%"=="logs" (
    echo [✓] Showing logs (Ctrl+C to exit)...
    %DOCKER_COMPOSE_CMD% logs -f
)

if "%COMMAND%"=="restart" (
    echo [✓] Restarting TenderTracker system...
    %DOCKER_COMPOSE_CMD% restart
    echo [✓] System restarted
)

if "%COMMAND%"=="status" (
    echo [✓] Checking service status...
    %DOCKER_COMPOSE_CMD% ps
)

if "%COMMAND%"=="clean" (
    echo [!] This will remove all containers, volumes, and images.
    set /p CONFIRM=Are you sure? (y/n): 
    if /i "!CONFIRM!"=="y" (
        echo [✓] Cleaning up...
        %DOCKER_COMPOSE_CMD% down -v --rmi all
        echo [✓] Cleanup completed
    ) else (
        echo [✓] Cleanup cancelled
    )
)

if "%COMMAND%"=="test" (
    echo [✓] Running system tests...
    
    echo [✓] Testing backend API...
    curl -s http://localhost:5000/health | findstr "Healthy" >nul
    if errorlevel 1 (
        echo [✗] Backend health check: FAILED
    ) else (
        echo [✓] Backend health check: PASSED
    )
    
    echo [✓] Testing database connection...
    curl -s http://localhost:5000/api/searchqueries >nul
    if errorlevel 1 (
        echo [✗] Database connection: FAILED
    ) else (
        echo [✓] Database connection: PASSED
    )
    
    echo [✓] Testing frontend...
    curl -s http://localhost:4300 | findstr "TenderTracker" >nul
    if errorlevel 1 (
        echo [✗] Frontend: FAILED
    ) else (
        echo [✓] Frontend: PASSED
    )
    
    echo [✓] All tests completed
)

if "%COMMAND%"=="seed" (
    echo [✓] Adding test data...
    
    curl -s http://localhost:5000/health >nul
    if errorlevel 1 (
        echo [✗] Backend is not running. Start the system first with: run.bat up
        exit /b 1
    )
    
    echo [✓] Adding test search queries...
    curl -X POST http://localhost:5000/api/searchqueries ^
        -H "Content-Type: application/json" ^
        -d "{\"keyword\": \"тестовый запрос 1\", \"category\": \"Тест\", \"isActive\": true}" ^
        -s >nul
    
    curl -X POST http://localhost:5000/api/searchqueries ^
        -H "Content-Type: application/json" ^
        -d "{\"keyword\": \"тестовый запрос 2\", \"category\": \"Тест\", \"isActive\": true}" ^
        -s >nul
    
    echo [✓] Test data added successfully
)

if not "%COMMAND%"=="up" if not "%COMMAND%"=="down" if not "%COMMAND%"=="logs" if not "%COMMAND%"=="restart" if not "%COMMAND%"=="status" if not "%COMMAND%"=="clean" if not "%COMMAND%"=="test" if not "%COMMAND%"=="seed" (
    echo Usage: run.bat [command]
    echo.
    echo Commands:
    echo   up       - Build and start the system ^(default^)
    echo   down     - Stop the system
    echo   logs     - View logs
    echo   restart  - Restart the system
    echo   status   - Check service status
    echo   test     - Run system tests
    echo   seed     - Add test data
    echo   clean    - Remove all containers, volumes, and images
    echo.
    exit /b 1
)

echo [✓] Done!
endlocal
