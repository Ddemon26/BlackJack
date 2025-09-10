# BlackJack Game Application

A comprehensive, feature-rich BlackJack (21) game implementation built with C# and .NET 8. This console-based application provides an authentic casino-style BlackJack experience with advanced features including multi-player support, sophisticated betting systems, and detailed statistics tracking.

## Features

### 🃏 Complete BlackJack Implementation
- **Authentic game rules** with standard BlackJack mechanics
- **Natural BlackJack recognition** (Ace + 10-value card pays 3:2)
- **Intelligent Ace handling** with automatic optimization (1 or 11)
- **Dealer AI** following standard casino rules (hits on soft 17)

### 🎮 Advanced Player Actions
- **Double Down** on eligible hands (9, 10, or 11)
- **Split Pairs** with independent hand management
- **Standard actions**: Hit, Stand, Insurance (where applicable)
- **Strategic gameplay** support for optimal BlackJack strategies

### 💰 Comprehensive Betting System
- **Bankroll management** with customizable starting amounts
- **Bet validation** preventing over-betting
- **Realistic payouts** including 3:2 BlackJack bonuses
- **Session-based** betting with persistent bankroll tracking

### 👥 Multi-Player Support
- **Up to 7 players** per game session
- **Individual bankroll tracking** for each player
- **Independent decision making** for each hand
- **Fair dealing** with proper turn management

### 📊 Statistics & Analytics
- **Comprehensive game statistics** (wins, losses, pushes, BlackJacks)
- **Performance metrics** including win percentages
- **Session summaries** with detailed breakdowns
- **Export functionality** for external analysis
- **Historical data** preservation across sessions

### ⚙️ Configurable Game Rules
- **Deck count** (1-8 decks)
- **Player limits** (1-7 players)
- **Rule toggles** (split, double down permissions)
- **Dealer behavior** (soft 17 hitting rules)
- **Shoe management** with customizable penetration thresholds

### 🖥️ Professional CLI Interface
- **Intuitive command structure** with comprehensive help system
- **Clear game state display** with formatted card representations
- **Error handling** with helpful user guidance
- **Color-coded output** for enhanced readability
- **Consistent formatting** throughout the application

## Installation

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

### Setup
1. **Clone the repository**
   ```bash
   git clone https://github.com/Ddemon26/BlackJack.git
   cd BlackJack
   ```

2. **Build the application**
   ```bash
   dotnet build
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

## Usage

### Quick Start
```bash
# Start a BlackJack game
dotnet run -- --blackjack

# View available commands
dotnet run -- --help

# Show current configuration
dotnet run -- --config show

# View player statistics
dotnet run -- --stats show
```

### Available Commands

| Command | Description |
|---------|-------------|
| `--blackjack` | Start an interactive BlackJack game |
| `--config` | Manage game configuration and rules |
| `--stats` | View and manage player statistics |
| `--test` | Run system diagnostics and tests |
| `--help` | Show detailed help information |
| `--version` | Display version and system information |
| `--diagnostics` | Show comprehensive system diagnostics |

### Configuration Management
```bash
# Show current settings
dotnet run -- --config show

# Set number of decks (1-8)
dotnet run -- --config set decks 6

# Enable/disable splitting
dotnet run -- --config set split true

# Reset to defaults
dotnet run -- --config reset
```

### Statistics Management
```bash
# Show all player statistics
dotnet run -- --stats show

# Show specific player stats
dotnet run -- --stats show PlayerName

# Export statistics to file
dotnet run -- --stats export stats.json

# Reset player statistics
dotnet run -- --stats reset PlayerName
```

## Architecture

The application follows Clean Architecture principles with clear separation of concerns:

```
src/
├── Domain/          # Core business logic and entities
│   ├── Entities/    # Game entities (Card, Hand, Player, etc.)
│   ├── ValueObjects/# Value objects (Money, CardRank, etc.)
│   ├── Services/    # Domain services
│   └── Interfaces/  # Domain interfaces
├── Application/     # Application logic and orchestration
│   ├── Services/    # Application services
│   ├── Models/      # DTOs and view models
│   └── Interfaces/  # Application interfaces
├── Infrastructure/ # External concerns (file I/O, etc.)
└── Presentation/   # User interface logic
```

### Key Components
- **Game Service**: Core game logic and rule enforcement
- **Betting Service**: Bankroll and wager management
- **Statistics Service**: Performance tracking and analytics
- **Session Manager**: Game session lifecycle management
- **Configuration Manager**: Settings and customization
- **Error Handler**: Comprehensive error management

## Development

### Building from Source
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run specific test category
dotnet test --filter Category=Unit
```

### Project Structure
- **GroupProject.csproj**: Main project file
- **Program.cs**: Application entry point with CLI handling
- **src/**: Source code organized by architectural layer
- **tests/**: Comprehensive test suite including unit and integration tests

### Running Tests
The application includes extensive testing coverage:
```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity detailed

# Run user acceptance tests
dotnet test --filter FullyQualifiedName~UserAcceptance
```

## Configuration

The application supports various configuration options stored in `appsettings.json` or set via CLI:

- **Game Rules**: Number of decks, player limits, action permissions
- **Betting**: Default bankroll amounts, minimum/maximum bets
- **Display**: Card formatting, color schemes, output verbosity
- **Statistics**: Tracking preferences, export formats

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines
- Follow Clean Architecture principles
- Maintain comprehensive test coverage
- Use descriptive commit messages
- Document public APIs
- Ensure cross-platform compatibility

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Authors

- **Damon Fedorick** - *Initial work* - [Ddemon26](https://github.com/Ddemon26)

## Acknowledgments

- Built with modern C# and .NET 8
- Follows Clean Architecture and SOLID principles
- Implements authentic BlackJack rules and strategies
- Designed for educational and entertainment purposes
