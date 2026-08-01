using System.Collections.Generic;
using UnityEngine;

namespace GrannyRacer.Characters
{
    /// <summary>
    /// Swaps granny's slippers at runtime.
    ///
    /// The Granny_Walker rig carries two world-aligned sockets, Slipper_Socket_L
    /// and Slipper_Socket_R, parented to the ankle bones. Slipper meshes are
    /// authored symmetrically around that socket point, so a slipper prefab is
    /// mounted with an identity local transform and needs no per-foot variant.
    /// </summary>
    public sealed class SlipperSwapper : MonoBehaviour
    {
        public const string LeftSocketName = "Slipper_Socket_L";
        public const string RightSocketName = "Slipper_Socket_R";

        [Header("Sockets (auto-located by name if left empty)")]
        [SerializeField] private Transform leftSocket;
        [SerializeField] private Transform rightSocket;

        [Header("Available slippers")]
        [Tooltip("Prefabs built from the FBX files in Art/Models/Granny/Slippers.")]
        [SerializeField] private List<GameObject> slipperPrefabs = new List<GameObject>();

        [Tooltip("Index into Slipper Prefabs equipped on start. -1 leaves her barefoot.")]
        [SerializeField] private int startingSlipper;

        private readonly GameObject[] equipped = new GameObject[2];

        /// <summary>Index of the currently equipped pair, or -1 when barefoot.</summary>
        public int EquippedIndex { get; private set; } = -1;

        public int SlipperCount => slipperPrefabs.Count;

        public void Configure(GameObject[] prefabs, int startingIndex)
        {
            slipperPrefabs.Clear();
            if (prefabs != null)
            {
                for (var i = 0; i < prefabs.Length; i++)
                {
                    if (prefabs[i] != null)
                    {
                        slipperPrefabs.Add(prefabs[i]);
                    }
                }
            }

            startingSlipper = startingIndex;
        }

        private void Awake()
        {
            ResolveSockets();
        }

        private void Start()
        {
            if (startingSlipper >= 0)
            {
                Equip(startingSlipper);
            }
        }

        private void ResolveSockets()
        {
            if (leftSocket == null)
            {
                leftSocket = FindDescendant(transform, LeftSocketName);
            }

            if (rightSocket == null)
            {
                rightSocket = FindDescendant(transform, RightSocketName);
            }

            if (leftSocket == null || rightSocket == null)
            {
                Debug.LogError(
                    $"{nameof(SlipperSwapper)} on '{name}' could not find the slipper " +
                    $"sockets ('{LeftSocketName}' / '{RightSocketName}'). Assign them " +
                    "manually, or check that the Granny_Walker model imported its " +
                    "socket transforms.", this);
            }
        }

        /// <summary>Equips the pair at <paramref name="index"/>; out of range unequips.</summary>
        public void Equip(int index)
        {
            if (index < 0 || index >= slipperPrefabs.Count)
            {
                Unequip();
                return;
            }

            Equip(slipperPrefabs[index]);
            EquippedIndex = index;
        }

        /// <summary>Equips the same prefab on both feet.</summary>
        public void Equip(GameObject slipperPrefab)
        {
            if (slipperPrefab == null)
            {
                Unequip();
                return;
            }

            ResolveSockets();
            Unequip();

            equipped[0] = Mount(slipperPrefab, leftSocket);
            equipped[1] = Mount(slipperPrefab, rightSocket);
            EquippedIndex = slipperPrefabs.IndexOf(slipperPrefab);
        }

        /// <summary>Cycles to the next pair, wrapping around.</summary>
        public void EquipNext()
        {
            if (slipperPrefabs.Count == 0)
            {
                return;
            }

            Equip((EquippedIndex + 1) % slipperPrefabs.Count);
        }

        public void Unequip()
        {
            for (int i = 0; i < equipped.Length; i++)
            {
                if (equipped[i] == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(equipped[i]);
                }
                else
                {
                    DestroyImmediate(equipped[i]);
                }

                equipped[i] = null;
            }

            EquippedIndex = -1;
        }

        private static GameObject Mount(GameObject prefab, Transform socket)
        {
            if (socket == null)
            {
                return null;
            }

            GameObject instance = Instantiate(prefab, socket);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static Transform FindDescendant(Transform root, string childName)
        {
            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
