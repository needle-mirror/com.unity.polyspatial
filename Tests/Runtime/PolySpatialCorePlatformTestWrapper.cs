using System;
using FlatSharp.Runtime.Extensions;
using Tests.Runtime.Functional.Components;
using Unity.Collections;
using Unity.PolySpatial.Internals;

namespace Tests.Runtime.Functional
{
    /// <summary>
    /// This class implements a command handler that allows for before/after callbacks around
    /// certain message sends.
    ///
    /// If you look at <see cref="ComponentTestBase"/> you can see how it's used to insert
    /// itself into the active pipeline.
    ///
    /// Usage example:
    /// <example>
    ///
    /// wrapper.OnBeforeAssetsDeletedCalled = (assetIds) =>
    /// {
    ///     ....Test expected pre conditions...
    /// };
    ///
    ///
    /// wrapper.OnAfterAssetsDeletedCalled = (assetIds) =>
    /// {
    ///     ....Test expected post conditions...
    /// };
    /// </example>
    ///
    /// </summary>
    class PolySpatialCorePlatformTestWrapper : IPolySpatialCommandHandler, IPolySpatialCommandDispatcher
    {
        public IPolySpatialCommandHandler NextHandler { get; set; }

        public unsafe void HandleCommand(PolySpatialCommand cmd, int argCount, void** argValues, int* argSizes)
        {
            if (cmd == PolySpatialCommand.DeleteAsset)
            {
                PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialAssetID* aid);
                var buf = PolySpatialUtils.GetNativeArrayForBuffer<PolySpatialAssetID>(aid, 1);

                OnBeforeAssetsDeletedCalled?.Invoke(buf);
                NextHandler?.HandleCommand(cmd, argCount, argValues, argSizes);
                OnAfterAssetsDeletedCalled?.Invoke(buf);
            }
            else
            {
                NextHandler?.HandleCommand(cmd, argCount, argValues, argSizes);
            }
        }

        public Action<NativeArray<PolySpatialAssetID>> OnBeforeAssetsDeletedCalled;
        public Action<NativeArray<PolySpatialAssetID>> OnAfterAssetsDeletedCalled;

        public PolySpatialCorePlatformTestWrapper(IPolySpatialCommandHandler backendHandler)
        {
            NextHandler = backendHandler;
        }
    }
}
