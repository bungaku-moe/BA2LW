using System;
using System.Reflection;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace BA2LW.Utils
{
    /// <summary>
    /// Spine runtimes helper.
    /// </summary>
    public static class SpineHelper
    {
        /// <summary>
        /// Create SkeletonDataAsset that accept SkeletonData and AnimationStateData as the parameters.
        /// </summary>
        /// <param>skeletonData</param>
        /// <param>stateData</param>
        public static SkeletonDataAsset CreateSkeletonDataAsset(SkeletonData skeletonData, AnimationStateData stateData)
        {
            // Create a new instance of SkeletonDataAsset
            SkeletonDataAsset skeletonDataAsset = ScriptableObject.CreateInstance<SkeletonDataAsset>();

            // Get the type of SkeletonDataAsset
            Type skeletonDataAssetType = skeletonDataAsset.GetType();

            // Get the skeletonData and stateData fields
            FieldInfo skeletonDataField = skeletonDataAssetType.GetField("skeletonData", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo stateDataField = skeletonDataAssetType.GetField("stateData", BindingFlags.NonPublic | BindingFlags.Instance);

            // Set the values of skeletonData and stateData
            skeletonDataField.SetValue(skeletonDataAsset, skeletonData);
            stateDataField.SetValue(skeletonDataAsset, stateData);

            // Set dummy value to skeletonJSON to make sure there's no
            // error returned if we call a method in SkeletonDataAsset
            skeletonDataAsset.skeletonJSON = new TextAsset("BA2LW");

            // Return the SkeletonDataAsset
            return skeletonDataAsset;
        }
    }
}
