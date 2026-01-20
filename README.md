# VAutomationEvents - Enhanced Automation System

## 🎯 Overview

VAutomationEvents is a comprehensive automation system for V Rising, built with modern architecture and enhanced logging capabilities. This version represents a complete cleanup and enhancement of legacy codebase, providing users with a streamlined, powerful automation experience.

## 🏗️ Architecture

### Clean Structure
The project follows a clean, modular architecture designed for scalability and maintainability:

```
VAutomationEvents/
├── API/                    # External interfaces and contracts
├── Automation/             # Core automation logic and workflows
├── Commands/               # User command handlers
│   ├── Arena/             # Arena-specific commands
│   ├── Automation/        # Automation system commands
│   ├── Character/         # Character management commands
│   ├── Dev/               # Development and debugging commands
│   ├── Player/            # Player interaction commands
│   ├── Utilities/        # Utility commands
│   ├── World/             # World manipulation commands
│   └── Zone/              # Zone management commands
├── Core/                   # Core system components and abstractions
├── Data/                   # Data access layer and models
├── Extensions/             # Extension methods and utilities
├── Patches/               # Game patches and hooks
├── Services/              # Business logic services
│   ├── Interfaces/        # Service interfaces
│   ├── Lifecycle/         # Lifecycle management services
│   ├── Snapshot/          # State snapshot services
│   ├── Systems/           # System-specific services
│   └── World/             # World interaction services
├── UI/                    # User interface components
├── Utilities/             # Helper utilities and tools
├── config/                # Configuration files
├── docs/                  # Documentation
├── libs/                  # External libraries
├── plans/                 # Development plans and specifications
├── scripts/               # Build and deployment scripts
└── archive/               # Legacy code archive
```

## 🚀 Key Features

### 1. **Modular Design**
- **Separation of Concerns**: Each module has a single responsibility
- **Loose Coupling**: Components interact through well-defined interfaces
- **High Cohesion**: Related functionality is grouped together

### 2. **Service-Oriented Architecture**
- **Dependency Injection**: Modern DI container for service management
- **Interface-Based Design**: All services implement clear interfaces
- **Lifecycle Management**: Automatic service initialization and cleanup

### 3. **Enhanced Logging System**
- **Comprehensive Visibility**: All services log with complete detail
- **Service Organization**: Clear service identification in logs
- **Status Tracking**: Complete/incomplete/failed states visible
- **Real-time Monitoring**: Live service status and operations

### 4. **Command System**
- **Categorized Commands**: Commands organized by functionality
- **Context-Aware**: Commands have access to execution context
- **Extensible**: Easy to add new commands and categories

### 5. **Configuration Management**
- **JSON-Based Configuration**: Modern, human-readable configuration
- **Environment Support**: Different configs for different environments
- **Hot Reload**: Configuration changes without restart

## 📚 User Guide

### Quick Start

1. **Installation**
   ```bash
   # Clone repository
   git clone https://github.com/Coyoteq1/VAutomationEvents.git
   cd VAutomationEvents

   # Restore dependencies
   dotnet restore

   # Build project
   dotnet build
   ```

2. **Configuration**
   - Edit `config/appsettings.json` for basic settings
   - Add `config/plugins.json` for plugin configuration
   - Adjust logging levels as needed

3. **Running**
   ```bash
   dotnet run
   ```

### Basic Commands

#### Arena Commands
- `.arena enter` - Enter arena zone
- `.arena exit` - Exit arena zone
- `.arena status` - Check arena status

#### Automation Commands
- `.automation start` - Start automation systems
- `.automation stop` - Stop automation systems
- `.automation status` - Check automation status

#### Character Commands
- `.character create` - Create new character
- `.character switch` - Switch between characters
- `.character stats` - View character statistics

#### Utility Commands
- `.help` - Show available commands
- `.status` - Show system status
- `.config reload` - Reload configuration

## 🔧 Configuration

### appsettings.json Structure
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "VAutomationEvents": "Debug"
    }
  },
  "VAutomationEvents": {
    "Arena": {
      "Enabled": true,
      "Center": { "X": -1000, "Y": -5, "Z": -500 },
      "Radius": 50
    },
    "Automation": {
      "Enabled": true,
      "UpdateInterval": 30
    }
  }
}
```

### Logging Configuration
The system uses comprehensive logging with:
- **Service Organization**: Each service logs with clear identification
- **Complete Visibility**: All log levels enabled (Debug, Info, Warning, Error)
- **Status Tracking**: Real-time service status and completion states
- **Harmony Integration**: Full patch and hook logging

## 🏛️ Service Architecture

### Core Services

#### 1. Plugin Manager
- **Purpose**: Manages plugin lifecycle and dependencies
- **Features**: Dynamic loading, unloading, and hot reload

#### 2. Command Registry
- **Purpose**: Registers and executes user commands
- **Features**: Command discovery, validation, and execution

#### 3. Configuration Service
- **Purpose**: Manages application configuration
- **Features**: Hot reload, environment-specific settings

#### 4. Logging Service
- **Purpose**: Provides structured logging with service organization
- **Features**: Multiple providers, log levels, filtering, status tracking

### Specialized Services

#### Arena Service
- **Purpose**: Manages arena zones and player interactions
- **Features**: Zone detection, player management, state tracking

#### Automation Service
- **Purpose**: Handles automated tasks and workflows
- **Features**: Task scheduling, event handling, state management

#### Character Service
- **Purpose**: Manages player characters and progression
- **Features**: Character creation, switching, statistics

## 🧩 Development Guide

### Adding New Commands

1. **Create Command Class**
   ```csharp
   using VAutomationEvents.Commands.Interfaces;

   namespace VAutomationEvents.Commands.MyCategory
   {
       public class MyCommands : ICommandCategory
       {
           public string Category => "MyCategory";

           [Command("mycommand")]
           public void MyCommand(CommandContext context, string parameter)
           {
               context.Reply("Command executed!");
           }
       }
   }
   ```

2. **Register Command**
   ```csharp
   // In Program.cs or service registration
   services.AddTransient<ICommandCategory, MyCommands>();
   ```

### Adding New Services

1. **Define Interface**
   ```csharp
   public interface IMyService
   {
       Task DoWorkAsync();
   }
   ```

2. **Implement Service**
   ```csharp
   public class MyService : IMyService
   {
       private readonly ILogger<MyService> _logger;

       public MyService(ILogger<MyService> logger)
       {
           _logger = logger;
       }

       public async Task DoWorkAsync()
       {
           _logger.LogInformation("Doing work...");
           // Implementation here
       }
   }
   ```

3. **Register Service**
   ```csharp
   services.AddSingleton<IMyService, MyService>();
   ```

## 📊 Performance Considerations

### Optimization Features
- **Lazy Loading**: Services loaded only when needed
- **Caching**: Intelligent caching for frequently accessed data
- **Async Operations**: Non-blocking operations where possible
- **Resource Management**: Proper disposal and cleanup

### Monitoring
- **Health Checks**: Built-in service health monitoring
- **Metrics**: Performance metrics and counters
- **Logging**: Structured logging for debugging
- **Diagnostics**: Runtime diagnostics and profiling

## 🛡️ Security

### Built-in Security
- **Input Validation**: All user inputs validated
- **Permission System**: Role-based access control
- **Audit Logging**: All actions logged for security
- **Error Handling**: Secure error reporting

### Best Practices
- **Principle of Least Privilege**: Minimal required permissions
- **Secure Defaults**: Secure configuration by default
- **Regular Updates**: Keep dependencies updated
- **Security Audits**: Regular security reviews

## 🔄 Migration from Legacy

### What Changed
- **Cleaner Structure**: Reorganized into logical modules
- **Modern Patterns**: Updated to use modern .NET patterns
- **Better Configuration**: JSON-based configuration system
- **Enhanced Logging**: Structured logging with service organization
- **Archive System**: Legacy code preserved in `/archive`

### Migration Steps
1. **Backup**: Old version automatically archived in `/archive`
2. **Update Configuration**: Convert old config to new JSON format
3. **Update Commands**: Command structure remains similar
4. **Test**: Verify all functionality works as expected

## 🤝 Contributing

### Development Setup
1. Clone repository
2. Install .NET SDK (latest version)
3. Run `dotnet restore`
4. Use Visual Studio or VS Code for development

### Code Standards
- **C# Conventions**: Follow Microsoft C# coding conventions
- **Comments**: XML documentation for public APIs
- **Tests**: Unit tests for all new features
- **PRs**: Clean, focused pull requests

### Testing
- **Unit Tests**: `dotnet test`
- **Integration Tests**: `dotnet test --filter Category=Integration`
- **Performance Tests**: `dotnet test --filter Category=Performance`

## 📝 Changelog

### v2.0.0 - Enhanced Architecture Release
- ✅ Complete codebase cleanup and reorganization
- ✅ Modern .NET architecture with dependency injection
- ✅ JSON-based configuration system
- ✅ Enhanced logging with service organization
- ✅ Improved command system with categorization
- ✅ Archive of legacy code for reference
- ✅ Comprehensive service visibility and status tracking

### Previous Versions
- See `/archive` directory for legacy versions
- Legacy code preserved for reference and rollback

## 📞 Support

### Getting Help
- **Documentation**: Check `/docs` directory
- **Issues**: Report bugs via GitHub issues
- **Community**: Join our Discord server
- **Examples**: See `/examples` directory

### Troubleshooting
- **Logs**: Check application logs for errors
- **Configuration**: Verify configuration syntax
- **Dependencies**: Ensure all dependencies are installed
- **Permissions**: Check file and directory permissions

---

## 🔗 Repository & Community

### GitHub Repository
📍 **Official Repository**: [https://github.com/Coyoteq1/VAutomationEvents](https://github.com/Coyoteq1/VAutomationEvents)

**Repository Details:**
- **Owner**: Coyoteq1
- **License**: [Your License Here]
- **Stars**: ⭐ [Check GitHub]
- **Forks**: 🍴 [Check GitHub]
- **Issues**: 🐛 [Report Issues]
- **Pull Requests**: 🔄 [Contribute]

### V Rising Modding Community
💬 **Discord Server**: [https://discord.gg/xcN6H6ep](https://discord.gg/xcN6H6ep)

**Community Features:**
- **Support Channels**: Get help with installation and usage
- **Development Discussion**: Collaborate on new features
- **Bug Reporting**: Report issues and get assistance
- **Feature Requests**: Suggest new functionality
- **Announcements**: Stay updated with latest releases

## 🎉 Summary

This enhanced version of VAutomationEvents provides:

✅ **Clean Architecture** - Modern, maintainable code structure
✅ **Better Organization** - Logical grouping of functionality
✅ **Enhanced Configuration** - JSON-based, environment-aware config
✅ **Improved Logging** - Comprehensive service organization and visibility
✅ **Archive System** - Legacy code preserved for reference
✅ **Modern Patterns** - Up-to-date .NET practices and patterns

The system is now easier to use, maintain, and extend while preserving all the powerful automation capabilities users expect from VAutomationEvents.

---

**📦 Installation**
```bash
git clone https://github.com/Coyoteq1/VAutomationEvents.git
cd VAutomationEvents
dotnet restore
dotnet build
```

**📖 Documentation**: [README.md](README.md) | [Wiki](https://github.com/Coyoteq1/VAutomationEvents/wiki)

**🐛 Issues**: [GitHub Issues](https://github.com/Coyoteq1/VAutomationEvents/issues)

**💡 Contributing**: [CONTRIBUTING.md](CONTRIBUTING.md)

**📜 License**: [LICENSE](LICENSE)

---

**© 2026 VAutomationEvents - Enhanced Automation System for V Rising**
**Version**: 2.0.0 | **Status**: ✅ Production Ready | **Last Updated**: January 17, 2026