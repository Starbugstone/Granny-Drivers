using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace GrannyRacer.Tests.EditMode
{
    /// <summary>
    /// Cheap guards for the repository conventions in AGENTS.md.
    /// </summary>
    /// <remarks>
    /// These also serve as the smoke test for the EditMode test harness itself: if
    /// Tools/unity-test.ps1 reports these running, the pipeline is wired correctly.
    /// </remarks>
    public class ProjectConventionsTests
    {
        private static string AssetsPath => Application.dataPath;
        private static string GameRoot => Path.Combine(AssetsPath, "GrannyRacer");

        [Test]
        public void GameAssetsLiveUnderGrannyRacerFolder()
        {
            Assert.IsTrue(Directory.Exists(GameRoot),
                $"Expected the game asset root at {GameRoot}. See AGENTS.md section 2.");
        }

        [Test]
        public void NoScriptsSitDirectlyInAssetsRoot()
        {
            var stray = Directory.GetFiles(AssetsPath, "*.cs", SearchOption.TopDirectoryOnly);

            Assert.IsEmpty(stray,
                "C# files must live under Assets/GrannyRacer/Scripts, not the Assets root. " +
                "Found: " + string.Join(", ", stray.Select(Path.GetFileName)));
        }

        [Test]
        public void EveryAssetFolderIsUnderARecognisedTopLevelFolder()
        {
            var allowed = new[] { "GrannyRacer", "ThirdParty", "Scenes", "Settings", "TutorialInfo" };

            var unexpected = Directory.GetDirectories(AssetsPath)
                .Select(Path.GetFileName)
                .Where(name => !allowed.Contains(name))
                .ToArray();

            Assert.IsEmpty(unexpected,
                "Unexpected top-level folder(s) under Assets/. Either move the content under " +
                "Assets/GrannyRacer or update this test deliberately. Found: " +
                string.Join(", ", unexpected));
        }
    }
}
