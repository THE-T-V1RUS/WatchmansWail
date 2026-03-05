using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class PipeOrganSoundController : MonoBehaviour
{
    private static readonly int[] LegacyDefaultRootMidis = { 45, 50, 55, 60, 65, 70 };
    private static readonly int[] DefaultRootMidis = { 12, 24, 36, 48, 60, 72 };

    [Serializable]
    private struct OrganSampleZone
    {
        public AudioClip clip;
        public int rootMidi;
    }

    [Header("Scale / Melody")]
    [SerializeField] private int tonicMidi = 45;
    [SerializeField] private int minMidi = 12;
    [SerializeField] private int maxMidi = 76;
    [SerializeField] private int notesPerPhrase = 16;
    [SerializeField] private int seed = 12345;

    [Header("Composition")]
    [SerializeField] private bool useComposedPhrase = true;
    [SerializeField] private bool useHarmonicMinorLeadingTone = true;

    [Header("Sound")]
    [SerializeField] private List<OrganSampleZone> organSampleZones = new();
    [SerializeField] private AudioMixerGroup outputMixerGroup;
    [SerializeField] private float releaseTime = 0.12f;
    [SerializeField] private float masterVolume = 0.35f;

    [Header("Bass Layer")]
    [SerializeField] private bool enableBassLayer = true;
    [SerializeField] private int melodyOctaveOffset = 0;
    [SerializeField] private int bassOctaveOffset = -2;
    [SerializeField, Range(0f, 2f)] private float bassVolumeRatio = 1.2f;

    [Header("Mood")]
    [SerializeField] private bool eerieMode = false;

    private readonly int[] dorian = { 0, 2, 3, 5, 7, 9, 10 };
    private readonly int[] naturalMinor = { 0, 2, 3, 5, 7, 8, 10 };
    private readonly int[] harmonicMinor = { 0, 2, 3, 5, 7, 8, 11 };
    private readonly List<int> melodyMidi = new();

    private AudioSource audioSource;
    private int phraseStep;
    private System.Random rng;
    private bool leftWasPressed;
    private bool rightWasPressed;
    private int[] lastPlayedMidiSet;

    private readonly List<AudioSource> leftChordSources = new();
    private readonly List<AudioSource> rightChordSources = new();

    private void Awake()
    {
        EnsureDefaultSampleZones();

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f;
        audioSource.outputAudioMixerGroup = outputMixerGroup;
    }

    private void OnValidate()
    {
        EnsureDefaultSampleZones();
    }

    private void OnDisable()
    {
        StopChord(leftChordSources, true);
        StopChord(rightChordSources, true);
    }

    private void Start()
    {
        if (!HasValidSampleZones())
        {
            Debug.LogError("PipeOrganSoundController: No audio samples assigned. Add clips to organSampleZones.");
            enabled = false;
            return;
        }

        rng = new System.Random(seed);
        BuildProceduralPhrase();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool leftPressed = keyboard.leftArrowKey.isPressed;
        bool rightPressed = keyboard.rightArrowKey.isPressed;

        if (leftPressed && !leftWasPressed)
        {
            StartChord(-1, leftChordSources);
        }

        if (!leftPressed && leftWasPressed)
        {
            StopChord(leftChordSources, false);
        }

        if (rightPressed && !rightWasPressed)
        {
            StartChord(1, rightChordSources);
        }

        if (!rightPressed && rightWasPressed)
        {
            StopChord(rightChordSources, false);
        }

        leftWasPressed = leftPressed;
        rightWasPressed = rightPressed;
    }

    private void StartChord(int steeringDirection, List<AudioSource> targetSources)
    {
        if (melodyMidi.Count == 0)
        {
            BuildProceduralPhrase();
        }

        int baseMidi = melodyMidi[phraseStep % melodyMidi.Count];
        int steeredMidi = baseMidi + steeringDirection;
        int melodyHandMidi = Mathf.Clamp(steeredMidi + (12 * melodyOctaveOffset), minMidi, maxMidi);
        int[] chordMidis = BuildChordMidis(melodyHandMidi);
        int bassMidi = Mathf.Clamp(melodyHandMidi + (12 * bassOctaveOffset), minMidi, maxMidi);

        if (eerieMode)
        {
            ApplyEerieFlavor(ref chordMidis, ref bassMidi, melodyHandMidi);
        }

        int[] combinedMidis = BuildCombinedMidiSet(chordMidis, bassMidi);

        if (IsSameAsLastPlayed(combinedMidis))
        {
            int adjustment = steeringDirection >= 0 ? 1 : -1;
            bool foundDifferent = false;

            for (int attempt = 1; attempt <= 12; attempt++)
            {
                int adjustedMelodyMidi = Mathf.Clamp(melodyHandMidi + (adjustment * attempt), minMidi, maxMidi);
                int[] adjustedChord = BuildChordMidis(adjustedMelodyMidi);
                int adjustedBass = Mathf.Clamp(adjustedMelodyMidi + (12 * bassOctaveOffset), minMidi, maxMidi);
                int[] adjustedCombined = BuildCombinedMidiSet(adjustedChord, adjustedBass);

                if (!IsSameAsLastPlayed(adjustedCombined))
                {
                    melodyHandMidi = adjustedMelodyMidi;
                    chordMidis = adjustedChord;
                    bassMidi = adjustedBass;
                    combinedMidis = adjustedCombined;
                    foundDifferent = true;
                    break;
                }
            }

            if (!foundDifferent)
            {
                for (int attempt = 1; attempt <= 12; attempt++)
                {
                    int adjustedMelodyMidi = Mathf.Clamp(melodyHandMidi - (adjustment * attempt), minMidi, maxMidi);
                    int[] adjustedChord = BuildChordMidis(adjustedMelodyMidi);
                    int adjustedBass = Mathf.Clamp(adjustedMelodyMidi + (12 * bassOctaveOffset), minMidi, maxMidi);
                    int[] adjustedCombined = BuildCombinedMidiSet(adjustedChord, adjustedBass);

                    if (!IsSameAsLastPlayed(adjustedCombined))
                    {
                        melodyHandMidi = adjustedMelodyMidi;
                        chordMidis = adjustedChord;
                        bassMidi = adjustedBass;
                        combinedMidis = adjustedCombined;
                        break;
                    }
                }
            }
        }

        StopChord(targetSources, true);

        float perVoiceVolume = chordMidis.Length > 0 ? masterVolume / chordMidis.Length : masterVolume;
        for (int i = 0; i < chordMidis.Length; i++)
        {
            AudioSource voice = CreateVoiceSource();
            AudioClip clip = GetClipForMidi(chordMidis[i], out float pitch);
            voice.clip = clip;
            float detunedPitch = eerieMode ? pitch + GetEerieDetune() : pitch;
            voice.pitch = Mathf.Max(0.01f, detunedPitch);
            voice.loop = true;
            voice.volume = perVoiceVolume;
            voice.Play();
            targetSources.Add(voice);
        }

        if (enableBassLayer)
        {
            AudioSource bassVoice = CreateVoiceSource();
            AudioClip bassClip = GetClipForMidi(bassMidi, out float bassPitch);
            bassVoice.clip = bassClip;
            float bassDetunedPitch = eerieMode ? bassPitch + (GetEerieDetune() * 0.6f) : bassPitch;
            bassVoice.pitch = Mathf.Max(0.01f, bassDetunedPitch);
            bassVoice.loop = true;
            bassVoice.volume = masterVolume * Mathf.Clamp(bassVolumeRatio, 0f, 2f);
            bassVoice.Play();
            targetSources.Add(bassVoice);
        }

        lastPlayedMidiSet = combinedMidis;

        phraseStep = (phraseStep + 1) % melodyMidi.Count;
    }

    private int[] BuildCombinedMidiSet(int[] chordMidis, int bassMidi)
    {
        if (!enableBassLayer)
        {
            int[] withoutBass = new int[chordMidis.Length];
            Array.Copy(chordMidis, withoutBass, chordMidis.Length);
            return withoutBass;
        }

        int[] withBass = new int[chordMidis.Length + 1];
        Array.Copy(chordMidis, withBass, chordMidis.Length);
        withBass[chordMidis.Length] = bassMidi;
        return withBass;
    }

    private void ApplyEerieFlavor(ref int[] chordMidis, ref int bassMidi, int rootMidi)
    {
        if (chordMidis == null || chordMidis.Length < 3)
        {
            return;
        }

        if (rng.NextDouble() < 0.65)
        {
            chordMidis[1] = Mathf.Clamp(rootMidi + 1, minMidi, maxMidi);
        }

        if (rng.NextDouble() < 0.75)
        {
            chordMidis[2] = Mathf.Clamp(chordMidis[2] - 1, minMidi, maxMidi);
        }

        if (rng.NextDouble() < 0.45)
        {
            bassMidi = Mathf.Clamp(bassMidi + 1, minMidi, maxMidi);
        }
    }

    private float GetEerieDetune()
    {
        return ((float)rng.NextDouble() * 2f - 1f) * 0.018f;
    }

    private bool IsSameAsLastPlayed(int[] currentMidis)
    {
        if (lastPlayedMidiSet == null || currentMidis == null)
        {
            return false;
        }

        if (lastPlayedMidiSet.Length != currentMidis.Length)
        {
            return false;
        }

        for (int i = 0; i < currentMidis.Length; i++)
        {
            if (lastPlayedMidiSet[i] != currentMidis[i])
            {
                return false;
            }
        }

        return true;
    }

    private int[] BuildChordMidis(int rootMidi)
    {
        int[] intervals = GetTriadIntervals(rootMidi);
        return new[]
        {
            Mathf.Clamp(rootMidi + intervals[0], minMidi, maxMidi),
            Mathf.Clamp(rootMidi + intervals[1], minMidi, maxMidi),
            Mathf.Clamp(rootMidi + intervals[2], minMidi, maxMidi)
        };
    }

    private int[] GetTriadIntervals(int midi)
    {
        int semitoneFromTonic = Mod(midi - tonicMidi, 12);

        if (semitoneFromTonic == 3 || semitoneFromTonic == 5 || semitoneFromTonic == 10)
        {
            return new[] { 0, 4, 7 };
        }

        if (semitoneFromTonic == 9)
        {
            return new[] { 0, 3, 6 };
        }

        return new[] { 0, 3, 7 };
    }

    private void BuildProceduralPhrase()
    {
        if (useComposedPhrase)
        {
            BuildComposedPhrase();
            return;
        }

        melodyMidi.Clear();

        int degree = 3;
        int octave = 0;

        for (int i = 0; i < notesPerPhrase; i++)
        {
            int barPosition = i % 4;
            int step = GetStepSizeForPosition(barPosition);

            degree += step;
            NormalizeDegree(ref degree, ref octave);

            int midi = tonicMidi + dorian[degree] + (octave * 12);
            midi = Mathf.Clamp(midi, minMidi, maxMidi);
            melodyMidi.Add(midi);
        }
    }

    private void BuildComposedPhrase()
    {
        melodyMidi.Clear();

        int[] progression = eerieMode ? new[] { 0, 5, 3, 4 } : new[] { 0, 5, 2, 4 };
        int noteCount = Mathf.Max(4, notesPerPhrase);

        for (int i = 0; i < noteCount; i++)
        {
            int beatInBar = i % 4;
            int bar = i / 4;
            int currentDegree = progression[bar % progression.Length];
            int nextDegree = progression[(bar + 1) % progression.Length];

            int root = ScaleDegreeToMidi(currentDegree, 0, false);
            int third = ScaleDegreeToMidi(currentDegree + 2, 0, false);
            int fifth = ScaleDegreeToMidi(currentDegree + 4, 0, false);

            int note;
            if (beatInBar == 0)
            {
                note = root;
            }
            else if (beatInBar == 1)
            {
                note = fifth;
            }
            else if (beatInBar == 2)
            {
                note = third;
            }
            else
            {
                int nextRoot = ScaleDegreeToMidi(nextDegree, 0, useHarmonicMinorLeadingTone);
                int approach = nextRoot > root ? -1 : 1;
                note = nextRoot + approach;
            }

            note = Mathf.Clamp(note, minMidi, maxMidi);
            melodyMidi.Add(note);
        }
    }

    private int ScaleDegreeToMidi(int degree, int octaveOffset, bool favorLeadingTone)
    {
        int[] scale = favorLeadingTone ? harmonicMinor : naturalMinor;
        int wrappedDegree = degree;
        int wrappedOctave = octaveOffset;

        while (wrappedDegree < 0)
        {
            wrappedDegree += scale.Length;
            wrappedOctave--;
        }

        while (wrappedDegree >= scale.Length)
        {
            wrappedDegree -= scale.Length;
            wrappedOctave++;
        }

        return tonicMidi + scale[wrappedDegree] + (wrappedOctave * 12);
    }

    private int GetStepSizeForPosition(int barPosition)
    {
        if (barPosition == 0)
        {
            return rng.Next(-1, 2);
        }

        if (barPosition == 3)
        {
            return rng.Next(-2, 3);
        }

        return rng.Next(-1, 3);
    }

    private void NormalizeDegree(ref int degree, ref int octave)
    {
        while (degree < 0)
        {
            degree += dorian.Length;
            octave--;
        }

        while (degree >= dorian.Length)
        {
            degree -= dorian.Length;
            octave++;
        }
    }

    private AudioClip GetClipForMidi(int midi, out float pitch)
    {
        if (TryGetBestSampleZone(midi, out AudioClip zoneClip, out int zoneRootMidi))
        {
            pitch = Mathf.Pow(2f, (midi - zoneRootMidi) / 12f);
            return zoneClip;
        }

        throw new InvalidOperationException("PipeOrganSoundController: Missing sample for playback. Assign clips in organSampleZones.");
    }

    private bool TryGetBestSampleZone(int midi, out AudioClip clip, out int rootMidi)
    {
        clip = null;
        rootMidi = 60;

        int bestDistance = int.MaxValue;
        bool found = false;

        for (int i = 0; i < organSampleZones.Count; i++)
        {
            OrganSampleZone zone = organSampleZones[i];
            if (zone.clip == null)
            {
                continue;
            }

            int distance = Mathf.Abs(midi - zone.rootMidi);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                clip = zone.clip;
                rootMidi = zone.rootMidi;
                found = true;
            }
        }

        return found;
    }

    private bool HasValidSampleZones()
    {
        for (int i = 0; i < organSampleZones.Count; i++)
        {
            if (organSampleZones[i].clip != null)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureDefaultSampleZones()
    {
        if (organSampleZones.Count == DefaultRootMidis.Length && MatchesRootLayout(LegacyDefaultRootMidis))
        {
            for (int i = 0; i < DefaultRootMidis.Length; i++)
            {
                OrganSampleZone zone = organSampleZones[i];
                zone.rootMidi = DefaultRootMidis[i];
                organSampleZones[i] = zone;
            }

            return;
        }

        if (organSampleZones.Count > 0)
        {
            return;
        }

        for (int i = 0; i < DefaultRootMidis.Length; i++)
        {
            organSampleZones.Add(new OrganSampleZone
            {
                clip = null,
                rootMidi = DefaultRootMidis[i]
            });
        }
    }

    private bool MatchesRootLayout(int[] layout)
    {
        if (organSampleZones.Count != layout.Length)
        {
            return false;
        }

        for (int i = 0; i < layout.Length; i++)
        {
            if (organSampleZones[i].rootMidi != layout[i])
            {
                return false;
            }
        }

        return true;
    }

    private void StopChord(List<AudioSource> sources, bool immediate)
    {
        for (int i = 0; i < sources.Count; i++)
        {
            AudioSource source = sources[i];
            if (source == null)
            {
                continue;
            }

            if (immediate || releaseTime <= 0.001f)
            {
                source.Stop();
                Destroy(source);
                continue;
            }

            StartCoroutine(FadeAndStop(source, releaseTime));
        }

        sources.Clear();
    }

    private System.Collections.IEnumerator FadeAndStop(AudioSource source, float duration)
    {
        if (source == null)
        {
            yield break;
        }

        float initialVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration && source != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            source.volume = Mathf.Lerp(initialVolume, 0f, t);
            yield return null;
        }

        if (source != null)
        {
            source.Stop();
            Destroy(source);
        }
    }

    private AudioSource CreateVoiceSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.loop = true;
        source.outputAudioMixerGroup = outputMixerGroup;
        return source;
    }

    private int Mod(int value, int mod)
    {
        int result = value % mod;
        return result < 0 ? result + mod : result;
    }

}
