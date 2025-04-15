/*
Made by Samuel Murrandah

AI Declaration:
Generative AI was used for editing and organisation such as reordering functions as well as some comments.
All code and logic was created and written by me
*/

using UnityEditor;
using UnityEngine;
using static PostProcessingEffects;

/// <summary>
/// Handles all the UI layout and rendering for the Screenshot Tool.
/// Includes collapsible sections, styles, and editor-friendly layout logic.
/// </summary>
public class ScreenshotToolUI
{
    private ScreenshotTool screenshotTool;

    // Styles for clean UI design
    private GUIStyle headerStyle;
    private GUIStyle sectionLabelStyle;
    private GUIStyle horizontalLineStyle;

    // Flag to ensure styles are only initialised once
    private bool stylesInitialised = false;

    public ScreenshotToolUI(ScreenshotTool tool)
    {
        screenshotTool = tool;
    }

    #region Initialisation

    /// <summary>
    /// Initialise custom styles for headers, section labels, and dividers.
    /// </summary>
    private void InitializeStyles()
    {
        // Main tool header style
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.2f, 0.7f, 1f) } // Cyan-blue tone
        };

        // Subsection titles
        sectionLabelStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };

        // Divider line between sections
        horizontalLineStyle = new GUIStyle
        {
            normal = { background = EditorGUIUtility.whiteTexture },
            margin = new RectOffset(0, 0, 4, 4),
            fixedHeight = 2
        };

        stylesInitialised = true;
    }

    #endregion

    #region Main Sections

    /// <summary>
    /// Draws all save-related settings (location, tag, format).
    /// </summary>
    public void DrawSaveSettings(ref bool showSaveSettings)
    {
        if (!stylesInitialised) InitializeStyles();

        showSaveSettings = EditorGUILayout.Foldout(showSaveSettings, "Save Settings", true);
        if (showSaveSettings)
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Space(5);
            screenshotTool.DisplaySaveLocationControls();
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            DrawHorizontalLine();
        }
        GUILayout.Space(10);
    }

    /// <summary>
    /// Draws capture settings such as resolution and format.
    /// Includes aspect ratio inputs and apply logic.
    /// </summary>
    public void DrawCaptureSettings(ref bool showCaptureSettings)
    {
        if (!stylesInitialised) InitializeStyles();

        showCaptureSettings = EditorGUILayout.Foldout(showCaptureSettings, "Capture Settings", true);
        if (showCaptureSettings)
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Space(5);

            screenshotTool.resolutionMultiplier = EditorGUILayout.IntSlider(
                new GUIContent("Resolution Multiplier", "Adjust the screenshot resolution scale"),
                screenshotTool.resolutionMultiplier, 1, 10);

            screenshotTool.selectedFormat = EditorGUILayout.Popup(
                new GUIContent("Image Format", "Select the format to save the screenshot"),
                screenshotTool.selectedFormat, screenshotTool.formats);

            GUILayout.Space(5);
            GUILayout.Label("Preview", sectionLabelStyle);
            screenshotTool.DisplayPreviewResolution();

            GUILayout.Space(5);
            GUILayout.Label("Scene View Aspect Ratio", sectionLabelStyle);

            screenshotTool.aspectRatio = EditorGUILayout.Vector2Field(
                new GUIContent("Aspect Ratio", "e.g., 16:9 or 1:1"),
                screenshotTool.aspectRatio);

            if (GUILayout.Button("Apply Scene View Aspect", GUILayout.Height(25)))
            {
                screenshotTool.ApplySceneViewAspectRatio();
            }

            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            DrawHorizontalLine();
        }
        GUILayout.Space(10);
    }

    /// <summary>
    /// Draws optional advanced settings like flash, delay, preview toggle, and watermark.
    /// </summary>
    public void DrawAdvancedSettings(ref bool showAdvancedSettings)
    {
        if (!stylesInitialised) InitializeStyles();

        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true);
        if (showAdvancedSettings)
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Space(5);

            screenshotTool.captureDelay = EditorGUILayout.IntSlider(
                new GUIContent("Capture Delay (s)", "Delay before taking the screenshot"),
                screenshotTool.captureDelay, 0, 10);

            screenshotTool.flashEnabled = EditorGUILayout.Toggle(
                new GUIContent("Enable Flash", "Toggle flash effect during capture"),
                screenshotTool.flashEnabled);

            screenshotTool.livePreviewEnabled = EditorGUILayout.Toggle(
                new GUIContent("Enable Live Preview", "Toggle the live preview window for performance"),
                screenshotTool.livePreviewEnabled);

            screenshotTool.colourblindMode = (ColourblindMode)EditorGUILayout.EnumPopup(
                new GUIContent("Colourblind Mode", "Choose a colourblind filter to apply"),
                screenshotTool.colourblindMode);

            screenshotTool.watermark = (Texture2D)EditorGUILayout.ObjectField(
                new GUIContent("Watermark", "Optional watermark overlay"),
                screenshotTool.watermark, typeof(Texture2D), false);

            if (screenshotTool.watermark != null)
            {
                if (!IsTextureReadable(screenshotTool.watermark))
                {
                    EditorGUILayout.HelpBox("Watermark texture is not readable. Enable 'Read/Write' in import settings.", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("Watermark is valid and ready.", MessageType.Info);
                }
            }

            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            DrawHorizontalLine();
        }
        GUILayout.Space(10);
    }

    /// <summary>
    /// Draws post-processing options like vignette, noise, effects, tint, and radiation.
    /// </summary>
    public void DrawPostProcessingSettings(ref bool showPostProcSettings)
    {
        if (!stylesInitialised) InitializeStyles();

        showPostProcSettings = EditorGUILayout.Foldout(showPostProcSettings, "Post Processing Settings", true);
        if (showPostProcSettings)
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Space(5);

            screenshotTool.vignetteIntensity = EditorGUILayout.Slider(
                new GUIContent("Vignette Intensity", "Adjust vignette effect intensity"),
                screenshotTool.vignetteIntensity, 0f, 1f);

            screenshotTool.noiseAmount = EditorGUILayout.Slider(
                new GUIContent("Noise Amount", "Add random noise"),
                screenshotTool.noiseAmount, 0f, 1f);

            screenshotTool.saturationLevel = EditorGUILayout.Slider(
                new GUIContent("Saturation", "Adjust colour saturation"),
                screenshotTool.saturationLevel, 0f, 2f);

            screenshotTool.tintColor = EditorGUILayout.ColorField(
                new GUIContent("Tint Colour", "Pick a tint colour"),
                screenshotTool.tintColor);

            screenshotTool.selectedEffect = (Effect)EditorGUILayout.EnumPopup(
                new GUIContent("Post-Processing Effect", "Select a post-processing effect"),
                screenshotTool.selectedEffect);

            screenshotTool.radiationMode = EditorGUILayout.Toggle(
                new GUIContent("Radiation Mode", "Apply a plutonium glitch effect"),
                screenshotTool.radiationMode);

            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            DrawHorizontalLine();
        }
        GUILayout.Space(10);
    }

    #endregion

    #region Draw Utility UI

    /// <summary>
    /// Creates the scroll view wrapper for the window.
    /// </summary>
    public Vector2 DrawScrollView(Vector2 scrollPosition, float windowWidth, float windowHeight)
    {
        return EditorGUILayout.BeginScrollView(
            scrollPosition,
            GUILayout.Width(windowWidth),
            GUILayout.Height(windowHeight));
    }

    /// <summary>
    /// Draws the main tool header.
    /// </summary>
    public void DrawHeader(string title)
    {
        if (!stylesInitialised) InitializeStyles();

        GUILayout.Space(10);
        GUILayout.Label(title, headerStyle);
        GUILayout.Space(5);
        DrawHorizontalLine();
        GUILayout.Space(5);
    }

    /// <summary>
    /// Draws the main screenshot button.
    /// </summary>
    public void DrawTakeScreenshotButton(SceneView sceneView)
    {
        if (!stylesInitialised) InitializeStyles();

        EditorGUI.BeginDisabledGroup(sceneView == null);
        if (GUILayout.Button(new GUIContent("Take Screenshot", "Capture the active Scene View"), GUILayout.Height(30)))
        {
            screenshotTool.TakeEditorScreenshot();
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);
    }

    /// <summary>
    /// Displays the screenshot preview (with post-processing).
    /// </summary>
    public void DrawLivePreview()
    {
        if (!stylesInitialised) InitializeStyles();

        GUILayout.Label("Live Preview", sectionLabelStyle);
        screenshotTool.DisplayLivePreview();
        GUILayout.Space(10);
    }

    /// <summary>
    /// Draws a simple white line to visually separate sections.
    /// </summary>
    private void DrawHorizontalLine()
    {
        if (!stylesInitialised) InitializeStyles();

        GUILayout.Space(4);
        GUILayout.Box("", horizontalLineStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(4);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Checks if the supplied texture is readable.
    /// </summary>
    private bool IsTextureReadable(Texture2D texture)
    {
        try
        {
            texture.GetPixel(0, 0); // Test read access
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }

    #endregion
}
