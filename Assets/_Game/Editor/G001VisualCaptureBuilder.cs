using System.IO;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
    // Buildskript fuer den G001-Capture-Lauf.
    // Menue: Eidren / G001 / Build Development Player
    // Batchmodus: -executeMethod Eidren.Editor.G001VisualCaptureBuilder.BuildDevelopmentPlayer
    public static class G001VisualCaptureBuilder
    {
        [MenuItem("Eidren/G001/Build Development Player")]
        public static void BuildDevelopmentPlayer()
        {
            WindowsReleaseBuilder.BuildDevelopment();
        }

        // Batchmodus-Eintrittspunkt: baut den Entwicklungs-Player.
        // Wird von Tools/G001-capture.ps1 aufgerufen.
        public static void BuildDevelopmentPlayerBatch()
        {
            try
            {
                WindowsReleaseBuilder.BuildDevelopment();
                Debug.Log("G001 BUILD: PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("G001 BUILD: FAIL - " + ex.Message);
                EditorApplication.Exit(1);
            }
        }
    }
}
