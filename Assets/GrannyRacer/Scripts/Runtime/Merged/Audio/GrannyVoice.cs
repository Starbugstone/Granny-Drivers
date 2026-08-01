using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrannyRacer.Audio
{
/// <summary>
/// Plays Granny's contextual voice lines. Add this component to the Granny root
/// and call the public reaction methods from movement, inventory and combat code.
/// Collision reactions work automatically when this component shares the object
/// with a 2D or 3D collider/rigidbody.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class GrannyVoice : MonoBehaviour
{
    public enum Reaction
    {
        Input,
        Collision,
        ItemPickup,
        AttackGiven,
        AttackReceived
    }

    [Header("Optional clip overrides")]
    [Tooltip("If a bank is empty, clips are loaded from its Resources/Audio/Granny folder.")]
    [SerializeField] private AudioClip[] inputClips;
    [SerializeField] private AudioClip[] collisionClips;
    [SerializeField] private AudioClip[] itemPickupClips;
    [SerializeField] private AudioClip[] attackGivenClips;
    [SerializeField] private AudioClip[] attackReceivedClips;

    [Header("Chatter control")]
    [Min(0f)] [SerializeField] private float inputCooldown = 2.5f;
    [Min(0f)] [SerializeField] private float collisionCooldown = 1.75f;
    [Min(0f)] [SerializeField] private float pickupCooldown = 0.3f;
    [Min(0f)] [SerializeField] private float attackCooldown = 0.35f;
    [Min(0f)] [SerializeField] private float minimumCollisionSpeed = 3f;
    [Range(0f, 1f)] [SerializeField] private float spatialBlend = 0.75f;
    [SerializeField] private bool interruptCurrentLine = false;

    private readonly Dictionary<Reaction, float> nextAllowedTime = new();
    private readonly Dictionary<Reaction, int> lastPlayedIndex = new();
    private AudioSource voiceSource;

    private void Awake()
    {
        voiceSource = GetComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = spatialBlend;

        inputClips = LoadFallback(inputClips, "Audio/Granny/Input");
        collisionClips = LoadFallback(collisionClips, "Audio/Granny/Collision");
        itemPickupClips = LoadFallback(itemPickupClips, "Audio/Granny/ItemPickup");
        attackGivenClips = LoadFallback(attackGivenClips, "Audio/Granny/AttackGiven");
        attackReceivedClips = LoadFallback(attackReceivedClips, "Audio/Granny/AttackReceived");
    }

    /// <summary>Call for a discrete player command, not every frame an input is held.</summary>
    public void ReactToInput() => PlayReaction(Reaction.Input);

    /// <summary>Call after an item has successfully entered Granny's inventory.</summary>
    public void ReactToItemPickup() => PlayReaction(Reaction.ItemPickup);

    /// <summary>Call when Granny's attack successfully hits a target.</summary>
    public void ReactToAttackGiven() => PlayReaction(Reaction.AttackGiven);

    /// <summary>Call when Granny receives damage.</summary>
    public void ReactToAttackReceived() => PlayReaction(Reaction.AttackReceived);

    /// <summary>Call from custom vehicle/character collision handling.</summary>
    public void ReactToCollision(float impactSpeed)
    {
        if (impactSpeed >= minimumCollisionSpeed)
            PlayReaction(Reaction.Collision);
    }

    /// <summary>UnityEvent-friendly entry point for custom integrations.</summary>
    public void PlayReaction(Reaction reaction)
    {
        AudioClip[] bank = GetBank(reaction);
        if (bank == null || bank.Length == 0)
            return;

        float now = Time.unscaledTime;
        if (nextAllowedTime.TryGetValue(reaction, out float allowedAt) && now < allowedAt)
            return;

        bool isCombatReaction = reaction == Reaction.AttackGiven || reaction == Reaction.AttackReceived;
        if (voiceSource.isPlaying && !interruptCurrentLine && !isCombatReaction)
            return;

        int index = PickNonRepeatingIndex(reaction, bank.Length);
        if (bank[index] == null)
            return;

        if (voiceSource.isPlaying)
            voiceSource.Stop();

        voiceSource.clip = bank[index];
        voiceSource.pitch = UnityEngine.Random.Range(0.97f, 1.03f);
        voiceSource.Play();
        nextAllowedTime[reaction] = now + GetCooldown(reaction);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ReactToCollision(collision.relativeVelocity.magnitude);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ReactToCollision(collision.relativeVelocity.magnitude);
    }

    private AudioClip[] GetBank(Reaction reaction)
    {
        return reaction switch
        {
            Reaction.Input => inputClips,
            Reaction.Collision => collisionClips,
            Reaction.ItemPickup => itemPickupClips,
            Reaction.AttackGiven => attackGivenClips,
            Reaction.AttackReceived => attackReceivedClips,
            _ => Array.Empty<AudioClip>()
        };
    }

    private float GetCooldown(Reaction reaction)
    {
        return reaction switch
        {
            Reaction.Input => inputCooldown,
            Reaction.Collision => collisionCooldown,
            Reaction.ItemPickup => pickupCooldown,
            Reaction.AttackGiven => attackCooldown,
            Reaction.AttackReceived => attackCooldown,
            _ => 0f
        };
    }

    private int PickNonRepeatingIndex(Reaction reaction, int count)
    {
        if (count <= 1)
            return 0;

        if (!lastPlayedIndex.TryGetValue(reaction, out int previous) || previous < 0 || previous >= count)
        {
            int firstIndex = UnityEngine.Random.Range(0, count);
            lastPlayedIndex[reaction] = firstIndex;
            return firstIndex;
        }

        int index = UnityEngine.Random.Range(0, count - 1);
        if (index >= previous)
            index++;

        lastPlayedIndex[reaction] = index;
        return index;
    }

    private static AudioClip[] LoadFallback(AudioClip[] configured, string resourcesPath)
    {
        return configured != null && configured.Length > 0
            ? configured
            : Resources.LoadAll<AudioClip>(resourcesPath);
    }
}
}
