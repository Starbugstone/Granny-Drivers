using System;
using GrannyRacer.Racing;
using UnityEditor;
using UnityEngine;

namespace GrannyRacer.Editor
{
    public sealed class CircuitBuilderWindow : EditorWindow
    {
        [SerializeField] private CircuitRecipe recipe;
        [SerializeField] private bool editRoute = true;
        [SerializeField] private bool placeAssets;
        [SerializeField] private GameObject brush;
        [SerializeField] private float brushYaw;
        [SerializeField] private float brushScale = 1;
        private Vector2 scroll;
        private string status = "Create or select a recipe. Edit points, validate, then build and play.";
        private bool error;

        [MenuItem("Granny Racer/Circuit Builder/Open Builder")]
        public static void ShowWindow() => Open(Selection.activeObject as CircuitRecipe);
        public static void Open(CircuitRecipe selected)
        {
            if (Application.isBatchMode) return;
            var window = GetWindow<CircuitBuilderWindow>("Circuit Builder");
            if (selected != null) window.recipe = selected;
            window.minSize = new Vector2(360, 500);
            window.Show();
        }
        private void OnEnable() => SceneView.duringSceneGui += SceneGui;
        private void OnDisable() => SceneView.duringSceneGui -= SceneGui;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("SUBURB CIRCUIT BUILDER", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Closed streets • square blocks • reusable neighbourhood kit", EditorStyles.wordWrappedLabel);
            recipe = (CircuitRecipe)EditorGUILayout.ObjectField("Recipe", recipe, typeof(CircuitRecipe), false);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Create / open Suburb Square")) Run(CircuitSceneBuilder.CreateDefault);
                if (GUILayout.Button("New circuit recipe…"))
                {
                    var path = EditorUtility.SaveFilePanelInProject("New circuit", "Circuit_NewSuburb", "asset", "Choose a recipe location.");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var copy = recipe != null ? Instantiate(recipe) : CreateInstance<CircuitRecipe>();
                        if (copy.controlPoints.Length == 0) copy.controlPoints = CircuitLayout.Square();
                        AssetDatabase.CreateAsset(copy, path); AssetDatabase.SaveAssets(); recipe = copy;
                    }
                }
                if (recipe == null) return;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Validate")) Run(() =>
                    {
                        var points = CircuitLayout.Bake(recipe); CircuitSceneBuilder.ValidateAssets(recipe, points);
                        status = $"Valid: {TrackLayoutBuilder.MeasureLapLength(points):0} m, {points.Length} samples. Human playtest still required.";
                    });
                    if (GUILayout.Button("Build playable scene")) Run(() =>
                    {
                        AssetDatabase.SaveAssetIfDirty(recipe);
                        CircuitSceneBuilder.Build(recipe, CircuitSceneBuilder.ScenePath(recipe));
                        status = "Built " + CircuitSceneBuilder.ScenePath(recipe) + ". Press Play to race.";
                    });
                }
                editRoute = EditorGUILayout.Toggle("Route handles in Scene view", editRoute);
                placeAssets = EditorGUILayout.Toggle("Place extra assets (Shift-click)", placeAssets);
                if (placeAssets)
                {
                    brush = (GameObject)EditorGUILayout.ObjectField("Asset", brush, typeof(GameObject), false);
                    brushYaw = EditorGUILayout.FloatField("Rotation", brushYaw);
                    brushScale = EditorGUILayout.Slider("Scale", brushScale, .1f, 10);
                }
                EditorGUILayout.HelpBox(status, error ? MessageType.Error : MessageType.Info);
                scroll = EditorGUILayout.BeginScrollView(scroll);
                var data = new SerializedObject(recipe); data.Update();
                foreach (var name in new[] { "circuitName", "laps", "seed", "lotSpacing", "houseSetback", "arcadeFeatures", "neighborhoodDetails", "drivewayShortcut", "shortcutStartControl", "shortcutEndControl", "shortcutWidth", "controlPoints", "palette", "extraAssets" })
                    EditorGUILayout.PropertyField(data.FindProperty(name), true);
                if (data.ApplyModifiedProperties()) SceneView.RepaintAll();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.HelpBox("Palette: drag a prefab/model into a row and choose House, Tree, Fence or Street Furniture for automatic placement. Landmark entries are available for manual placement. Extra assets and route points are saved in the recipe and survive rebuilds. All route points use Y = 0.", MessageType.None);
            }
        }

        private void SceneGui(SceneView view)
        {
            if (recipe == null || EditorApplication.isPlaying) return;
            var points = recipe.controlPoints;
            if (points == null) return;
            for (var i = 0; i < points.Length; i++)
            {
                Handles.color = points[i].checkpoint ? Color.yellow : Color.cyan;
                Handles.DrawLine(points[i].position, points[(i + 1) % points.Length].position);
                Handles.Label(points[i].position + Vector3.up, $"{i}" + (points[i].checkpoint ? " CP" : ""));
                if (!editRoute || placeAssets) continue;
                EditorGUI.BeginChangeCheck();
                var position = Handles.FreeMoveHandle(points[i].position, HandleUtility.GetHandleSize(points[i].position) * .09f,
                    Vector3.one, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(recipe, "Move circuit point"); position.y = 0;
                    recipe.controlPoints[i].position = position; EditorUtility.SetDirty(recipe); Repaint();
                }
            }
            if (recipe.extraAssets != null)
            for (var i = 0; i < recipe.extraAssets.Length; i++)
            {
                var extra = recipe.extraAssets[i]; if (extra == null) continue;
                Handles.color = new Color(.9f,.5f,.15f);
                Handles.DrawWireDisc(extra.position, Vector3.up, 1);
                Handles.Label(extra.position, extra.prefab == null ? "Missing extra" : extra.prefab.name);
                if (placeAssets) continue;
                EditorGUI.BeginChangeCheck();
                var position = Handles.FreeMoveHandle(extra.position, HandleUtility.GetHandleSize(extra.position) * .06f, Vector3.one, Handles.CubeHandleCap);
                if (EditorGUI.EndChangeCheck())
                { Undo.RecordObject(recipe, "Move extra asset"); position.y = 0; extra.position = position; EditorUtility.SetDirty(recipe); Repaint(); }
            }
            var evt = Event.current;
            if (!placeAssets || brush == null || !evt.shift || evt.alt) return;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out var distance)) return;
            var hit = ray.GetPoint(distance); hit.x = Mathf.Round(hit.x); hit.z = Mathf.Round(hit.z); hit.y = 0;
            Handles.color = Color.yellow; Handles.DrawWireDisc(hit, Vector3.up, 1);
            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                Undo.RecordObject(recipe, "Place circuit extra");
                var extras = recipe.extraAssets ?? Array.Empty<CircuitPlacement>();
                Array.Resize(ref extras, extras.Length + 1);
                extras[extras.Length - 1] = new CircuitPlacement { prefab = brush, position = hit, yaw = brushYaw, scale = brushScale };
                recipe.extraAssets = extras; EditorUtility.SetDirty(recipe); evt.Use(); Repaint();
            }
            view.Repaint();
        }

        private void Run(Action action)
        {
            try { error = false; action(); }
            catch (Exception exception) { error = true; status = exception.Message; Debug.LogException(exception); }
        }
    }
}
