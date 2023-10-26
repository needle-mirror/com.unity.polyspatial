---
uid: ps-changelog
---
# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.5.0] - 2023-10-26

### Added
- "Disable Tracking Mask" in PolySpatial settings allows omitting objects from tracking based on their layer at creation.
- Added support for new shader graph nodes: Channel Mixer, Replace Color, White Balance, Fade Transition, Channel Mask, Color Mask, Flip
- Added Trail support for BakeToMesh Particle Systems.
- Added support for RGB/linear conversions to shader graph Colorspace Conversion node.

### Changed
- Fixed a typo in PolySpatialPointerKind: `indDirectPinch` -> `indirectPinch`
- Fixed an issue accessing UV1 in shader graphs.
- Changed the access modifier of the serialized fields in the PolySpatial settings (class `PolySpatialSettings`) from public to private and renamed these members to include the `m_` prefix.
- Renamed the PolySpatial settings `Enable Default Volume Camera` to `Auto-Create Volume Camera`.

### Deprecated

### Removed

### Fixed
- Fixed an issue where deleting a canvas renderer instance at runtime could cause an OOB exception in the tracker.
- Fixed an issue causing incorrect mesh index buffer sizes.
- Fixed shader graph world position node outputs for output volumes with heights greater than 2.
- Fixed inconsistent RGB/HSV conversion in shader graph Colorspace Conversion node.
- Fixed an issue with updating texture contents.
- Fixed invalid transforms for shader graph Transform and Transformation Matrix nodes used in vertex stage.
- Fixed issue with sprite textures wrapping at edges.
- Fixed an issue where `SpatialPointerDevice` events reported the Began phase for more than one frame in a row.

### Security

## [0.4.3] - 2023-10-13

## [0.4.2] - 2023-10-12

## Added
- Documentation for Volume Camera around configuration assets.

## Fixed
- Build error if trying to build for Simulator SDK in Unity prior to 2022.3.11f1.
- Fixed an issue causing incorrect negative scales.
- Sprite performance improvements.
- Improved CanvasRenderer/UI performance. Best performance metrices will only be attained with Unity 2022.3.12f1 or later.

## [0.4.1] - 2023-10-06

## [0.4.0] - 2023-10-04

### Added
- PolySpatial now supports Xcode 15.1 beta 1 and visionOS 1.0 beta 4
- PolySpatial now supports transferring 3D textures and sampling them in shader graphs.
- PolySpatial Lighting Node now supports reflection probes.

### Changed
- Updated dependency on `com.unity.collections` to version 2.1.4 to resolve conflicts with `com.unity.entities`

### Fixed
- Native texture pointers for RenderTextures no longer cached, which may fix issues with RenderTextures that are released and recreated.
- Sprite performance improvements.

## [0.3.2] - 2023-09-18

## [0.3.1] - 2023-09-15

## [0.3.0] - 2023-09-13

## [0.2.2] - 2023-08-28

## [0.2.1] - 2023-08-25

## [0.2.0] - 2023-08-21

### Added
- PolySpatial now supports Xcode 15 beta 5 & 6
- Lighting
    - Adds PolySpatial Lighting node, which allows developers to opt into using Unity's internal lighting in addition to the IBL lighting model provided by RealityKit
    - Lighting parameter shader globals (dynamic lights, light probe volumes, and baked lightmaps) are now accessible via Unity ShaderGraph & MaterialX

- Materials/ShaderGraph/MaterialX
    - Adds limited support for the custom funtion node in Unity ShaderGraph (see documentation for details)
    - Additional materialX nodes are now supported (see documentation for details)
    - Unity PBR materials now map to the RealityKitPBR shader node rather than the UsdPreviewSurface shader node, as the RealityKitPBR supports additional useful features


### Fixed
- Input
    - XRI now works with QuantumTouchSpace
    - Other assorted input fixes
- UI
- Other Bug fixes
    - Native Texture & UI texture fixes
    - Fixes for sprite UVs
    - ARKit meshes now appear in MR mode
    - Fixes null object error related to particle systems
    - Fixes bad reference caused by inactive skinned mesh renderers
    - Fixes crash if mesh collider was disabled at scene load.
    - Other sssorted bug fixes

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
