using System;
using System.Collections.Generic;

namespace BA2LW.Core
{
    [Serializable]
    public class Setting
    {
        public bool debug;
        public string student;
        public bool rotation;
        public float scale;
        public float lookRange;
        public List<string> imageList = new List<string>();
        public Bone bone;
        public Bg bg;
        public Pat pat;
        public Bgm bgm;
        public Sfx sfx;
        public Talk talk;

        [Serializable]
        public class Pat
        {
            public float range;
            public bool somethingWrong;
        }

        [Serializable]
        public class Bgm
        {
            public bool enable;
            public float volume;
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
            public float volume;
            public bool onlyTalk;
            public int maxIndex;
            public List<string> voiceList = new List<string>();
        }

        [Serializable]
        public class Bone
        {
            public string eyeL;
            public string eyeR;
            public string halo;
            public string neck;
        }

        [Serializable]
        public class Bg
        {
            public bool isSpine;
            public string name;
            public State state;
            public List<string> imageList = new List<string>();
        }

        [Serializable]
        public class State
        {
            public bool more;
            public string name;
        }
    }
}
