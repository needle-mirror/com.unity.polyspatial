# Notes on material support

This document contains notes on the materials supported for Polyspatial conversion and their properties.

## Unlit materials

### Universal Render Pipeline/Unlit

For the URP unlit material, Polyspatial supports the `Base Map` color and texture properties as well as the `Opaque` and `Transparent` (with blending mode `Alpha`) surface types.  `Alpha Clipping` may be enabled; if the `Threshold` is greater than zero, no blending will be performed (only alpha testing).

### Unlit/Color

For the built-in renderer pipeline unlit color material, the `Main Color` property is supported.

### Unlit/Texture

For the built-in renderer pipeline unlit texture material, the `Base (RGB)` texture is supported.

### Unlit/Transparent

For the built-in renderer pipeline unlit transparent material, the `Base (RGBA)` texture is supported.

## Lit materials

### Universal Render Pipeline/Lit

For the URP lit material, the `Base Map` color and texture are respected, as is the `Render Face` option and the `Surface Inputs` `Tiling` and `Offset` properties.

When in the `Specular` workflow and `Specular Highlights` are enabled, Polyspatial supports the `Specular Map` texture or the (grayscale) intensity of the associated color.  When in the `Metallic` workflow, Polyspatial supports the `Metallic Map` texture or intensity and respects the `Specular Highlights` toggle.

For both workflows, the `Smoothness` intensity is respected, but smoothness from texture channel (e.g., from `Metallic Alpha`) is not supported.  `Normal Map`[^1] is supported, but the normal scale is not.  Likewise, `Occlusion Map` is supported, but occlusion intensity is not.  `Emission` color *or* texture may be specified; if they are both given, the color is reduced to grayscale and acts as a multiplier.

The `Opaque` and `Transparent` (with `Alpha` blend mode) surface types are supported.  In `Transparent` mode, the `Preserve Specular` flag is respected.  `Alpha Clipping` may be enabled; if the `Threshold` is greater than zero, no blending will be performed (only alpha testing). 

### Universal Render Pipeline/Simple Lit

For the URP simple lit material, the options supported are the same as for the lit material, except that there are no `Metallic` properties and no `Occlusion Map`.  

### Universal Render Pipeline/Complex Lit

For the URP complex lit material, the options supported are the same as for the lit material, with the addition of the `Clear Coat` option and its `Mask` and `Smoothness` properties.

### Standard

The built-in standard lit material is supported in much the same way as the URP lit material in `Metallic` workflow.  The `Albedo` texture and color are similarly respected, as is the `Metallic` map or intensity and the `Smoothness` intensity (but not the smoothness `Source`).  `Normal Map`[^1] and `Occlusion` are supported (but not their corresponding intensities), as is `Emission` color or texture, `Main Maps` `Tiling` and `Offset`, and the `Specular Highlights` flag.  All rendering modes are supported: `Opaque`, `Transparent`, `Fade`, and `Cutout`.

### Standard (Specular setup)

The built-in lit specular material is supported in the same way as the URP lit material in `Specular` workflow.  The `Albedo` texture and color are similarly respected, as is the `Specular` map or (grayscale) intensity and the `Smoothness` intensity (but not the smoothness `Source`).  `Normal Map`[^1] and `Occlusion` are supported (but not their corresponding intensities), as is `Emission` color or texture, `Main Maps` `Tiling` and `Offset`, and the `Specular Highlights` flag.  All rendering modes are supported: `Opaque`, `Transparent`, `Fade`, and `Cutout`.

[^1]: Currently, normal maps used in non-shader-graph materials must be imported as the `Default` texture type--that is, *not* the `Normal map` type--with the `sRGB (Color Texture)` option unchecked.

## Special purpose materials

### TextMeshPro/Distance Field
### TextMeshPro/Mobile/Distance Field

The TMP distance field materials are converted to shader graph materials that respect the `Face Color` and `Face Texture` properties only.

### UI/Default

The UI default material is converted to an unlit material that respects the tint and texture properties.

### Universal Render Pipeline/Particles/Unlit

The URP unlit particles material is converted to an unlit material that respects the `Base Map` texture and color and the `Surface Type`.

### AR/Basic Occlusion
### AR/Occlusion

The occlusion materials are converted to basic equivalents.

## Shader graph materials

Shader graphs may use the Builtin or URP targets and the Unlit or Lit materials.  All output blocks are supported.  For more information about shader graph support, see the [shader graph conversion notes](Editor/ShaderGraphConversion/NOTES.md)