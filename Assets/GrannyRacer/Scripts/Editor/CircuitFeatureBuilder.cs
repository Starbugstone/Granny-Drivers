using GrannyRacer.Racing;
using UnityEditor;
using UnityEngine;

namespace GrannyRacer.Editor
{
    public static class CircuitFeatureBuilder
    {
        public static void Add(CircuitRecipe recipe, TrackDefinition track, Transform parent, string folder)
        {
            var race = Object.FindAnyObjectByType<RaceController>();
            race.SetCircuitName(recipe.circuitName);
            var cream = CircuitRoadBuilder.Material(folder, "GateCream", new Color(.97f,.88f,.65f));
            var teal = CircuitRoadBuilder.Material(folder, "GateTeal", new Color(.04f,.37f,.34f));
            var yellow = CircuitRoadBuilder.Material(folder, "BoostGold", new Color(1,.65f,.08f));
            var checkpoints = Object.FindObjectsByType<RaceCheckpoint>();
            var waypoint = 0;
            // The trigger's vertical plane spans asphalt, both sidewalks and hops. A reset is
            // on the centerline beyond it; neither resetting nor crossing backwards grants progress.
            foreach (var checkpoint in checkpoints)
            {
                var targetIndex = checkpoint.Index;
                var found = -1;
                for (var i = 0; i < track.Waypoints.Length; i++)
                    if (track.Waypoints[i].checkpoint && ++found == targetIndex) { waypoint = i; break; }
                var point = track.Waypoints[waypoint];
                var direction = (track.Waypoints[(waypoint + 1) % track.Waypoints.Length].position - point.position).normalized;
                var safe = checkpoint.transform.GetChild(0);
                safe.SetPositionAndRotation(point.position + direction * 3 + Vector3.up * .9f, Quaternion.LookRotation(direction));
                checkpoint.ConfigurePassage(safe);
                var collider = checkpoint.GetComponent<BoxCollider>();
                collider.center = new Vector3(0, 2.5f, 0);
                collider.size = new Vector3(point.width + 6.4f, 8, 2);
                if (targetIndex == 0) continue;
                var gate = new GameObject("Sector Gate " + targetIndex).transform; gate.SetParent(parent, false);
                gate.SetPositionAndRotation(point.position, Quaternion.LookRotation(direction));
                var half = point.width * .5f + 3.6f;
                Box("Gate left", gate, new Vector3(-half,2,0), new Vector3(.25f,4,.3f), teal);
                Box("Gate right", gate, new Vector3(half,2,0), new Vector3(.25f,4,.3f), teal);
                Box("Gate banner", gate, new Vector3(0,4,0), new Vector3(half * 2 + .3f,.6f,.18f), teal);
                Label("GATE " + targetIndex, gate, new Vector3(0,4,-.13f), .11f);
                for (var side = -1; side <= 1; side += 2)
                    Box("Gate stripe", gate, new Vector3(half*side,1,0), new Vector3(.27f,.45f,.32f), cream);
            }
            if (!recipe.arcadeFeatures) return;
            var padCount = 0;
            for (var i = 0; i < recipe.controlPoints.Length && padCount < 3; i++)
            {
                var a = recipe.controlPoints[i]; var b = recipe.controlPoints[(i+1)%recipe.controlPoints.Length];
                if (Vector3.Distance(a.position,b.position) < 75 || i % 2 != 0) continue;
                var direction = (b.position-a.position).normalized;
                var right = Vector3.Cross(Vector3.up,direction);
                var pad = new GameObject("Rocket strip " + (padCount+1)).transform; pad.SetParent(parent,false);
                pad.SetPositionAndRotation(a.position+direction*18+right*(padCount%2==0 ? -2.2f : 2.2f),Quaternion.LookRotation(direction));
                var trigger = pad.gameObject.AddComponent<BoxCollider>(); trigger.isTrigger = true;
                trigger.center = new Vector3(0,.4f,0); trigger.size = new Vector3(2.6f,.8f,5);
                pad.gameObject.AddComponent<TrackBoostPad>();
                Box("Boost surface",pad,new Vector3(0,.012f,0),new Vector3(2.6f,.02f,5),teal);
                for (var j = -1; j <= 1; j++)
                {
                    var left = Box("Boost chevron",pad,new Vector3(-.45f,.03f,j*1.35f),new Vector3(.16f,.018f,1.2f),yellow);
                    left.localRotation = Quaternion.Euler(0,45,0);
                    var rightArrow = Box("Boost chevron",pad,new Vector3(.45f,.03f,j*1.35f),new Vector3(.16f,.018f,1.2f),yellow);
                    rightArrow.localRotation = Quaternion.Euler(0,-45,0);
                }
                Label("ROCKET STRIP",pad,new Vector3(0,2,-3),.05f);
                padCount++;
            }
        }

        private static Transform Box(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial=material;go.isStatic=true;return go.transform;
        }
        private static void Label(string value, Transform parent, Vector3 position, float size)
        {
            var go=new GameObject(value);go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var text=go.AddComponent<TextMesh>();text.text=value;text.anchor=TextAnchor.MiddleCenter;
            text.fontSize=80;text.characterSize=size;text.color=new Color(.97f,.88f,.65f);
        }
    }
}
