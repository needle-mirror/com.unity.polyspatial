# PolySpatial Tests

The purpose of this document is to cover some general guidelines for writing, executing and updating PolySpatial tests.
For any PolySpatial test related questions, feel free to reach out to `manuel.gonzalez@unity3d.com`
## Local Test Scripts
TBD

## Yamato Test Jobs
TBD

## Editor Tests
TBD

## Runtime Tests
### Helper Classes
There are several helper and utility classes available for use when writing new tests. See the following directories below for each available utilities modules and classes.
* `Tests/Runtime/PolySpatialTest/Scripts` - a collection of `MonoBehaviour` scripts that can be used to transform test GameObjects
* `Tests/Runtime/Utils/PolySpatialStateValidator.cs` - a utility class that retrieves, diffs and generates diff strings for PolySpatial GameObject state.
   This class fetches supported GameObject data and diffs between simulation and renderer layers.
* `Tests/Runtime/Utils/ScreenValidator.cs` - a utility class that captures and retrieves renderer content as images for image comparison and validation.

### Unit Tests
TBD

### Integration Tests
#### Scene Rendering Tests
This integration test fixture loads existing Scenes and passes them through image compare and state validation steps.
The primary purpose of this test suite is to quickly test PolySpatial functionality, validating both rendered content via
image comparison and PolySpatial state changes. Contributors can quickly enable new tests by creating a new test Scene for the
desired use case (adding N number of PolySpatial GameObjects, enabling MonoBehaviour scripts, etc) and have the Scene Rendering
Tests fixture pick it up and pass it through its validations.

#### Supported platforms
Currently Scene Rendering Tests run on Unity Editor platforms only (win and osx).

##### Creating Scene Rendering Tests
**Note**: Scene Rendering Tests live in `Packages/com.unity.polyspatial/Tests/Runtime/Rendering/SceneRenderingTests.cs`

To create a new Scene Rendering Tests use case, users can follow these steps to generate new test cases from it:
1. Create a new test Scene under `Tests/Scenes` following naming convention listed in the header of `Tests/Runtime/Rendering/SceneRenderingTests.cs`.
   The naming convention has several important components to keep in mind for use cases:
  * **Base Scene name** - this prefix is the name that gets used when generating the Scene Reference Image names
    * **Unity UI Scenes** - For test Scenes having Unity UI components, such as `Canvas` and `CanvasRenderer`, prefix the Base name with `UGUI` to detect and account for additional Scene loading validation.
  * **Frame count** - this integer value specifies how many frames to run the test Scene through and validate via image compare and state validation for each of those frames.
2. Include the new test Scene in Editor Build Settings (edit `Projects/PolySpatialSamples/ProjectSettings/EditorBuildSettings.asset`)
3. Include the new test Scene in the Scene Rendering Tests fixture by adding it to the static array `s_TestScenes`.
   Be sure to include a description of what the test Scene use case is.
4. Generate reference images via local test runs.
  * Modify `SceneRenderingTests.cs` - set `m_FailOnMissingReferenceImages = false` to disable failing when no reference images are found.
  * Launch the PolySpatialSamples project via `project-launcher.sh` script, and run the Scene Rendering Tests via Test Runner Window. Make sure a test case is listed for the newly added Scene.
  * Once complete, the test run should create a directory under `Packages/com.unity.polyspatial/Tests/Results/` with the same name as the test Scene.
  * Copy the generated folder and its contents over to `Packages/com.unity.polyspatial/Tests/ReferenceImages/`
  * Revert the change made to `SceneRenderingTests.cs`; set `m_FailOnMissingReferenceImages = true`
  * Rerun the Scene Rendering Tests; confirm there aren't any test failures from missing reference images.
5. Once reference images exist, include them in their own unique Scene name directory under `Tests/ReferenceImages`
6. Commit all changes to a feature branch and submit PR.

For example, let's say I want to validate that multiple (3) PolySpatial GameObjects can rotate on screen for 20 frames.
I'll create new Scene `Tests/Scenes/RotatingCubesTest_20_MonoBehaviour.unity`. In it, I'll include GameObjects with
transformation script `Tests/RuntimePolySpatialTest/Scripts/RotateTransform.cs` added to them. Then, I'll edit build settings
to include the newly created Scene, and I'll then open `Tests/Runtime/Rendering/SceneRenderingTests.cs` and edit as follows:

```
        private static string[] s_TestScenes =
        {
            // Test Scene with single PolySpatial Cube that remain static
            "Packages/com.unity.polyspatial/Tests/Scenes/CubeTest_5.unity",
            // Test Scene with single PolySpatial Cube that rotates, moves and scales at each frame
-            "Packages/com.unity.polyspatial/Tests/Scenes/CubeTest_5_MonoBehaviour.unity"
+            "Packages/com.unity.polyspatial/Tests/Scenes/CubeTest_5_MonoBehaviour.unity",
+            // Test Scene with 3 PolySpatial Cubes that all rotate at each frame.
+            "Packages/com.unity.polyspatial/Tests/Scenes/RotatingCubesTest_20_MonoBehaviour.unity"
        };
```
