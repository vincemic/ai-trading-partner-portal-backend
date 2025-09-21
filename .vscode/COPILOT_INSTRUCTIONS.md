# GitHub Copilot Instructions for Trading Partner Portal

## Development Environment Requirements

**ALWAYS use Visual Studio Code tasks for all development operations. Never use terminal commands directly.**

### Running Processes

**CRITICAL: Always check for running processes before building or testing**

Before any build or test operation, check if the API is currently running:

- If the API is running, DLL files will be locked and builds will fail
- Look for file lock errors mentioning "TradingPartnerPortal.Api" process
- Always stop running API processes before rebuilding

When the user asks to:

- **Build the solution**: First stop any running API, then use Task → "Build Solution"
- **Run tests**: First stop any running API, then use Task → "Run Tests"
- **Start the API**: Use Task → "Run Trading Partner Portal API"
- **Start API with HTTPS**: Use Task → "Run Trading Partner Portal API (HTTPS)"
- **Watch tests**: Use Task → "Watch Tests"
- **Validate API**: Use Task → "Validate API Health" or "Full Validation Suite"

#### Process Check Workflow

1. **Before building/testing**: Check for running API processes
2. **If API is running**: Stop it using `Ctrl+C` in the terminal where it's running
3. **Wait for process to fully stop**: Ensure DLL files are unlocked
4. **Then proceed**: With build/test operations

### Stopping Processes

When the user asks to stop running processes:

- **Background tasks**: Instruct to press `Ctrl+C` in the appropriate terminal panel
- **Never use terminal commands**: Refer to Task → "Stop Background Tasks" for guidance

### Development Workflow

1. **Initial Setup**:
   - Task → "Restore Packages"
   - Task → "Build Solution"

2. **Development Loop**:
   - Make changes
   - Task → "Build Solution" (check compilation)
   - Task → "Run Tests" (verify functionality)
   - Task → "Run Trading Partner Portal API" (test manually)

3. **Validation**:
   - Task → "Full Validation Suite" (comprehensive check)
   - Task → "Validate API Health" (quick health check)

### Debugging

- **Start debugging**: Press `F5` or use VS Code debug launch configurations
- **Launch configurations available**:
  - "Launch API (HTTP)"
  - "Launch API (HTTPS)"
  - "Attach to Process"

### Testing API Endpoints

- **Use REST client**: `.vscode/api-tests.http` file
- **Swagger UI**: Available at `http://localhost:5096/swagger` when API is running
- **Never use curl directly**: Always use the configured tools

### Task Categories

#### Build & Restore

- Build Solution
- Clean Solution  
- Restore Packages

#### Running & Debugging

- Run Trading Partner Portal API
- Run Trading Partner Portal API (HTTPS)
- Launch configurations in VS Code

#### Testing

- Run Tests
- Watch Tests (continuous testing)

#### Validation

- Validate API Health
- Validate API Version
- Full Validation Suite

### Port Configuration

- **HTTP**: `http://localhost:5096`
- **HTTPS**: `https://localhost:7096`
- **Swagger**: Available at both URLs with `/swagger` path

### File Structure Reference

```
.vscode/
├── tasks.json           # All available tasks
├── launch.json          # Debug configurations
├── settings.json        # VS Code settings
├── DEVELOPMENT.md       # Detailed development guide
└── api-tests.http       # REST client test file
```

### When to Use Each Task

| User Request | Task to Use | Notes |
|--------------|-------------|-------|
| "Build the project" | Build Solution | Compiles and shows errors |
| "Run tests" | Run Tests | One-time test execution |
| "Start the API" | Run Trading Partner Portal API | HTTP only |
| "Start with HTTPS" | Run Trading Partner Portal API (HTTPS) | HTTP + HTTPS |
| "Watch for changes" | Watch Tests | Continuous testing |
| "Check if API works" | Validate API Health | Quick health check |
| "Full validation" | Full Validation Suite | Build + Test + Validate |
| "Clean build" | Clean Solution → Build Solution | Remove artifacts first |

### Error Handling

- **Build errors**: Check Problems panel (`Ctrl+Shift+M`)
- **Test failures**: Check Terminal output from test task
- **API errors**: Check Terminal output from API task
- **Port conflicts**: Ensure ports 5096/7096 are available
- **File lock errors**: If you see "The process cannot access the file... because it is being used by another process", stop the running API first

#### Common File Lock Errors

If you encounter errors like:

```
error MSB3027: Could not copy "...TradingPartnerPortal.Application.dll"... 
The file is locked by: "TradingPartnerPortal.Api (process-id)"
```

**Resolution steps**:

1. Find the terminal running the API (usually shows "Run Trading Partner Portal API")
2. Press `Ctrl+C` to stop the API process
3. Wait a few seconds for the process to fully terminate
4. Retry the build or test operation

### Best Practices

1. **Always check for running processes first**: Before build/test operations, ensure the API is not running
2. **Always build before testing**: Tasks have dependencies configured
3. **Use watch mode during development**: "Watch Tests" for continuous feedback
4. **Validate before committing**: "Full Validation Suite"
5. **Stop background tasks properly**: Use `Ctrl+C` in terminal panels
6. **Use REST client for API testing**: Avoid manual curl commands

## Response Templates

When user asks to run something, respond with:

"I'll use the VS Code task for that. Please run:

1. Press `Ctrl+Shift+P`
2. Type "Tasks: Run Task"
3. Select "[Task Name]"

Or use the Terminal menu: Terminal → Run Task → [Task Name]"

When user asks to debug:

"I'll set up debugging for you:

1. Set any breakpoints you need
2. Press `F5` or go to Run → Start Debugging
3. Choose the appropriate launch configuration"

When user asks to test API:

"For API testing, use the REST client file:

1. Open `.vscode/api-tests.http`
2. Click the "Send Request" link above any HTTP request
3. Or start the API with Task → 'Run Trading Partner Portal API' and use Swagger UI"
