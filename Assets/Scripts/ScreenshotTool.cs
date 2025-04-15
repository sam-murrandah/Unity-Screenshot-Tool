/*
Made by Samuel Murrandah

AI Declaration:
Generative AI was used for editing and organisation such as reordering functions as well as some comments.
All code and logic was created and written by me
*/

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics; //This is for the actual saving of the image
using System.Threading.Tasks; // This is pretty much only for the countdown
using static PostProcessingEffects;

public class ScreenshotTool : EditorWindow
{
    #region Variables and Settings
    // Basic Settings
    public string folderPath = Path.Combine(Application.dataPath, "Screenshots");
    public string[] formats = { "PNG", "JPEG", "EXR" };
    public int selectedFormat = 0;
    public int resolutionMultiplier = 1;
    public string fileTag = "Screenshot";
    public int captureDelay = 0;
    public Texture2D watermark;
    public bool flashEnabled = true;  // Flash toggle setting
    public bool livePreviewEnabled;
    public Vector2 aspectRatio = new Vector2(16, 9);



    // Post-Processing Settings
    public bool radiationMode = false;
    public float vignetteIntensity = 0f;
    public float noiseAmount = 0f;
    public float saturationLevel = 1f; // Default to 1 (normal saturation)
    public Effect selectedEffect = Effect.None;
    public ColourblindMode colourblindMode = ColourblindMode.Normal;
    public Color tintColor = Color.white; // Default to no tint (white)


    // UI State
    private RenderTexture previewTexture;
    private Vector2 scrollPosition = Vector2.zero;
    private bool showSaveSettings = true;
    private bool showCaptureSettings = true;
    private bool showAdvancedSettings = false;
    private bool showPostProcSettings = false;


    // UI Management
    private ScreenshotToolUI ui;
    #endregion

    #region Core Functions
    void OnGUI()
{
    scrollPosition = ui.DrawScrollView(scrollPosition, position.width, position.height);

    GUILayout.Space(10);
    ui.DrawHeader("📸 Quick Screenshot Tool");

    GUILayout.Space(10);
    ui.DrawSaveSettings(ref showSaveSettings);

    GUILayout.Space(10);
    ui.DrawCaptureSettings(ref showCaptureSettings);

    GUILayout.Space(10);
    ui.DrawAdvancedSettings(ref showAdvancedSettings);

    GUILayout.Space(10);
    ui.DrawPostProcessingSettings(ref showPostProcSettings);

    GUILayout.Space(10);
    ui.DrawTakeScreenshotButton(SceneView.lastActiveSceneView);

    GUILayout.Space(10);
    ui.DrawLivePreview();

    EditorGUILayout.EndScrollView();
    
}

    [MenuItem("Tools/Quick Screenshot Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScreenshotTool>("Screenshot Tool");
        window.minSize = new Vector2(400, 300);
    }

    private void OnEnable()
    {
        ui = new ScreenshotToolUI(this);
    }
    #endregion

    #region Display

    // ----- UI Element Functions -----

    /// <summary>
    /// Displays the controls for the save location (path, choose, open).
    /// </summary>
    internal void DisplaySaveLocationControls()
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label(new GUIContent("Save Location", "Path where screenshots will be saved"), GUILayout.Width(100));
        EditorGUILayout.SelectableLabel(folderPath, EditorStyles.textField, GUILayout.Height(18));

        if (GUILayout.Button(new GUIContent("Choose", "Select a folder to save screenshots"), GUILayout.Width(60)))
        {
            ChooseSaveLocation();
        }

        if (GUILayout.Button(new GUIContent("Open", "Open the folder where screenshots are saved"), GUILayout.Width(60)))
        {
            OpenSaveLocation();
        }

        GUILayout.EndHorizontal();

        fileTag = EditorGUILayout.TextField(
            new GUIContent("File Tag", "Tag to be included in the screenshot filename"), fileTag);
    }

    /// <summary>
    /// Displays the preview of screenshot dimensions.
    /// </summary>
    internal void DisplayPreviewResolution()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) return;

        var (width, height) = GetScreenshotDimensions(sceneView);
        GUILayout.Label($"Resolution: {width} x {height} pixels");
    }

    // ----- File Management Functions -----

    /// <summary>
    /// Opens a folder selection dialog to choose the save location.
    /// </summary>
    internal void ChooseSaveLocation()
    {
        string selectedPath = EditorUtility.OpenFolderPanel("Choose Save Folder", folderPath, "");
        if (string.IsNullOrEmpty(selectedPath)) return;
        folderPath = selectedPath;
    }

    /// <summary>
    /// Opens the save location folder in the file explorer.
    /// </summary>
    internal void OpenSaveLocation()
    {
        if (Directory.Exists(folderPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
        }
        else
        {
            UnityEngine.Debug.LogWarning("Save location does not exist.");
            folderPath = "Screenshots/"; // Reset to default if invalid
        }
    }


    /// <summary>
    /// Generates the full path for the screenshot file.
    /// </summary>
    internal string GenerateFilePath()
    {
        string extension = formats[selectedFormat].ToLower();
        string fileName = $"{fileTag}_{System.DateTime.Now:yyyyMMdd_HHmmss}.{extension}";
        return Path.Combine(folderPath, fileName);
    }


    /// <summary>
    /// Force the active Scene View window to match the user’s chosen aspect ratio.
    /// Only works reliably if Scene View is floating (undocked).
    /// </summary>
    public void ApplySceneViewAspectRatio()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            UnityEngine.Debug.LogWarning("No active Scene View to resize!");
            return;
        }

        // The Scene View is an EditorWindow, so we can tweak its position rect.
        Rect currentPos = sceneView.position;

        // Decide which dimension to keep as your "base." 
        // Let's keep height the same, adjust width to match aspect ratio:
        float desiredAspect = aspectRatio.x / Mathf.Max(0.001f, aspectRatio.y);
        float newWidth = currentPos.height * desiredAspect;

        // Now apply that new width to the Scene View window
        currentPos.width = newWidth;
        sceneView.position = currentPos;

        // Force an immediate repaint
        sceneView.Repaint();
        UnityEngine.Debug.Log($"Scene View resized to ~{newWidth} x {currentPos.height} for aspect ratio {aspectRatio.x}:{aspectRatio.y}");
    }

    #endregion

    #region Screenshot Handling

    // ----- Core Screenshot Functionality -----

    /// <summary>
    /// Captures a screenshot of the Scene View with the current settings.
    /// </summary>
    internal async void TakeEditorScreenshot()
    {
        await Task.Delay(captureDelay * 1000); // Apply capture delay

        FlashSceneView();  // Flash the Scene View
        string fullPath = GenerateFilePath(); // Generate file path

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath); // Create folder if it doesn't exist
        }

        SceneView sceneView = GetActiveSceneView();
        if (sceneView == null) return;

        var (width, height) = GetScreenshotDimensions(sceneView);
        RenderTexture renderTexture = CaptureScene(sceneView, width, height); //Get the screenshot data from the scene view

        SaveScreenshot(renderTexture, fullPath); // Save the screenshot
        CleanupAfterCapture(sceneView, renderTexture); // Clean up resources
        this.ShowNotification(new GUIContent("Screenshot saved!")); //yippee
        UnityEngine.Debug.Log($"Screenshot saved: {fullPath}");
        AssetDatabase.Refresh(); // Refresh AssetDatabase to reflect changes in Unity
    }

    /// <summary>
    /// Saves the captured screenshot to the specified path.
    /// </summary>
    void SaveScreenshot(RenderTexture renderTexture, string fullPath)
    {
        //Get the screenshot data
        RenderTexture.active = renderTexture;
        Texture2D screenshot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        screenshot.Apply();

        //Apply all the effects (post processing, colorblindness etc)
        AddAllEffects(screenshot);

        byte[] bytes = selectedFormat switch
        {
            0 => screenshot.EncodeToPNG(),
            1 => screenshot.EncodeToJPG(),
            _ => screenshot.EncodeToEXR()
        };

        File.WriteAllBytes(fullPath, bytes);
    }

    // ----- Scene View and Capture Utility Functions -----

    /// <summary>
    /// Retrieves the active Scene View.
    /// </summary>
    SceneView GetActiveSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            UnityEngine.Debug.LogError("No active Scene View found.");
        }
        return sceneView;
    }

    /// <summary>
    /// Calculates the dimensions for the screenshot based on the Scene View.
    /// </summary>
    (int width, int height) GetScreenshotDimensions(SceneView sceneView)
    {
        Rect sceneViewRect = sceneView.position;
        int width = (int)(Mathf.FloorToInt(sceneViewRect.width) * resolutionMultiplier);
        int height = (int)(Mathf.FloorToInt(sceneViewRect.height) * resolutionMultiplier);
        return (width, height);
    }

    /// <summary>
    /// Captures the Scene View to a RenderTexture.
    /// </summary>
    RenderTexture CaptureScene(SceneView sceneView, int width, int height)
    {
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        sceneView.camera.targetTexture = renderTexture;
        sceneView.camera.Render();
        return renderTexture;
    }

    // ----- Live Preview Logic -----
    private Texture2D cachedPreviewTexture;
    private double lastPreviewUpdateTime = 0f;
    private const float previewUpdateInterval = 0.25f; // Seconds between updates

    /// <summary>
    /// Displays a live preview of what the screenshot would look like.
    /// This version is performance-optimised by only updating at a set interval,
    /// rather than on every frame like OnGUI normally does.
    /// </summary>
    internal void DisplayLivePreview()
    {
        // If the live preview toggle is off, don't show anything
        if (!livePreviewEnabled) return;

        // Grab the current active Scene View (usually the main editor camera view)
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null || sceneView.camera == null)
        {
            GUILayout.Label("No active Scene View available.");
            return;
        }

        // ----------------------------
        // STEP 1: Throttle how often we update the preview
        // ----------------------------

        // Get the current editor runtime in seconds
        double currentTime = EditorApplication.timeSinceStartup;

        // Only update if enough time has passed (defined by previewUpdateInterval)
        bool shouldUpdate = (currentTime - lastPreviewUpdateTime) > previewUpdateInterval;

        // ----------------------------
        // STEP 2: Calculate dimensions based on Scene View & window size
        // ----------------------------

        var (sceneWidth, sceneHeight) = GetScreenshotDimensions(sceneView);
        float aspectRatio = (float)sceneWidth / sceneHeight;

        // Use some padding to avoid UI overlaps
        float availableWidth = position.width - 20;
        float availableHeight = position.height - 100;

        float previewWidth, previewHeight;

        // Keep the preview box within the bounds of the editor window
        if (availableWidth / aspectRatio <= availableHeight)
        {
            previewWidth = availableWidth;
            previewHeight = availableWidth / aspectRatio;
        }
        else
        {
            previewHeight = availableHeight;
            previewWidth = availableHeight * aspectRatio;
        }

        // ----------------------------
        // STEP 3: Only regenerate the preview if it’s time or size changed
        // ----------------------------

        if (shouldUpdate || cachedPreviewTexture == null ||
            cachedPreviewTexture.width != (int)previewWidth ||
            cachedPreviewTexture.height != (int)previewHeight)
        {
            // Clear out the old preview render texture if needed
            if (previewTexture != null) previewTexture.Release();

            // Set up a new RenderTexture with the correct size
            previewTexture = new RenderTexture((int)previewWidth, (int)previewHeight, 24);

            // Tell the Scene View camera to render to our RenderTexture
            sceneView.camera.targetTexture = previewTexture;
            sceneView.camera.Render(); // Manually render the camera
            sceneView.camera.targetTexture = null;

            // Convert the RenderTexture into a Texture2D that we can display
            RenderTexture.active = previewTexture;
            Texture2D tempTexture = new Texture2D((int)previewWidth, (int)previewHeight, TextureFormat.RGB24, false);
            tempTexture.ReadPixels(new Rect(0, 0, previewWidth, previewHeight), 0, 0);
            tempTexture.Apply();
            RenderTexture.active = null;

            // Optional: apply post-processing to the preview so users see the full effect
            AddAllEffects(tempTexture);

            // Clean up the previous cached preview
            if (cachedPreviewTexture != null)
            {
                DestroyImmediate(cachedPreviewTexture);
            }

            // Store the newly generated preview
            cachedPreviewTexture = tempTexture;

            // Record the time we last updated
            lastPreviewUpdateTime = currentTime;
        }

        // ----------------------------
        // STEP 4: Draw the cached preview to the UI
        // ----------------------------

        GUILayout.Label(new GUIContent(cachedPreviewTexture), GUILayout.Width(previewWidth), GUILayout.Height(previewHeight));
    }



    #endregion

    #region PostProcessing
    void AddAllEffects(Texture2D screenshot)
    {
        ApplyEffect(screenshot, (Effect)selectedEffect); // Use enum from PostProcessingEffects
        if (radiationMode) ApplyRadiationMode(screenshot);
        if (noiseAmount > 0) ApplyNoise(screenshot, noiseAmount);
        ApplyVignette(screenshot, vignetteIntensity);
        ApplyTint(screenshot, tintColor);
        if (saturationLevel != 1f) ApplySaturation(screenshot, saturationLevel);
        if (colourblindMode != ColourblindMode.Normal) ApplyColourblindFilter(screenshot, colourblindMode);
        if (watermark != null) ApplyWatermark(screenshot);

    }

    /// <summary>
    /// Applies a watermark to the screenshot.
    /// </summary>
    void ApplyWatermark(Texture2D screenshot)
    {
        if (watermark == null)
        {
            return;
        }

        // Check if the watermark texture has read/write enabled
        try
        {
            // Try to access a pixel to test read/write permissions
            watermark.GetPixel(0, 0);
        }
        catch (UnityException)
        {
            return;
        }

        //Set this to max size in relation to the screenshot (Stops it from overwhelming the image on lower resolutions
        float maxWatermarkWidthPercentage = 0.5f;
        float maxWatermarkHeightPercentage = 0.5f;

        int maxWidth = Mathf.FloorToInt(screenshot.width * maxWatermarkWidthPercentage);
        int maxHeight = Mathf.FloorToInt(screenshot.height * maxWatermarkHeightPercentage);

        float widthScale = Mathf.Min(1, maxWidth / (float)watermark.width);
        float heightScale = Mathf.Min(1, maxHeight / (float)watermark.height);
        float scale = Mathf.Min(widthScale, heightScale);

        int finalWidth = Mathf.FloorToInt(watermark.width * scale);
        int finalHeight = Mathf.FloorToInt(watermark.height * scale);

        int x = screenshot.width - finalWidth;
        int y = 0;

        for (int i = 0; i < finalWidth; i++)
        {
            for (int j = 0; j < finalHeight; j++)
            {
                int watermarkX = Mathf.FloorToInt(i / scale);
                int watermarkY = Mathf.FloorToInt(j / scale);

                Color watermarkPixel = watermark.GetPixel(watermarkX, watermarkY);
                watermarkPixel.a *= 0.5f;

                //Techy pixel combination technique
                if (watermarkPixel.a > 0)
                {
                    Color screenshotPixel = screenshot.GetPixel(x + i, y + j);
                    Color blendedPixel = Color.Lerp(screenshotPixel, watermarkPixel, watermarkPixel.a);
                    screenshot.SetPixel(x + i, y + j, blendedPixel);
                }
            }
        }

        screenshot.Apply();
    }


    void ApplyRadiationMode(Texture2D screenshot)
    {
        int width = screenshot.width;
        int height = screenshot.height;

        // Get the original pixels
        Color[] originalPixels = screenshot.GetPixels();
        Color[] glitchPixels = new Color[originalPixels.Length];

        // Initialize glitchPixels with the original pixels (avoids black gaps)
        for (int i = 0; i < originalPixels.Length; i++)
        {
            glitchPixels[i] = originalPixels[i];
        }

        // Randomly shift pixels and corrupt colors
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Calculate the original pixel index
                int originalIndex = y * width + x;

                // Generate a random offset (can wrap around the edges)
                int offsetX = (x + Random.Range(-30, 30) + width) % width;
                int offsetY = (y + Random.Range(-30, 30) + height) % height;
                int newIndex = offsetY * width + offsetX;

                // Randomly corrupt the color channels
                Color corruptedColor = originalPixels[originalIndex];
                if (Random.value > 0.9f) // % chance of color corruption
                {
                    corruptedColor.r = Random.value; // Random red value
                    corruptedColor.g = Random.value; // Random green value
                    corruptedColor.b = Random.value; // Random blue value
                }
                else if (Random.value > 0.9f) // % chance of black
                {
                    corruptedColor.r = 0.2f; // Random red value
                    corruptedColor.g = 0.2f; // Random green value
                    corruptedColor.b = 0.2f; // Random blue value
                }
                else
                {
                    corruptedColor.g += 0.1f; // Random green value
                }

                // Assign the corrupted color to the new position
                glitchPixels[newIndex] = corruptedColor;
            }
        }

        // Apply the glitchy pixels back to the texture
        screenshot.SetPixels(glitchPixels);
        screenshot.Apply(); // Apply the changes
    }
    #endregion

    #region Resource Management
    /// <summary>
    /// Cleans up resources after capturing. (Good for memory leaks and just overall cleanliness)
    /// </summary>
    void CleanupAfterCapture(SceneView sceneView, RenderTexture renderTexture)
    {
        sceneView.camera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(renderTexture);
    }
    #endregion

    #region Quirky Features
    internal async void FlashSceneView()
    {
        if (!flashEnabled) return;  // Exit if flash is disabled
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) return;

        float alpha = 1f;  // Start fully opaque

        // Register a callback to draw the white overlay with fading alpha
        void DrawFadingOverlay(SceneView sv)
        {
            Handles.BeginGUI();
            GUI.color = new Color(1, 1, 1, alpha); // White with fading alpha
            GUI.DrawTexture(new Rect(0, 0, sv.position.width, sv.position.height), Texture2D.whiteTexture);
            Handles.EndGUI();
        }

        SceneView.duringSceneGui += DrawFadingOverlay;
        sceneView.Repaint();

        // Gradually reduce alpha
        float duration = 3f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);  // Lerp from 1 to 0
            sceneView.Repaint();  // Repaint to show the updated alpha
            await Task.Yield();  // Wait for the next frame
        }

        // Cleanup after fading is complete
        SceneView.duringSceneGui -= DrawFadingOverlay;
        sceneView.Repaint();
    }
    #endregion
}
