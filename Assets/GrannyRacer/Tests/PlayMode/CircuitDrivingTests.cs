using System.Collections;
using System.Collections.Generic;
using GrannyRacer.Racing;
using GrannyRacer.UI;
using GrannyRacer.Walker;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GrannyRacer.Tests.PlayMode
{
    public sealed class CircuitDrivingTests
    {
        private Gamepad pad;
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Circuit_SuburbSquare");
            yield return new WaitForFixedUpdate();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (pad != null) InputSystem.RemoveDevice(pad);
            pad = null;
            yield return null;
        }

        [UnityTest] public IEnumerator EntireRoadAndSidewalksHaveGroundIncludingTheClosingSeam()
        {
            var mesh = GameObject.Find("Generated Road").GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices;
            var layer = LayerMask.GetMask("Track");
            for (var i = 0; i < vertices.Length; i += 2)
            {
                var next = (i + 2) % vertices.Length;
                var steps = Mathf.CeilToInt(Vector3.Distance(vertices[i], vertices[next]) / .5f);
                for (var step = 0; step <= steps; step++)
                {
                    var left = Vector3.Lerp(vertices[i], vertices[next], (float)step / steps);
                    var right = Vector3.Lerp(vertices[i+1], vertices[next+1], (float)step / steps);
                    for (var lane = 0; lane <= 4; lane++)
                    {
                        var position = Vector3.Lerp(left, right, lane / 4f);
                        Assert.That(Physics.Raycast(position + Vector3.up * 2, Vector3.down, out var hit, 3, layer), Is.True, $"Missing ground at road segment {i/2}, lane {lane}.");
                        Assert.That(hit.point.y, Is.GreaterThanOrEqualTo(-.005f), "Safety lawn is not a substitute for the road collider.");
                    }
                    var outward = (right - left).normalized;
                    foreach (var position in new[] { left - outward * 1.5f, right + outward * 1.5f })
                    {
                        Assert.That(Physics.Raycast(position + Vector3.up * 2, Vector3.down, out var hit, 3, layer), Is.True);
                        Assert.That(hit.point.y, Is.EqualTo(.13f).Within(.005f));
                    }
                }
            }
            yield return null;
        }

        [UnityTest] public IEnumerator PhysicalControllerCanCompleteAnOrderedLapOnTheGeneratedCircuit() => DriveLap(false);

        [UnityTest] public IEnumerator DrivewayBranchRejoinsTheSameOrderedCheckpointSequence() => DriveLap(true);

        [UnityTest] public IEnumerator ExpectedGateRejectsBackwardEntryAndAcceptsAForwardJump()
        {
            var race=Object.FindAnyObjectByType<RaceController>();
            var walker=Object.FindAnyObjectByType<ArcadeWalkerController>();
            var body=walker.GetComponent<Rigidbody>();
            RaceCheckpoint gate=null;
            foreach(var candidate in Object.FindObjectsByType<RaceCheckpoint>()) if(candidate.Index==1)gate=candidate;
            while(race.State==RaceState.Countdown)yield return null;
            body.position=gate.transform.position+gate.transform.forward*3;
            body.rotation=Quaternion.LookRotation(-gate.transform.forward);
            body.linearVelocity=-gate.transform.forward*8;
            yield return new WaitForSeconds(1);
            Assert.That(race.NextCheckpoint,Is.EqualTo(1), "Backward entry must not grant progress.");
            body.position=gate.transform.position-gate.transform.forward*3+Vector3.up*2;
            body.rotation=gate.transform.rotation;body.linearVelocity=gate.transform.forward*8;
            yield return new WaitForSeconds(1);
            Assert.That(race.NextCheckpoint,Is.EqualTo(2), "Forward airborne crossing must count.");
            walker.ResetToSpawn();
            Assert.That(Vector3.Dot(body.position-gate.transform.position,gate.transform.forward),Is.GreaterThan(2));
            Assert.That(body.position.y,Is.EqualTo(.9f).Within(.01f));
        }

        [UnityTest] public IEnumerator BoostPanelAcceleratesAForwardRacerAndRestartClearsIt()
        {
            var race=Object.FindAnyObjectByType<RaceController>();
            var walker=Object.FindAnyObjectByType<ArcadeWalkerController>();
            var body=walker.GetComponent<Rigidbody>();
            var pad=Object.FindAnyObjectByType<TrackBoostPad>();
            Assert.That(pad,Is.Not.Null);
            Assert.That(walker.TryApplyTrackBoost(5,1),Is.False,"No boost during countdown.");
            while(race.State==RaceState.Countdown)yield return null;
            body.position=pad.transform.position-pad.transform.forward*4+Vector3.up*.9f;
            body.rotation=pad.transform.rotation;body.linearVelocity=pad.transform.forward*8;
            yield return new WaitForSeconds(.55f);
            Assert.That(walker.IsTrackBoostActive,Is.True);
            Assert.That(walker.Speed,Is.GreaterThan(9));
            race.StartRace();
            Assert.That(walker.IsTrackBoostActive,Is.False);
            yield return new WaitForFixedUpdate();
            Assert.That(walker.Speed,Is.LessThan(2));
        }

        [UnityTest] public IEnumerator LawnSlowsTheRacerAndABoostCanCarryItAcross()
        {
            var race=Object.FindAnyObjectByType<RaceController>();
            var walker=Object.FindAnyObjectByType<ArcadeWalkerController>();
            var body=walker.GetComponent<Rigidbody>();
            while(race.State==RaceState.Countdown)yield return null;
            var cruise = walker.Stats.MaximumSpeed;
            body.position=new Vector3(0,.9f,0);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*cruise;
            yield return new WaitForSeconds(.5f);
            Assert.That(walker.IsOffRoad,Is.True);
            Assert.That(walker.Speed,Is.LessThan(cruise * .65f), "Lawn must shed at least 35% of cruising speed.");
            var lawnSpeed = walker.Speed;
            Assert.That(walker.TryApplyTrackBoost(5,1),Is.True);
            yield return new WaitForSeconds(.2f);
            Assert.That(walker.Speed,Is.GreaterThan(lawnSpeed + 3), "The pad boost must carry momentum over the lawn.");
        }

        [UnityTest] public IEnumerator MapleGardensHasContinuousPavementAndAPlayableOrderedLap()
        {
            yield return SceneManager.LoadSceneAsync("Circuit_MapleGardens");
            yield return new WaitForFixedUpdate();
            yield return EntireRoadAndSidewalksHaveGroundIncludingTheClosingSeam();
            yield return DriveLap(false);
        }

        [UnityTest] public IEnumerator SchoolRunHasContinuousPavementAndAPlayableOrderedLap()
        {
            yield return SceneManager.LoadSceneAsync("Circuit_SchoolRun");
            yield return new WaitForFixedUpdate();
            yield return EntireRoadAndSidewalksHaveGroundIncludingTheClosingSeam();
            yield return DriveLap(false);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [UnityTest] public IEnumerator ArcadeSpeedLapUsesBoostHeatAndFinishesUnderSixtySeconds()
        {
            pad = InputSystem.AddDevice<Gamepad>();
            var race = Object.FindAnyObjectByType<RaceController>();
            var walker = Object.FindAnyObjectByType<ArcadeWalkerController>();
            var body = walker.GetComponent<Rigidbody>();
            var vertices = GameObject.Find("Generated Road").GetComponent<MeshFilter>().sharedMesh.vertices;
            var points = new Vector3[vertices.Length / 2];
            for (var i = 0; i < points.Length; i++) points[i] = (vertices[2*i] + vertices[2*i+1]) * .5f;
            var driver = new CircuitReviewDriver(points);
            while (race.State == RaceState.Countdown) yield return null;
            var deadline = Time.time + 60;
            var topSpeed = 0f; var maxHeat = 0f; var maxDistance = 0f;
            while (race.CurrentLap < 2 && Time.time < deadline)
            {
                InputSystem.QueueStateEvent(pad, driver.Sample(walker, body.linearVelocity));
                topSpeed = Mathf.Max(topSpeed, walker.Speed); maxHeat = Mathf.Max(maxHeat, walker.Heat);
                maxDistance = Mathf.Max(maxDistance, driver.RoadDistance);
                Assert.That(body.position.y, Is.GreaterThan(-.5f), "Racer fell below the road.");
                yield return null;
            }
            Debug.Log($"[ARCADE LAP] time={race.ElapsedTime:F2}, speed={topSpeed:F2}, heat={maxHeat:F2}, distance={maxDistance:F2}");
            Assert.That(race.CurrentLap, Is.EqualTo(2), "Fast physical lap must complete all ordered gates.");
            Assert.That(topSpeed, Is.GreaterThan(32), "Boost must reach racing speed.");
            Assert.That(maxHeat, Is.GreaterThan(.8f), "Review must exercise critical slipper heat.");
            Assert.That(maxDistance, Is.LessThan(5), "Fast lap left the road corridor.");
        }
#endif

        private IEnumerator DriveLap(bool shortcut)
        {
            pad = InputSystem.AddDevice<Gamepad>();
            var race = Object.FindAnyObjectByType<RaceController>();
            var walker = Object.FindAnyObjectByType<ArcadeWalkerController>();
            var vertices = GameObject.Find("Generated Road").GetComponent<MeshFilter>().sharedMesh.vertices;
            var points = new Vector3[vertices.Length / 2];
            for (var i = 0; i < points.Length; i++) points[i] = (vertices[i*2] + vertices[i*2+1]) * .5f;
            if(shortcut)
            {
#if UNITY_EDITOR
                var meshPath = UnityEditor.AssetDatabase.GetAssetPath(GameObject.Find("Generated Road").GetComponent<MeshFilter>().sharedMesh);
                var track = UnityEditor.AssetDatabase.LoadAssetAtPath<TrackDefinition>(System.IO.Path.GetDirectoryName(meshPath).Replace('\\','/') + "/Track.asset");
                var entry = track.ShortcutStartWaypoint; var exit = track.ShortcutEndWaypoint;
                var route = new List<Vector3>();
                for (var i = 0; i <= entry; i++) route.Add(points[i]);
                var steps = Mathf.CeilToInt(Vector3.Distance(points[entry], points[exit]) / .5f);
                for (var i = 1; i < steps; i++) route.Add(Vector3.Lerp(points[entry], points[exit], (float)i / steps));
                for (var i = exit; i < points.Length; i++) route.Add(points[i]);
                points=route.ToArray();
#else
                Assert.Ignore("Shortcut route definition requires the editor asset database.");
#endif
            }
            while (race.State == RaceState.Countdown) yield return null;
            Time.timeScale = 3;
            var deadline = Time.time + 180;
            var maximumOffroad = 0f;
            while (race.CurrentLap < 2 && Time.time < deadline)
            {
                var position = walker.transform.position; position.y = 0;
                var nearest = 0; var distance = float.PositiveInfinity;
                for (var i = 0; i < points.Length; i++)
                {
                    var candidate = (position - points[i]).sqrMagnitude;
                    if (candidate < distance) { distance = candidate; nearest = i; }
                }
                maximumOffroad = Mathf.Max(maximumOffroad, Mathf.Sqrt(distance));
                var target = (nearest + 1) % points.Length;
                var ahead = 0f;
                while (ahead < 8)
                {
                    var next = (target + 1) % points.Length;
                    ahead += Vector3.Distance(points[target], points[next]); target = next;
                }
                var angle = Vector3.SignedAngle(walker.transform.forward, points[target] - position, Vector3.up);
                var steering = Mathf.Clamp(angle / 28f, -1, 1);
                InputSystem.QueueStateEvent(pad, new GamepadState
                {
                    rightTrigger = walker.Speed < 8 ? .65f : 0,
                    leftTrigger = walker.Speed > 9 ? .25f : 0,
                    leftStick = new Vector2(steering, 0)
                });
                Assert.That(walker.transform.position.y, Is.GreaterThan(-.5f), "Racer fell below the circuit.");
                yield return null;
            }
            Assert.That(race.CurrentLap, Is.EqualTo(2), $"Physical lap did not complete; maximum centreline distance {maximumOffroad:0.00} m.");
            Assert.That(maximumOffroad, Is.LessThan(5), "Driving probe left the intended road corridor.");
        }
    }
}
