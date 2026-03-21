# Task Manager Web Application

A full-stack task management web application built with .NET Core backend and Vue.js frontend, designed to run on Linux servers.

## Features

### Core Features
- **Forum-based Task Management**: Organize tasks into different forums
- **Task Management**: Create, read, update, and delete tasks with:
  - Title and body text
  - File attachments
  - Assigned person
  - Task status (Open, In Progress, Completed, Closed, On Hold)
  - Priority levels (Low, Normal, High, Urgent)
  - Due dates
- **Reply System**: Users can reply to tasks and discuss
- **File Attachments**: Attach files to tasks and replies

### User Management
- **Authentication**: JWT-based authentication system
- **User Roles**:
  - **Normal Users**: Can read, post tasks, and reply
  - **Admin Users**: Full CRUD access to users, forums, and posts

## Tech Stack

### Backend
- .NET Core 8.0
- Entity Framework Core with PostgreSQL
- JWT Authentication
- Swagger/OpenAPI documentation

### Frontend
- Vue.js 3 with TypeScript
- Vite build tool
- Pinia state management
- Vue Router

### Infrastructure
- Docker & Docker Compose
- Nginx reverse proxy
- PostgreSQL database

## Quick Start

### Prerequisites
- Docker and Docker Compose installed
- (Optional) .NET SDK 8.0 and Node.js 20+ for local development

### Running with Docker Compose

1. Clone the repository:
```bash
cd /home/z/my-project/task-management
```

2. Start all services:
```bash
docker-compose up -d --build
```

3. Access the application:
- Frontend: http://localhost
- Backend API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

### Local Development

#### Backend

1. Install PostgreSQL or use Docker:
```bash
docker run -d --name postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=taskmanager -p 5432:5432 postgres:16-alpine
```

2. Run the backend:
```bash
cd backend
dotnet restore
dotnet run
```

The API will be available at http://localhost:5000

#### Frontend

1. Install dependencies:
```bash
cd frontend
npm install
```

2. Run the development server:
```bash
npm run dev
```

The frontend will be available at http://localhost:3000

## Project Structure

```
task-management/
├── backend/
│   ├── Controllers/       # API Controllers
│   ├── Data/              # Database context
│   ├── Models/            # Entity models
│   ├── Services/          # Business logic
│   ├── DTOs/              # Data transfer objects
│   ├── Program.cs         # Application entry point
│   ├── appsettings.json   # Configuration
│   └── Dockerfile
├── frontend/
│   ├── src/
│   │   ├── api/           # API client
│   │   ├── components/    # Vue components
│   │   ├── views/         # Page components
│   │   ├── stores/        # Pinia stores
│   │   ├── router/        # Vue Router config
│   │   ├── types/         # TypeScript types
│   │   └── main.ts        # Application entry
│   ├── vite.config.ts
│   └── Dockerfile
├── docker-compose.yml
└── README.md
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login
- `POST /api/auth/register` - Register new user

### Forums
- `GET /api/forums` - List all forums
- `GET /api/forums/{id}` - Get forum by ID
- `POST /api/forums` - Create forum (authenticated)
- `PUT /api/forums/{id}` - Update forum (admin only)
- `DELETE /api/forums/{id}` - Delete forum (admin only)

### Tasks
- `GET /api/tasks/forum/{forumId}` - List tasks in forum
- `GET /api/tasks/{id}` - Get task details
- `POST /api/tasks` - Create task (authenticated)
- `PUT /api/tasks/{id}` - Update task (owner or admin)
- `DELETE /api/tasks/{id}` - Delete task (owner or admin)

### Replies
- `GET /api/replies/task/{taskId}` - List replies for task
- `POST /api/replies/task/{taskId}` - Create reply (authenticated)
- `PUT /api/replies/{id}` - Update reply (owner or admin)
- `DELETE /api/replies/{id}` - Delete reply (owner or admin)

### Users (Admin)
- `GET /api/users` - List all users
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user
- `PUT /api/users/{id}/role` - Set user role
- `PUT /api/users/{id}/active` - Set user active status

### Attachments
- `POST /api/attachments/upload` - Upload attachment
- `GET /api/attachments/{id}/download` - Download attachment
- `DELETE /api/attachments/{id}` - Delete attachment

## Configuration

### Environment Variables

#### Backend
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `Jwt__SecretKey` - JWT signing key (change in production!)
- `Jwt__Issuer` - JWT issuer
- `Jwt__Audience` - JWT audience
- `Jwt__ExpirationMinutes` - Token expiration time

#### Frontend
- `VITE_API_URL` - API base URL (default: `/api`)

## License

MIT License
