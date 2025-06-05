## [1.2.2] - 2025-05-05 
### Fixed
- Fixed an issue where the SimPlayer item display when comparing did not show the correct item quality.

## [1.2.1] - 2025-04-22 
### Added
- Added a config option to allow users to change the hotkey used to switch the item slot they are comparing with.

### Fixed
- Fixed an issue where the item value shown in the compare window would not update.

## [1.2.0] - 2025-04-20
### Added
- Implemented Item Info display on hover when looking at a Sim Player's equipment.
- Implemented Equipment comparison with Sim Player's equipment if you are currently inspecting a Sim Player.
  
### Fixed
- Fixed item comparison switching for ring and wrist slots.
- Fixed window flickering when switching an item and the window had been clamped to screen bounds.

## [1.1.0] - 2025-04-18
### Added
- UI windows (Item Info and Compare) now dynamically adapt their position based on screen resolution and aspect ratio.
- Edge clamping added to prevent windows from rendering off-screen on any resolution.

### Fixed
- Previously, the windows only positioned correctly at 2560 x 1440 resolution. This update ensures consistent display across all screen sizes.
