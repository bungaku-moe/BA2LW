using System;
using System.Collections.Generic;
using UnityEngine;

namespace BA2LW.Serialization
{
    [Serializable]
    public class SpineSettings
    {
        /// <summary>
        /// Base Student file name without extension.
        /// </summary>
        public string student;

        /// <summary>
        /// Camera rotation is equated with Pat angle.
        /// </summary>
        public bool rotateCamera;

        [Tooltip("Spine scale")]
        /// <summary>
        /// Spine Animation scale.
        /// </summary>
        public float scale;

        /// <summary>
        /// How far the eyes rotate when looking.
        /// </summary>
        public float lookRange;

        /// <summary>
        /// List of textures file name without extension.
        /// </summary>
        public List<string> textures = new List<string>();

        /// <summary>
        /// Required Bones to calculate Pat & Talk button.
        /// </summary>
        public Bones bones;
        public Bg bg;

        /// <summary>
        /// Pat settings.
        /// </summary>
        public Pat pat;

        /// <summary>
        /// Background music settings.
        /// </summary>
        public Bgm bgm;
        public Sfx sfx;
        public Talk talk;

        [Serializable]
        public class Pat
        {
            /// <summary>
            /// How much the Head rotate while Patting.
            /// </summary>
            public float rotateRange;

            /// <summary>
            /// Enable this if the Head isn't rotating while Patting.
            /// </summary>
            public bool fixRotation;
        }

        [Serializable]
        public class Bgm
        {
            /// <summary>
            /// Enable Background Music?
            /// </summary>
            public bool enable;

            /// <summary>
            /// Background Music volume.
            /// </summary>
            public float volume;

            /// <summary>
            /// Background Music file name.
            /// </summary>
            public string clip;
        }

        [Serializable]
        public class Sfx
        {
            public bool enable;
            public string name;
            public float volume;
        }

        [Serializable]
        public class Talk
        {
            /// <summary>
            /// Talk voice folder name.
            /// </summary>
            public string voiceDirectory;

            /// <summary>
            /// Talk voice volume.
            /// </summary>
            public float volume;

            /// <summary>
            /// Execute only the Talk event or all events that has the same name as the Talk voices.
            /// </summary>
            public bool onlyTalk;
        }

        [Serializable]
        public class Bones
        {
            /// <summary>
            /// The Left eye. Used along rightEye to calculate the Pat angle.
            /// </summary>
            public string eyeL;

            /// <summary>
            /// The Right eye. Used with leftEye to calculate the Pat angle.
            /// </summary>
            public string eyeR;

            /// <summary>
            /// The Halo. Used to calculate Pat button size & position.
            /// </summary>
            public string halo;

            /// <summary>
            /// The Neck. Used to calculate Pat & Talk button.
            /// </summary>
            public string neck;

            /// <summary>
            /// The Hip. Used to calculate Talk button.
            /// </summary>
            public string hip;
        }

        [Serializable]
        public class Bg
        {
            public bool isSpine;
            public string name;
            public State state;
            public List<string> textures = new List<string>();
        }

        [Serializable]
        public class State
        {
            public bool more;
            public string name;
        }
    }
}
