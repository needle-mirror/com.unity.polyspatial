# Notes on shader graph conversion support

This document contains notes on the process of converting shader graphs to MaterialX, including a list of supported nodes and their various caveats.

## Artistic

### Adjustment

* `Contrast`
    * Colors may not be consistent.
* `Hue`
    * Colors may not be consistent.
* `Saturation`
    * Colors may not be consistent.

### Blend

* `Blend`
    * Supports Difference, Subtract, Burn, Dodge, Linear Dodge, Overlay, Screen, Overwrite, Negation, Multiply

### Filter

* ~~`Dither`~~
    * Requires simulation of Screen Space Position.
    * Doesn't work at the moment due to bug with curvelookup node definitions.

### Normal

* `Normal Blend`
* `Normal From Height`
    * Tangent mode requires tangent space is available on target platform.
* `Normal Reconstruct Z`
* `Normal Strength`
* `Normal Unpack`

### Utility

* `Colorspace Conversion`
    * Not consistent--linear conversions not implemented.

## Channel

* `Combine`
* `Split`
* `Swizzle`

## Input

* `Custom Interpolator`
    * Limited to specific names/types:
    * `Color`: Vector4
    * `UV0`: Vector2
    * `UV1`: Vector2
    * `UserAttribute`: Vector4

### Basic

* `Boolean`
* `Color`
* `Constant`
* `Integer`
* `Slider`
* `Time`
* `Float`
* `Vector2`
* `Vector3`
* `Vector4`

### Geometry

* `Bitangent Vector`
    * Tangent and View space options are not standard.
* `Normal Vector`
    * Tangent and View space options are not standard.
* `Position`
    * Tangent and View space options are not standard.
* `Screen Position`
* `Tangent Vector`
    * Tangent and View space options are not standard.
* `UV`
* `Vertex Color`
* `Vertex ID`
* `View Direction`

### Gradient

* `Gradient`
* `Sample Gradient`

### Lighting

* `Main Light Direction`

### Matrix

* ~~`Matrix 3x3`~~
    * Doesn't work (due to bug in constant matrix node definitions?)
* ~~`Matrix 4x4`~~
    * Doesn't work (due to bug in constant matrix node definitions?)
* `Matrix Construction`
* `Transformation Matrix`
    * Tangent and View space options are not standard.

### Scene

* `Camera`
    * `Position` and `Direction` outputs supported (non-standard).
* `Object`
* `Scene Depth`
    * We don't have access to the depth buffer, this is just the camera distance in either clip or view space.
* `Screen`

### Texture

* `Sample Texture 2D`
* `Sample Texture 2D LOD`
* `Sampler State`
    * `MirrorOnce` wrap mode not supported. 
* `Texture 2D Asset`
* `Texture Size`

## Math

### Advanced

* `Absolute`
* `Exponential`
* `Length`
* `Log`
* `Modulo`
* `Negate`
* `Normalize`

### Basic

* `Add`
* `Divide`
* `Multiply`
* `Power`
* `Square Root`
* `Subtract`

### Interpolation

* `Inverse Lerp`
* `Lerp`
* `Smoothstep`

### Matrix

* `Matrix Determinant`
    * Will flag as unsupported if using Matrix2.
* `Matrix Transpose`
    * Will flag as unsupported if using Matrix2.

### Range

* `Clamp`
* `Fraction`
* `Maximum`
* `Minimum`
* `One Minus`
* `Random Range`
* `Remap`
* `Saturate`

### Round

* `Ceiling`
* `Floor`
* `Round`
* `Sign`
* `Step`

### Trigonometry

* `Arccosine`
* `Arcsine`
* `Arctangent`
* `Arctangent2`
* `Cosine`
* `Sine`
* `Tangent`

### Vector

* `Cross Product`
* `Distance`
* `Dot Product`
* `Fresnel Effect`
* `Reflection`
* `Rotate About Axis`
* `Transform`
    * Some spaces are simulated and not covered in tests.

### Wave

* `Triangle Wave`

## Procedural

### Noise

* `Gradient Noise`
    * Can't be certain that target platform noise functions will behave the same.
    * Frequency is currently off (scale is mapped to amplitude rather than frequency).
* `Voronoi`
    * Can't be certain that target platform noise functions will behave the same.

### Shapes

* `Ellipse`

## Utility

* `Preview`
* `Split LR`
    * Non-standard shader graph node specific to PolySpatial.  Implements the splitlr function as described in the MaterialX spec: https://materialx.org/assets/MaterialX.v1.38.Spec.pdf

### Logic

* `Branch`
* `Comparison`
* `Or`

## UV

* `Flipbook`
* `Rotate`: Only Degrees
* `Tiling and Offset`
* `Triplanar`