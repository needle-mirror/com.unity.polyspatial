# Polyspatial Lifetime Management

Polyspatial tracks the lifetimes of and changes to Unity objects to make sure that all platforms connected to an instance are updated correctly. Polyspatial cares about two specific types of Unity objects in regards to lifetime management: Assets and Components. In each case it is important that the developer correctly track and cleanup instances of these as necessary.
 
In all cases it is important that any tracked item is correctly deleted. This means calling `Object.Destroy` or, when running in Editor, `Object.DestroyImmediate`. For instances that are not created by the Polyspatial developer, it is up to the Unity developer to do this. For instances that are created by the Polyspatial developer, it is their responsibility to do this in a timely manner. This is the only way to guarantee that `UnityEngine.Object` instances are cleaned up when we want them to be. If these are not called, then Unity will eventually clean up the resources at some point in the future, usually at scene unload. If this happens then we will "leak" all Polyspatial representations or created data until that time.

## Assets

Assets are simply `UnityEngine.Object` instances that are added to the `AssetManager`. Putting these instances into Polyspatial asset management implies that Polyspatial depends on Unity to notify us when that asset is updated or destroyed. Registering the asset using `AssetManager.Register` will set the initial reference count of the asset to 2, one for Polyspatial and one for Unity. The asset is not considered completely removed until all of the Polyspatial owning reference counts are gone and Unity has told us that the asset has been deleted. This requires the caller to have implemented a correct change management in Unity and the `AssetManager` to handle update and delete notifications.

For Unity, a new type needs to be defined for tne `Type` enum within the `AssetNotificationSystem` and instances of that type need to be hooked up correctly to tell the notification system of any changes for those asset types. You can see examples of this for both `Mesh` and `Material` in `AssetNotificationSystem`.

Within Polyspatial, the `AssetManager` needs to be updated to add data handling for type changes, subscription for that type with the `AssetNotificationSystem` and functionality for retrieving notifications from the `AssetNotificationSystem` and processing those notifications. You can see examples of this for both `Mesh` and `Material` in `AssetManager`.

### Custom Asset
Adding a new type to `s_TrackedAssets` in `AssetManager` will treat the new asset type as a Unity asset and enable tracking in the `AssetNotificationSystem` for it. Afterwards, asset changes be be processed in `AssetManager::ProcessChanges()` and pooled into a list for Changed/Deleted assets. 

Of note, for custom assets that follow this lifecycle, Creation/Deletion should be picked up by the `AssetNotificationSystem` but updates to custom assets may not be picked up. In that case, they can be manually marked for updating using `ObjectBridge.MarkObjectChanged()`.

## Components

Components are any instances of objects that are derived dfrom `UnityEngine.Component` and attached to a `UnityEngine.GameObject`. These are `UnityEngine.Object` instances and so follow some of the same rules as assets. The biggest difference being in how they are tracked and changes/deletions are notified. 

When a developer wants to track some type based off `UnityEngine.Component` for change or deletion tracking, then need to provide an implementation of `UnityObjectTracker` and add overrides for `TransferObjectData` and `CleanUpDestroyedDataObject`. The first allows the developer update Polyspatial data with changes to the component, either at edit or at runtime. This includes registering assets, creating untracked registered assets or doing any other data management for the updated component.

The latter is where the developer must unregister all items they registered in the former, as well as destroying any untracked assets that where created. All asset ids for assets that were unregistered must be set invalid to make sure no one trys to use the data before the data instance is cleaned up.
