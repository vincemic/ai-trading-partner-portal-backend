# VS Code Development Guide

This guide explains how to use Visual Studio Code tasks and configurations for developing the Trading Partner Portal API.

## Available Tasks

### Build & Restore
- **Build Solution**: Compiles the entire solution and shows any compilation errors
- **Clean Solution**: Removes all build artifacts (bin/obj folders)
- **Restore Packages**: Restores NuGet packages for all projects

### Running the API
- **Run Trading Partner Portal API**: Starts the API on HTTP (localhost:5096)
- **Run Trading Partner Portal API (HTTPS)**: Starts the API on HTTPS (localhost:7096) and HTTP (localhost:5096)

### Testing
- **Run Tests**: Executes all unit and integration tests
- **Watch Tests**: Runs tests in watch mode (automatically re-runs when code changes)

### Validation
- **Validate API Health**: Checks if the API health endpoint is responding
- **Validate API Version**: Checks if the API version endpoint is responding
- **Full Validation Suite**: Runs build, tests, and API validation in sequence

### Utility
- **Stop Background Tasks**: Helper task to remind you how to stop running processes

## How to Use Tasks

### Method 1: Command Palette
1. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
2. Type "Tasks: Run Task"
3. Select the task you want to run

### Method 2: Terminal Menu
1. Go to `Terminal > Run Task...`
2. Select the task you want to run

### Method 3: Keyboard Shortcuts
- `Ctrl+Shift+P` → "Tasks: Run Build Task" → Quick build
- `Ctrl+Shift+P` → "Tasks: Run Test Task" → Quick test

## Debugging

### Launch Configurations
- **Launch API (HTTP)**: Debug the API with HTTP only
- **Launch API (HTTPS)**: Debug the API with HTTPS and HTTP
- **Attach to Process**: Attach debugger to a running process

### How to Debug
1. Set breakpoints in your code
2. Press `F5` or go to `Run > Start Debugging`
3. Choose the appropriate launch configuration
4. The API will start and open Swagger UI automatically

## Development Workflow

### Initial Setup
1. Run `Tasks: Run Task` → `Restore Packages`
2. Run `Tasks: Run Task` → `Build Solution`

### Development Loop
1. Make code changes
2. Run `Tasks: Run Task` → `Build Solution` to check for compilation errors
3. Run `Tasks: Run Task` → `Run Tests` to verify functionality
4. For API testing, run `Tasks: Run Task` → `Run Trading Partner Portal API`
5. Use `Tasks: Run Task` → `Validate API Health` to verify the API is responding

### Continuous Testing
- Use `Tasks: Run Task` → `Watch Tests` to automatically run tests when code changes
- This is helpful during active development

### API Validation
Before committing changes:
1. Run `Tasks: Run Task` → `Full Validation Suite`
2. This will build, test, and validate the API endpoints

## Stopping Running Processes

### Background Tasks (API Server)
- In the Terminal panel, find the terminal running the API
- Press `Ctrl+C` to stop the process

### Watch Tasks (Test Watcher)
- In the Terminal panel, find the terminal running the watch task
- Press `Ctrl+C` to stop the process

## Troubleshooting

### Build Errors
- Check the Problems panel (`Ctrl+Shift+M`) for detailed error information
- Run `Tasks: Run Task` → `Clean Solution` then `Build Solution` to do a clean build

### Test Failures
- Check the Terminal output for detailed test failure information
- Use `Tasks: Run Task` → `Run Tests` to see verbose test output

### API Not Responding
- Ensure no other process is using ports 5096 or 7096
- Check the Terminal output for startup errors
- Use `Tasks: Run Task` → `Validate API Health` to test connectivity

## Port Configuration

- **HTTP**: http://localhost:5096
- **HTTPS**: https://localhost:7096
- **Swagger UI**: Available at both HTTP and HTTPS endpoints with `/swagger` path

## Environment Variables

The API uses the following environment variables:
- `ASPNETCORE_ENVIRONMENT`: Set to "Development" for local development
- `ASPNETCORE_URLS`: Configures the URLs the API listens on

These are automatically set by the VS Code launch configurations and tasks.