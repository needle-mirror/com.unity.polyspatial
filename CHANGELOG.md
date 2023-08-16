---
uid: ps-changelog
---
# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.1.2] - 2023-08-16

### Added
- `WorldTouchState.colliderObject`, which provides the GameObject targeted by input. 

### Fixed

- Crash when loading a scene containing a disabled mesh collider.
- In-use skinned meshes were removed from scenes when calling `Resourced.UnloadUnusedAssets()`. 
- Project Validation entries not displaying when targeting visionOS. These validation entries no longer appear when targeting Standalone, iOS, or Android.
- Samples: Input now uses `worldTouch.colliderObject` to identify the hit object, fixing issues where input would occasionally miss the targeted object. 
- Samples: Objects reset position after being manipulated. 
- Samples: Missing script warnings resolved. 
- Samples: Settings assets removed from samples, fixing settings-related issues. 
- Samples: Samples now import to the Assets/Samples/ folder rather than Assets/ directly. 
- Documentation: Fixed broken links.
- Documentation: Component reference images displaying. 
- Documentation: Various typo fixes and clarity improvements.

## [0.1.0] - 2023-07-19

## [0.0.4] - 2023-07-18

## [0.0.3] - 2023-07-18

## [0.0.2] - 2023-07-17

### Added
- Initial PolySpatial package.

### Changed (from most recent pre-release)
- The codename `Quantum` has been changed to its release name `PolySpatial` with corresponding name changes throughout the package. 

- The volume camera, previously called `QuantumVolumeCamera` is now just called `VolumeCamera` and existing instances will need to be fixed. Other changed class names include:
    - `QuantumHoverEffect` is now `PolySpatialHoverEffect`

- The settings asset has been renamed, so you will need to recreate your `PolySpatial` project settings.
