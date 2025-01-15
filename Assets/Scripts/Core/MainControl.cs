using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BA2LW.Serialization;
using BA2LW.Utils;
using Cysharp.Threading.Tasks;
using Gilzoide.SerializableCollections;
using Spine;
using Spine.Unity;
using TMPro;
using Unity.Logging;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace BA2LW.Core
{
    [AddComponentMenu("BA2LW/Core/Main Control")]
    public class MainControl : MonoBehaviour
    {
        #region Components
        [Header("Spine")]
        [SerializeField]
        private GameObject m_CharacterBase;

        [SerializeField]
        private GameObject m_BackgroundBase,
            m_RotationBase;

        [SerializeField]
        private Shader m_SpineShader;

        [SerializeField, Range(0.01f, 0.02f)]
        private float m_SpineScaleMultiplier = 0.0115f;
        private SkeletonAnimation sprAnimation,
            bgAnimation;
        private Bone lookBone,
            patBone;

        [Header("Components")]
        [SerializeField]
        private Button m_PatButton;

        [SerializeField]
        private Button m_TalkButton;

        [SerializeField]
        private AudioSource m_BGMAudioSource,
            m_SFXAudioSource,
            m_VoiceAudioSource;

        private bool allowInteraction;

        //! Talk
        private bool isTalking;
        private int voiceIndex = 1,
            secondVoiceIndex = 1,
            totalVoice;
        private SerializableDictionary<string, AudioClip> voiceList =
            new SerializableDictionary<string, AudioClip>();

        //! Look
        private bool isLooking,
            lookEnding;
        private float lookSpeed = 4f,
            lookRange = 1f;

        /// <summary>
        /// Current eyes looking rotation.
        /// </summary>
        private Vector3 look;

        /// <summary>
        /// Eyes blinking initial state animation.
        /// </summary>
        private string lookA;

        /// <summary>
        /// Eyes turn left and right initial state animation.
        /// </summary>
        private string lookM;

        /// <summary>
        /// Eyes blinking animation.
        /// </summary>
        private string lookEndA;

        /// <summary>
        /// Eyes turn left and right animation.
        /// </summary>
        private string lookEndM;

        //! Pat
        private bool isPatting,
            isFirstPat,
            patEnding;
        private float patSpeed = 2f,
            patRange = 0.5f;
        private Vector3 pat;
        private string patA,
            patM,
            patEndA,
            patEndM;

        [Header("UI")]
        [SerializeField]
        private Canvas m_MainCanvas;

        [SerializeField]
        private ScrollRect m_DebugWindow;

        [SerializeField]
        private TextMeshProUGUI m_DebugText;

        [SerializeField]
        private RectTransform m_BoneIndicatorPrefab;

        private SettingsManager settingsManager;
        private GlobalConfig config => settingsManager.GlobalConfig;
        private SpineSettings settings => settingsManager.Settings;

        private InputManager inputManager;
        #endregion

        #region Initialization
        private void Awake()
        {
            inputManager = FindFirstObjectByType<InputManager>();
            settingsManager = FindFirstObjectByType<SettingsManager>();
        }

        public async UniTask Initialize()
        {
            m_DebugText.text = string.Empty;

            Log.Info("Initializing Components...");
            m_BGMAudioSource.gameObject.SetActive(settings.bgm.enable);
            m_SFXAudioSource.gameObject.SetActive(settings.sfx.enable);
            m_VoiceAudioSource.gameObject.SetActive(settings.talk.voiceDirectory != string.Empty);
            m_DebugWindow.gameObject.SetActive(config.debug);

            // Init properties value
            patRange = settings.pat.rotateRange;
            lookRange = settings.lookRange;

            // Setup BGM Audio Source if enabled
            if (settings.bgm.enable)
            {
                Log.Info("Setting up & Caching BGM...");
                string bgmPath = Path.Combine(
                    settingsManager.CurrentWallpaperPath,
                    settings.bgm.clip
                );

                // Cache the audio clip...
                m_BGMAudioSource.clip = await WebRequestHelper.GetAudioClip(bgmPath);
                m_BGMAudioSource.volume = settings.bgm.volume;
                m_BGMAudioSource.loop = true;
            }

            // Setup SFX Audio Source if enabled
            if (settings.sfx.enable)
            {
                Log.Info("Setting up & Caching SFX...");
                string sfxPath = Path.Combine(
                    settingsManager.CurrentWallpaperPath,
                    settings.sfx.name
                );

                // Cache the audio clip...
                m_SFXAudioSource.clip = await WebRequestHelper.GetAudioClip(sfxPath);
                m_SFXAudioSource.volume = settings.sfx.volume;
                m_SFXAudioSource.loop = false;
            }

            if (settings.talk.voiceDirectory != string.Empty)
            {
                Log.Info("Setting up & Caching Character Voices...");
                string voicePath = Path.Combine(
                    settingsManager.CurrentWallpaperPath,
                    settings.talk.voiceDirectory
                );

                if (Directory.Exists(voicePath))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(voicePath);
                    FileInfo[] files = directoryInfo.GetFiles();

                    foreach (FileInfo file in files)
                    {
                        if (file.Name.ToLower().Contains("memoriallobby"))
                        {
                            AudioClip clip = await WebRequestHelper.GetAudioClip(file.FullName);
                            voiceList.Add(file.Name.Replace(".ogg", ""), clip);
                        }
                    }

                    totalVoice = GetTalkTotalVoices(voiceList.Keys.ToArray());
                }
            }

            // Instantiate character spine
            Log.Info("Setting up Character Spine...");
            sprAnimation = await SpineHelper.InstantiateSpine(
                settingsManager.CurrentWallpaperPath,
                settings.student,
                settings.textures,
                m_CharacterBase,
                m_SpineShader,
                settings.scale,
                m_SpineScaleMultiplier
            );
            sprAnimation.AnimationState.Data.DefaultMix = 0.5f;

            // Start intro animation and disallow any interaction until it completed.
            TrackEntry sprIntro = sprAnimation.AnimationState.SetAnimation(
                0,
                "Start_Idle_01",
                false
            );
            Log.Info($"Start character intro: {sprIntro}");
            sprIntro.Complete += trackEntry =>
            {
                Log.Info($"End character intro: {trackEntry}");
                allowInteraction = true;
            };

            // Queue idle animation and play it continuously
            sprAnimation.AnimationState.AddAnimation(0, "Idle_01", true, 0);

            // If has spine background
            if (settings.bg.isSpine)
            {
                Log.Info("Setting up Background Spine...");
                // Instantiate background spine
                bgAnimation = await SpineHelper.InstantiateSpine(
                    settingsManager.CurrentWallpaperPath,
                    settings.bg.name,
                    settings.bg.textures,
                    m_BackgroundBase,
                    m_SpineShader,
                    settings.scale,
                    m_SpineScaleMultiplier
                );
                bgAnimation.AnimationState.Data.DefaultMix = 0.7f;

                TrackEntry bgIntro = sprAnimation.AnimationState.AddAnimation(
                    0,
                    $"Start_{settings.bg.state.name}",
                    false,
                    0
                );
                bgIntro.Complete += trackEntry => Log.Info($"End background intro: {trackEntry}");
                Log.Info($"Start background intro: {bgIntro}");

                // Queue idle animation and play continuously
                bgAnimation.AnimationState.AddAnimation(0, "Idle_01", true, 0);

                if (settings.bg.state.more)
                    bgAnimation.AnimationState.SetAnimation(1, settings.bg.state.name, true);

                if (config.debug)
                {
                    foreach (Spine.Animation animation in bgAnimation.skeleton.Data.Animations)
                        m_DebugText.text += $"{animation.Name}\n";
                }
            }

            // Debug Window
            Log.Info($"Debug Mode Enabled: {config.debug}");
            Debug(config.debug);

            // Play Audio Source after all Spine initialized
            Log.Info("Enabling Audio\'s...");
            if (settings.bgm.enable)
                m_BGMAudioSource.Play();
            if (settings.sfx.enable)
                m_SFXAudioSource.Play();

            Log.Info("Registering Interaction Events...");
            sprAnimation.AnimationState.Event += HandleEvent;
            void HandleEvent(TrackEntry trackEntry, Spine.Event spineEvent)
            {
                if (settings.talk.onlyTalk)
                {
                    if (spineEvent.Data.Name == "Talk")
                    {
                        foreach (string voice in voiceList.Keys)
                        {
                            if (voice.ToLower().EndsWith($"memoriallobby_{voiceIndex - 1}"))
                            {
                                Log.Info($"Talk: {trackEntry} -> {voice}");
                                m_VoiceAudioSource.clip = voiceList[voice];
                                m_VoiceAudioSource.Play();
                                break;
                            }
                            else if (
                                voice
                                    .ToLower()
                                    .EndsWith($"memoriallobby_{voiceIndex - 1}_{secondVoiceIndex}")
                            )
                            {
                                Log.Info($"Talk: {trackEntry} -> {voice}");
                                m_VoiceAudioSource.clip = voiceList[voice];
                                m_VoiceAudioSource.Play();
                                secondVoiceIndex++;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (string key in voiceList.Keys)
                    {
                        if (spineEvent.Data.Name.Contains(key))
                        {
                            m_VoiceAudioSource.clip = voiceList[key];
                            m_VoiceAudioSource.Play();
                            break;
                        }
                    }
                }
            }

            Log.Info("Setting up Interaction Components...");
            SetupPatAndTalkButton();
            SetupLook();
        }
        #endregion

        private void Update()
        {
            if (!allowInteraction || isTalking)
                return;

            Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(
                inputManager.PointerPosition
            );
            Vector2 mouseDownPoint = m_RotationBase.transform.InverseTransformPoint(
                worldMousePosition
            );

            // Patting...
            if (isPatting)
            {
                mouseDownPoint = m_RotationBase.transform.TransformPoint(
                    new Vector2(
                        Mathf.Clamp(mouseDownPoint.x, pat.x - patRange, pat.x + patRange),
                        pat.y
                    )
                );
                patBone.SetPositionSkeletonSpace(mouseDownPoint);
            }
            else if (patEnding)
            {
                // If the absolute difference between patBone.X and pat.x is <= to the threshold or tolerance...
                if (Mathf.Abs(patBone.X - pat.x) <= 0.1f)
                {
                    patEnding = false;
                    patBone.SetToSetupPose();
                }
                else
                {
                    Vector3 patBonePosition = Vector3.MoveTowards(
                        patBone.GetWorldPosition(sprAnimation.transform),
                        m_RotationBase.transform.TransformPoint(pat),
                        patSpeed * Time.smoothDeltaTime
                    );
                    patBone.SetPositionSkeletonSpace(patBonePosition);
                }
            }

            // Looking...
            if (isLooking)
            {
                float sx = (mouseDownPoint.y - look.y) / (mouseDownPoint.x - look.x);
                float sy = (mouseDownPoint.x - look.x) / (mouseDownPoint.y - look.y);

                if (mouseDownPoint.x - look.x >= lookRange && Math.Abs(sx) <= 1)
                {
                    mouseDownPoint.y = look.y + lookRange * sx;
                    mouseDownPoint.x = look.x + lookRange;
                }
                else if (mouseDownPoint.x - look.x <= -lookRange && Math.Abs(sx) <= 1)
                {
                    mouseDownPoint.y = look.y - lookRange * sx;
                    mouseDownPoint.x = look.x - lookRange;
                }
                else if (mouseDownPoint.y - look.y >= lookRange && Math.Abs(sx) > 1)
                {
                    mouseDownPoint.y = look.y + lookRange;
                    mouseDownPoint.x = look.x + lookRange * sy;
                }
                else if (mouseDownPoint.y - look.y <= -lookRange && Math.Abs(sx) > 1)
                {
                    mouseDownPoint.y = look.y - lookRange;
                    mouseDownPoint.x = look.x - lookRange * sy;
                }

                mouseDownPoint = m_RotationBase.transform.TransformPoint(mouseDownPoint);
                lookBone.SetPositionSkeletonSpace(mouseDownPoint);
            }
            else if (lookEnding)
            {
                if (math.abs(lookBone.X - look.x) <= 0.1f)
                {
                    lookEnding = false;
                    lookBone.SetToSetupPose();
                }
                else
                {
                    Vector3 lookBonePosition = Vector3.MoveTowards(
                        lookBone.GetWorldPosition(sprAnimation.transform),
                        m_RotationBase.transform.TransformPoint(look),
                        lookSpeed * Time.smoothDeltaTime
                    );
                    lookBone.SetPositionSkeletonSpace(lookBonePosition);
                }
            }
        }

        /// <summary>
        /// Play talking animation.
        /// </summary>
        public void Talking()
        {
            if (!isTalking && allowInteraction)
            {
                allowInteraction = false;
                isTalking = true;
                voiceIndex = voiceIndex > totalVoice ? 1 : voiceIndex;
                secondVoiceIndex = 1;

                // Set empty animations to fade out previous animations
                sprAnimation.AnimationState.AddEmptyAnimation(3, 0.5f, 0);
                sprAnimation.AnimationState.AddEmptyAnimation(4, 0.5f, 0);

                // Add new animations with blending
                sprAnimation.AnimationState.AddAnimation(3, $"Talk_0{voiceIndex}_A", false, 0);
                sprAnimation.AnimationState.AddAnimation(4, $"Talk_0{voiceIndex}_M", false, 0);

                sprAnimation.AnimationState.AddEmptyAnimation(3, 0.5f, 0);
                sprAnimation.AnimationState.AddEmptyAnimation(4, 0.5f, 0).Complete += _ =>
                {
                    allowInteraction = true;
                    isTalking = false;
                };
                voiceIndex++;
            }
        }

        public void Looking(bool state)
        {
            if (isTalking || !allowInteraction)
                return;

            Log.Info($"Look At Mouse: {state}");
            isLooking = state;

            if (state)
            {
                if (lookA != null)
                {
                    sprAnimation.AnimationState.AddEmptyAnimation(1, 0.5f, 0);
                    sprAnimation.AnimationState.AddAnimation(1, lookA, false, 0);
                }
                if (lookM != null)
                {
                    sprAnimation.AnimationState.AddEmptyAnimation(2, 0.5f, 0);
                    sprAnimation.AnimationState.AddAnimation(2, lookM, false, 0);
                }
            }
            else
            {
                if (lookEndA != null)
                {
                    sprAnimation.AnimationState.AddEmptyAnimation(1, 1f, 0);
                    sprAnimation.AnimationState.AddAnimation(1, lookEndA, false, 0);
                }
                if (lookEndM != null)
                {
                    sprAnimation.AnimationState.AddEmptyAnimation(2, 1f, 0);
                    sprAnimation.AnimationState.AddAnimation(2, lookEndM, false, 0);
                }
                sprAnimation.AnimationState.AddEmptyAnimation(1, 0.5f, 0);
                sprAnimation.AnimationState.AddEmptyAnimation(2, 0.5f, 0);

                lookEnding = true;
            }
        }

        public void Patting(bool patting)
        {
            if (!isTalking)
            {
                Log.Info($"Patting: {patting}");
                isPatting = patting;

                if (patting)
                {
                    if (!isFirstPat)
                    {
                        isFirstPat = true;

                        if (patA != null)
                        {
                            sprAnimation.AnimationState.AddEmptyAnimation(1, 0.25f, 0);
                            sprAnimation.AnimationState.AddAnimation(1, patA, false, 0);
                        }
                        if (patM != null)
                        {
                            sprAnimation.AnimationState.AddEmptyAnimation(2, 0.25f, 0);
                            sprAnimation.AnimationState.AddAnimation(2, patM, false, 0);
                            // if (settings.pat.fixRotation)
                            sprAnimation.AnimationState.AddEmptyAnimation(2, 0, 0);
                        }
                    }
                }
                else
                {
                    if (patEndA != null)
                    {
                        sprAnimation.AnimationState.AddEmptyAnimation(1, 0.35f, 0);
                        sprAnimation.AnimationState.AddAnimation(1, patEndA, false, 0);
                    }
                    if (patEndM != null)
                    {
                        sprAnimation.AnimationState.AddEmptyAnimation(2, 0.35f, 0);
                        sprAnimation.AnimationState.AddAnimation(2, patEndM, false, 0);
                    }
                    sprAnimation.AnimationState.AddEmptyAnimation(1, 0.35f, 0);
                    sprAnimation.AnimationState.AddEmptyAnimation(2, 0.35f, 0);

                    patEnding = true;
                    isFirstPat = false;
                }
            }
        }

        private void SetupPatAndTalkButton()
        {
            Vector3 leftEye = SpineHelper.BoneScreenPosition(
                sprAnimation,
                settings.bones.eyeL,
                m_MainCanvas.GetComponent<RectTransform>()
            );
            Vector3 rightEye = SpineHelper.BoneScreenPosition(
                sprAnimation,
                settings.bones.eyeR,
                m_MainCanvas.GetComponent<RectTransform>()
            );

            float patAngle = SpineHelper.GetAngle(leftEye, rightEye);
            m_RotationBase.transform.localRotation = Quaternion.Euler(0, 0, patAngle);

            if (settings.rotateCamera)
                Camera.main.transform.localRotation = Quaternion.Euler(0, 0, patAngle);
            else
            {
                m_PatButton.transform.localEulerAngles = new Vector3(0, 0, patAngle);
                m_TalkButton.transform.localEulerAngles = new Vector3(0, 0, patAngle);
            }

            Vector3 halo = SpineHelper.BoneScreenPosition(
                sprAnimation,
                settings.bones.halo,
                m_MainCanvas.GetComponent<RectTransform>()
            );
            Vector3 head = SpineHelper.BoneScreenPosition(
                sprAnimation,
                "Head_Rot",
                m_MainCanvas.GetComponent<RectTransform>()
            );
            Vector3 neck = SpineHelper.BoneScreenPosition(
                sprAnimation,
                settings.bones.neck,
                m_MainCanvas.GetComponent<RectTransform>()
            );
            Vector3 hip = SpineHelper.BoneScreenPosition(
                sprAnimation,
                settings.bones.hip,
                m_MainCanvas.GetComponent<RectTransform>()
            );

            Vector3 patButtonPosition = SpineHelper.GetMidpoint(halo, neck);
            Vector2 patButtonSize = SpineHelper.GetDistance(halo, neck) * Vector2.one;

            // Pat Button
            m_PatButton.transform.GetComponent<RectTransform>().sizeDelta = patButtonSize;
            m_PatButton.transform.localPosition = patButtonPosition;

            // Talk Button
            m_TalkButton.transform.GetComponent<RectTransform>().sizeDelta =
                SpineHelper.GetDistance(neck, hip) * Vector2.one * 1.2f;
            m_TalkButton.transform.localPosition = SpineHelper.GetMidpoint(neck, hip);

            foreach (Spine.Animation animation in sprAnimation.skeleton.Data.Animations)
            {
                if (animation.Name.StartsWith("Pat_0"))
                {
                    if (animation.Name.EndsWith("A"))
                        patA = animation.Name;
                    else if (animation.Name.EndsWith("M"))
                        patM = animation.Name;
                }
                else if (animation.Name.StartsWith("PatEnd"))
                {
                    if (animation.Name.EndsWith("A"))
                        patEndA = animation.Name;
                    else if (animation.Name.EndsWith("M"))
                        patEndM = animation.Name;
                }
            }

            patBone = sprAnimation.skeleton.FindBone("Touch_Point");
            pat = m_RotationBase.transform.InverseTransformPoint(
                patBone.GetWorldPosition(sprAnimation.transform)
            );
        }

        /// <summary>
        /// Setting up eyes looking bones.
        /// </summary>
        private void SetupLook()
        {
            foreach (Spine.Animation animation in sprAnimation.skeleton.Data.Animations)
            {
                if (animation.Name.StartsWith("Look_"))
                {
                    if (animation.Name.EndsWith("A"))
                        lookA = animation.Name;
                    else if (animation.Name.EndsWith("M"))
                        lookM = animation.Name;
                }
                else if (animation.Name.StartsWith("LookEnd"))
                {
                    if (animation.Name.EndsWith("A"))
                        lookEndA = animation.Name;
                    else if (animation.Name.EndsWith("M"))
                        lookEndM = animation.Name;
                }
            }

            lookBone = sprAnimation.skeleton.FindBone("Touch_Eye");
            look = m_RotationBase.transform.InverseTransformPoint(
                lookBone.GetWorldPosition(sprAnimation.transform)
            );
        }

        private void Debug(bool debug)
        {
            if (!debug)
                return;

            m_PatButton.GetComponent<Image>().color = new Color(0, 255, 253, 0.35f);
            m_TalkButton.GetComponent<Image>().color = new Color(255, 0, 0, 0.35f);

            m_DebugText.text += "<b>Events:</b>\n";
            foreach (EventData e in sprAnimation.skeleton.Data.Events)
                m_DebugText.text += $"● {e.Name}\n";

            m_DebugText.text += "\n<b>Bones (filtered):</b>\n";
            foreach (Bone bone in sprAnimation.Skeleton.Bones)
            {
                string boneName = bone.Data.Name;
                string boneNameLower = boneName.ToLower();

                if (
                    boneNameLower.Contains("eye")
                    || boneNameLower.Contains("halo")
                    || boneNameLower.Contains("neck")
                    || boneNameLower.Contains("hip")
                    || boneNameLower.Contains("hair")
                )
                {
                    // Debug window text
                    m_DebugText.text += $"● {bone.Data.Name}\n";

                    RectTransform boneIndicator = Instantiate(m_BoneIndicatorPrefab);
                    boneIndicator.SetParent(m_DebugWindow.transform, false);

                    boneIndicator.gameObject.name = boneName;
                    boneIndicator.GetComponentInChildren<TextMeshProUGUI>().text = boneName;

                    // Convert bone's world position to screen position
                    Vector3 boneWorldPosition = sprAnimation.transform.TransformPoint(
                        bone.GetWorldPosition(sprAnimation.transform)
                    );
                    Vector3 screenPosition = Camera.main.WorldToScreenPoint(boneWorldPosition);

                    // Convert screen position to UI canvas position
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        m_DebugWindow.transform as RectTransform,
                        screenPosition,
                        Camera.main,
                        out Vector2 localPoint
                    );
                    boneIndicator.localPosition = localPoint;
                }
            }
        }

        /// <summary>
        /// Calculate the total index number of voices available.
        /// </summary>
        /// <param name="fileNames"></param>
        /// <returns></returns>
        private int GetTalkTotalVoices(string[] fileNames)
        {
            int[] indexes = { 0, 0, 0 };
            foreach (string name in fileNames)
            {
                Log.Info(name);
                string[] nameSplit = name.Split('_');
                int nameSplitLength = nameSplit.Length;

                if (nameSplitLength > 1)
                {
                    if (int.TryParse(nameSplit[nameSplitLength - 1], out indexes[1]))
                    {
                        if (int.TryParse(nameSplit[nameSplitLength - 2], out indexes[2]))
                        {
                            if (indexes[2] > indexes[0] || indexes[2] == 0)
                            {
                                indexes[0]++;
                                continue;
                            }
                        }
                        else
                        {
                            indexes[0]++;
                            continue;
                        }
                    }
                    else
                        continue;
                }
            }
            return indexes[0];
        }
    }
}
