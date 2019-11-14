# Changelog

All notable changes to this project will be documented in this file.

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