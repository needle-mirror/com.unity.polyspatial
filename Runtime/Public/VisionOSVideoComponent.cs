using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.Video;

namespace Unity.PolySpatial
{
    /// <summary>
    /// The mesh renderer on which the video should render. Overwrites the current material on the renderer.
    /// </summary>
    public class VisionOSVideoComponent : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The mesh renderer that this video clip will be applied to.")]
        MeshRenderer m_TargetMaterialRenderer;
        /// <summary>
        /// The mesh renderer on which the video should render. Overwrites the current material on the renderer.
        /// </summary>
        public MeshRenderer targetMaterialRenderer
        {
            get => m_TargetMaterialRenderer;
            set
            {
                m_TargetMaterialRenderer = value;
                ObjectBridge.MarkDirty(this);
            }
        }

        [SerializeField]
        [Tooltip("The video asset to play.")]
        VideoClip m_Clip;
        /// <summary>
        /// The video asset to play.
        /// </summary>
        public VideoClip clip
        {
            get => m_Clip;
            set
            {
                m_Clip = value;
                ObjectBridge.MarkDirty(this);
            }
        }

        [SerializeField]
        [Tooltip("Whether the video should repeat when playback reaches the end of the clip.")]
        bool m_IsLooping = true;
        /// <summary>
        /// Whether the video should repeat when playback reaches the end of the clip.
        /// </summary>
        public bool isLooping
        {
            get => m_IsLooping;
            set
            {
                m_IsLooping = value;
                ObjectBridge.MarkDirty(this);
            }
        }

        [SerializeField]
        [Tooltip("Whether video clip should play on awake.")]
        bool m_PlayOnAwake = true;
        /// <summary>
        /// Whether video clip should play on awake.
        /// </summary>
        public bool playOnAwake
        {
            get => m_PlayOnAwake;
            set
            {
                m_PlayOnAwake = value;
                ObjectBridge.MarkDirty(this);
            }
        }

        [SerializeField]
        [Tooltip("Mute status of first track on the video clip. Multiple tracks currently not supported")]
        bool m_Mute = false;

        [SerializeField]
        [Tooltip("Volume of the first track on the video clip. Multiple tracks currently not supported.")]
        [Range(0.0F, 1.0F)]
        float m_Volume = 1.0f;

        /// <summary>
        /// Enumerates to possible playing states of the video player. Use <see cref="GetState"/>
        /// to access the current state of a visionOS video component.
        /// </summary>
        public enum PlayerState: int
        {
            /// <summary>
            /// The video player is currently playing.
            /// </summary>
            IsPlaying,

            /// <summary>
            /// The video player is currently stopped.
            /// </summary>
            IsStopped,

            /// <summary>
            /// The video player is currently paused.
            /// </summary>
            IsPaused
        }

        PlayerState m_State = PlayerState.IsStopped;

        void Start()
        {
            if (playOnAwake)
                m_State = PlayerState.IsPlaying;
        }

        /// <summary>
        /// Obtain the current play state of the video player.
        /// </summary>
        /// <returns>The player state.</returns>
        public PlayerState GetState()
        {
            return m_State;
        }

        /// <summary>
        /// Starts playback of the video.
        /// </summary>
        public void Play()
        {
            if (m_State != PlayerState.IsPlaying)
            {
                m_State = PlayerState.IsPlaying;
                this.MarkDirty();
            }
        }

        /// <summary>
        /// Stops the playback of the video.
        /// </summary>
        public void Stop()
        {
            if (m_State != PlayerState.IsStopped)
            {
                m_State = PlayerState.IsStopped;
                this.MarkDirty();
            }

        }

        /// <summary>
        /// Pauses the playback of the video.
        /// </summary>
        public void Pause()
        {
            if (m_State != PlayerState.IsPaused)
            {
                m_State = PlayerState.IsPaused;
                this.MarkDirty();
            }
        }

        /// <summary>
        /// Gets the mute status of first track on the video clip.
        /// </summary>
        /// <param name="trackIndex">The track index (indices greater than 0 not supported).</param>
        /// <returns>The mute status of first track on the video clip.</returns>
        public bool GetDirectAudioMute(ushort trackIndex)
        {
            if (trackIndex > 0)
                Debug.LogWarning("trackIndices greater than 0 currently not supported.");

            return m_Mute;
        }

        /// <summary>
        /// Sets the mute state of the first track on the video clip.
        /// </summary>
        /// <param name="trackIndex">The track index (indices greater than 0 not supported).</param>
        /// <param name="mute">The mute state to set.</param>
        public void SetDirectAudioMute(ushort trackIndex, bool mute)
        {
            if (trackIndex > 0)
                Debug.LogWarning("trackIndices greater than 0 currently not supported.");

            m_Mute = mute;
            this.MarkDirty();
        }

        /// <summary>
        /// Gets the volume of the first track on the video clip.
        /// </summary>
        /// <param name="trackIndex">The track index (indices greater than 0 not supported).</param>
        /// <returns>The volume between 0.0 and 1.0</returns>
        public float GetDirectAudioVolume(ushort trackIndex)
        {
            if (trackIndex > 0)
                Debug.LogWarning("trackIndices greater than 0 currently not supported.");

            return m_Volume;
        }

        /// <summary>
        /// Sets the volume of the first track on the video clip.
        /// </summary>
        /// <param name="trackIndex">The track index (indices greater than 0 not supported).</param>
        /// <param name="volume">The volume to set between 0.0 and 1.0.</param>
        public void SetDirectAudioVolume(ushort trackIndex, float volume)
        {
            if (trackIndex > 0)
                Debug.LogWarning("trackIndices greater than 0 currently not supported.");

            m_Volume = Mathf.Clamp(volume, 0, 1);
            this.MarkDirty();
        }
    }
}
