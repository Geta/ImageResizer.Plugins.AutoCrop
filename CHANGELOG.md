# Changelog

All notable changes to this project will be documented in this file.

## [3.0.0]

### Changed
- Removed forced aspect ratio (may affect cropping of all images).
- Added optical weighting of crop.

## [2.1.1]

### Changed
- Optimized sobel filtering.

## [2.1.0]

### Added
- Added edge detection based cropping method.

### Changed
- Debug mode will now render the entire image as the selected analyzer sees it, in addition to the evaluated bounding box.

## [2.0.0]

### Changed
- Major rewrites to cropping, no GDI+ dependencies left.
- Debug drawing.

### Added
- Possibility to integrate with FastScaling plugin.
- Automator project for batching (on GitHub).

### Fixed
- Bug with debug rectangle positioning.
- CMYK image detection (will now skip instead of break).

## [1.4.5]

### Fixed
- Fixed an access violation exception when analyzing images.

## [1.4.4]

### Fixed
- Issue with image scaling.
- Image quality improvements.

## [1.4.3]

### Fixed
- Image quality improvements (delisted).

## [1.4.2]

### Fixed
- Image quality improvements (delisted).

## [1.4.1]

### Fixed
- Problem with rectangle validation on near-edge bounding boxes.

## [1.4.0]

### Added
- Improved image analysis (combined bucket analysis and color thresholding)
- Improved layout capabilities, will now add padding to images that don't have enough whitespace.

## [1.3.0]

### Added
- Added automatic resolution of background color when no ?bgcolor parameter is specified.

### Fixed
- Fixed a problem with RGBA bounding box evaluation.

## [1.2.0]

### Added
- Added ability to override FitMode

## [1.1.5]

### Added
- Additional error handling

## [1.1.4]

### Fixed
- Faulty tolerance calculation when validating rectangle

## [1.1.3]

### Fixed
- Alpha problem when evaluating transparent colors against blacks
- Resolved a bug with negative size bounding boxes

## [1.1.2]

### Fixed
- Performance issue with complex images and late-exit analysis

## [1.1.1]

### Changed
- Changed border edge handling to prioritize centering
- Lowered .NET Framework version from 4.6.1 to 4.5

## [1.1.0]

### Added
- Added alpha channel support

## [1.0.1]

### Fixed
- Fixed conflict with strong name signing

## [1.0.0]

### Added
- Added initial implementation