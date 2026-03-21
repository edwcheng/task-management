#!/bin/bash

# Task Manager - Development Startup Script
# This script starts both the backend and frontend for local development

echo "Starting Task Manager Development Environment..."

# Check if PostgreSQL is running
if ! docker ps | grep -q postgres; then
    echo "Starting PostgreSQL container..."
    docker run -d --name taskmanager-postgres \
        -e POSTGRES_USER=postgres \
        -e POSTGRES_PASSWORD=postgres \
        -e POSTGRES_DB=taskmanager \
        -p 5432:5432 \
        postgres:16-alpine
    sleep 5  # Wait for PostgreSQL to start
fi

# Start backend
echo "Starting .NET Core backend..."
cd backend
export PATH="$PATH:$HOME/.dotnet"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet run &
BACKEND_PID=$!

# Start frontend
echo "Starting Vue.js frontend..."
cd ../frontend
npm run dev &
FRONTEND_PID=$!

echo ""
echo "Task Manager is now running!"
echo "  Frontend: http://localhost:3000"
echo "  Backend:  http://localhost:5000"
echo "  Swagger:  http://localhost:5000/swagger"
echo ""
echo "Press Ctrl+C to stop all services"

# Wait for processes
trap "kill $BACKEND_PID $FRONTEND_PID 2>/dev/null" EXIT
wait
