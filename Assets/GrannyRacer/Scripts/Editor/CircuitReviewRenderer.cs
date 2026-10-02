using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GrannyRacer.Editor
{
    public static class CircuitReviewRenderer
    {
        public static void CaptureTour()
        {
            Directory.CreateDirectory("Docs/Art/Previews/Neighborhood");
            foreach(var destination in NeighborhoodTourBuilder.Destinations())
            {
                EditorSceneManager.OpenScene(destination.scenePath);
                var camera=UnityEngine.Camera.main;
                var name=Path.GetFileNameWithoutExtension(destination.scenePath);
                var racer=Object.FindAnyObjectByType<GrannyRacer.Walker.ArcadeWalkerController>();
                camera.transform.LookAt(racer.transform.position+Vector3.up*.9f);
                Capture(camera,"Docs/Art/Previews/Neighborhood/"+name+"_driver.png");
                camera.transform.position=new Vector3(260,310,-320);camera.transform.LookAt(Vector3.zero);
                camera.farClipPlane=1000;camera.orthographic=true;camera.orthographicSize=235;RenderSettings.fog=false;
                Capture(camera,"Docs/Art/Previews/Neighborhood/"+name+"_overview.png");
            }
            Debug.Log("[GrannyRacer] Captured all three tour scenes using actual imported meshes.");
        }

        public static void CaptureDefault()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(CircuitSceneBuilder.DefaultScenePath);
            Directory.CreateDirectory("Docs/Art/Previews");
            var camera = UnityEngine.Camera.main;
            var racer = Object.FindAnyObjectByType<GrannyRacer.Walker.ArcadeWalkerController>();
            camera.transform.LookAt(racer.transform.position + Vector3.up * .9f);
            Capture(camera, "Docs/Art/Previews/circuit_driver.png");
            camera.transform.position = new Vector3(230, 250, -265);
            camera.transform.LookAt(Vector3.zero);
            camera.farClipPlane = 900; camera.orthographic = true; camera.orthographicSize = 195;
            RenderSettings.fog = false;
            Capture(camera, "Docs/Art/Previews/circuit_overview.png");
            Debug.Log("[GrannyRacer] Rendered actual circuit meshes through Unity. Human art review pending.");
        }
        private static void Capture(UnityEngine.Camera camera, string path)
        {
            var target = new RenderTexture(1600, 1000, 24);
            var previous = RenderTexture.active;
            var oldTarget = camera.targetTexture;
            var texture = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                // Let URP populate material buffers after opening the scene before reading back.
                camera.Render(); camera.Render(); camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0,0,1600,1000),0,0); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { camera.targetTexture = oldTarget; RenderTexture.active = previous; Object.DestroyImmediate(texture); target.Release(); Object.DestroyImmediate(target); }
        }
    }
}
