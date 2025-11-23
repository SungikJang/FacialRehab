using System.Collections;
using Mediapipe;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;
using UnityEngine;
using UnityEngine.Rendering;

namespace InGameManagers
{
    public interface IMediaPipeManager
    {
        void Init();
        void GetReadyForFaceLandmarker();
    }

    // public class MediaPipeManager
    // {
    //     public readonly FaceLandmarkDetectionConfig config = new FaceLandmarkDetectionConfig();
    //     protected IEnumerator Run()
    //     {
    //         Debug.Log($"Delegate = {config.Delegate}");
    //         Debug.Log($"Image Read Mode = {config.ImageReadMode}");
    //         Debug.Log($"Running Mode = {config.RunningMode}");
    //         Debug.Log($"NumFaces = {config.NumFaces}");
    //         Debug.Log($"MinFaceDetectionConfidence = {config.MinFaceDetectionConfidence}");
    //         Debug.Log($"MinFacePresenceConfidence = {config.MinFacePresenceConfidence}");
    //         Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");
    //         Debug.Log($"OutputFaceBlendshapes = {config.OutputFaceBlendshapes}");
    //         Debug.Log($"OutputFacialTransformationMatrixes = {config.OutputFacialTransformationMatrixes}");
    //
    //         yield return AssetLoader.PrepareAssetAsync(config.ModelPath);
    //
    //         var options = config.GetFaceLandmarkerOptions(config.RunningMode == Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnFaceLandmarkDetectionOutput : null);
    //         taskApi = FaceLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
    //         var imageSource = ImageSourceProvider.ImageSource;
    //
    //         yield return imageSource.Play();
    //
    //         if (!imageSource.isPrepared)
    //         {
    //             Debug.LogError("Failed to start ImageSource, exiting...");
    //             yield break;
    //         }
    //
    //         // Use RGBA32 as the input format.
    //         // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so maybe the following code needs to be fixed.
    //         _textureFramePool = new Mediapipe.Unity.Experimental.TextureFramePool(imageSource.textureWidth,
    //             imageSource.textureHeight, TextureFormat.RGBA32, 10);
    //
    //         // NOTE: The screen will be resized later, keeping the aspect ratio.
    //         screen.Initialize(imageSource);
    //
    //         // SetupAnnotationController(_faceLandmarkerResultAnnotationController, imageSource);
    //
    //         var transformationOptions = imageSource.GetTransformationOptions();
    //         var flipHorizontally = transformationOptions.flipHorizontally;
    //         var flipVertically = transformationOptions.flipVertically;
    //         var imageProcessingOptions =
    //             new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(
    //                 rotationDegrees: (int)transformationOptions.rotationAngle);
    //
    //         AsyncGPUReadbackRequest req = default;
    //         var waitUntilReqDone = new WaitUntil(() => req.done);
    //         var waitForEndOfFrame = new WaitForEndOfFrame();
    //         var result = FaceLandmarkerResult.Alloc(options.numFaces);
    //
    //         // NOTE: we can share the GL context of the render thread with MediaPipe (for now, only on Android)
    //         var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 &&
    //                              GpuManager.GpuResources != null;
    //         using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;
    //
    //         while (true)
    //         {
    //             if (isPaused)
    //             {
    //                 yield return new WaitWhile(() => isPaused);
    //             }
    //
    //             if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
    //             {
    //                 yield return null;
    //                 continue;
    //             }
    //
    //             // Build the input Image
    //             Image image;
    //             switch (config.ImageReadMode)
    //             {
    //                 case ImageReadMode.GPU:
    //                     if (!canUseGpuImage)
    //                     {
    //                         throw new System.Exception("ImageReadMode.GPU is not supported");
    //                     }
    //
    //                     textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally,
    //                         flipVertically);
    //                     image = textureFrame.BuildGPUImage(glContext);
    //                     // TODO: Currently we wait here for one frame to make sure the texture is fully copied to the TextureFrame before sending it to MediaPipe.
    //                     // This usually works but is not guaranteed. Find a proper way to do this. See: https://github.com/homuler/MediaPipeUnityPlugin/pull/1311
    //                     yield return waitForEndOfFrame;
    //                     break;
    //                 case ImageReadMode.CPU:
    //                     yield return waitForEndOfFrame;
    //                     textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally,
    //                         flipVertically);
    //                     image = textureFrame.BuildCPUImage();
    //                     textureFrame.Release();
    //                     break;
    //                 case ImageReadMode.CPUAsync:
    //                 default:
    //                     req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally,
    //                         flipVertically);
    //                     yield return waitUntilReqDone;
    //
    //                     if (req.hasError)
    //                     {
    //                         Debug.LogWarning($"Failed to read texture from the image source");
    //                         continue;
    //                     }
    //
    //                     image = textureFrame.BuildCPUImage();
    //                     textureFrame.Release();
    //                     break;
    //             }
    //
    //             switch (taskApi.runningMode)
    //             {
    //                 case Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE:
    //                     if (taskApi.TryDetect(image, imageProcessingOptions, ref result))
    //                     {
    //                         // _faceLandmarkerResultAnnotationController.DrawNow(result);
    //                     }
    //                     else
    //                     {
    //                         // _faceLandmarkerResultAnnotationController.DrawNow(default);
    //                     }
    //
    //                     break;
    //                 case Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO:
    //                     if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions,
    //                             ref result))
    //                     {
    //                         // _faceLandmarkerResultAnnotationController.DrawNow(result);
    //                     }
    //                     else
    //                     {
    //                         // _faceLandmarkerResultAnnotationController.DrawNow(default);
    //                     }
    //
    //                     break;
    //                 case Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM:
    //                     taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
    //                     break;
    //             }
    //         }
    //     }
    // }
}