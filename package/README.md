# VAutomationEvents

A powerful automation mod for V Rising that provides automated arena management, player lifecycle handling, and PvP practice functionality.

## Features

- **Automated Arena Zone Management**: Dynamic zone detection and player teleportation
- **Player State Lifecycle**: Complete snapshot-based state management with crash recovery
- **Build System Integration**: Modular loadout management with JSON configuration
- **VBlood Progression**: Automated unlocking of VBlood bosses for testing
- **Real-time Monitoring**: Position-based automatic entry/exit detection
- **Comprehensive Command Framework**: Admin commands for full system control

## Installation

1. Install [BepInEx](https://github.com/BepInEx/BepInEx/releases) for V Rising
2. Install [VampireCommandFramework](https://thunderstore.io/c/v-rising/p/molenzwiebel/VampireCommandFramework/)
3. Download the latest release from [Thunderstore](https://thunderstore.io/c/v-rising/p/Coyoteq1/VAutomationEvents/) or build from source
4. Extract to your `BepInEx/plugins` folder
5. Launch the game

## Usage

### Basic Arena Commands
- `.arena enter` - Enter arena with full unlocks
- `.arena exit` - Exit arena and restore state
- `.arena status` - Show current arena state

### Character Management
- `.charswap` - Swap between characters
- `.char create` - Create PvP character

### Service Management
- `.service enable <servicename>` - Enable service
- `.service disable <servicename>` - Disable service

## Configuration

Configuration files are located in `BepInEx/config/VAuto.Arena/`

## Support

Join our Discord community: https://discord.gg/68JZU5zaq7

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues on GitHub.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Disclaimer

This mod is not affiliated with Stunlock Studios or V Rising. Use at your own risk.
