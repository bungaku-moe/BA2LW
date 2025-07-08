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
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BA2LW.Core
{
    [AddComponentMenu("BA2LW/Core/Main Control")]
    public class MainControl : MonoBehaviour
    {
        #region Components

        [Header("Spine")] [SerializeField] private GameObject m_CharacterBase;

        [SerializeField] private GameObject m_BackgroundBase,
            m_RotationBase;

        [SerializeField] private Shader m_SpineShader;

        [SerializeField, Range(0.01f, 0.02f)] private float m_SpineScaleMultiplier = 0.0115f;

        private SkeletonAnimation sprAnimation,
            bgAnimation;

        private Bone lookBone,
            patBone;

        [Header("Components")] [SerializeField]
        private Button m_PatButton;

        [SerializeField] private Button m_TalkButton;

        [SerializeField] private AudioSource m_BGMAudioSource,
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

        [SerializeField] private float lookSpeed = 4f;
        [SerializeField] private float lookRange = 1f;

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

        [Header("UI")] [SerializeField] private Canvas m_MainCanvas;

        [SerializeField] private ScrollRect m_DebugWindow;

        [SerializeField] private TextMeshProUGUI m_DebugText;

        [SerializeField] private RectTransform m_BoneIndicatorPrefab;

        private SettingsManager settingsManager;
        private GlobalConfig config => settingsManager.GlobalConfig;
        private SpineSettings settings => settingsManager.Settings;
        private InputManager inputManager;

        // Accessors
        private GameObject RotationBase => m_BackgroundBase;
        private Dictionary<Bone, RectTransform> _bones = new Dictionary<Bone, RectTransform>();

        private float LookRange
        {
            get => lookRange * 10;
            set => lookRange = value;
        }

        private float LookSpeed
        {
            get => lookSpeed * 10;
            set => lookSpeed = value / 10;
        }

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
            lookRange = settings.eyes.lookRange;
            lookSpeed = settings.eyes.lookSpeed;

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
                SetupPatAndTalkButton();
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
                bgAnimation.AnimationState.Data.DefaultMix = 0.5f;

                // TrackEntry bgIntro = sprAnimation.AnimationState.SetAnimation(
                //     0,
                //     $"Start_{settings.bg.state.name}",
                //     false
                // );
                // Log.Info($"Start background intro: {bgIntro}");
                // bgIntro.Complete += trackEntry => Log.Info($"End background intro: {trackEntry}");

                // Queue idle animation and play continuously
                bgAnimation.AnimationState.AddAnimation(0, "Idle_01", true, 0);

                if (settings.bg.state.more)
                    bgAnimation.AnimationState.SetAnimation(1, settings.bg.state.name, true);
            }

            // Debug Window
            Log.Info($"Debug Mode Enabled: {config.debug}");
            IsDebug(config.debug);

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
            SetupLook();
        }

        #endregion

        private void Update()
        {
            foreach (KeyValuePair<Bone, RectTransform> bone in _bones)
            {
                if (bone.Key == null || bone.Value == null)
                    continue;

                // Update the position of the bone indicator
                bone.Value.localPosition = GetBoneScreenPosition(sprAnimation, bone.Key);
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
            Vector3 mouseDownPoint = m_RotationBase.transform.InverseTransformPoint(
                worldMousePosition
            );

            if (!isTalking)
            {
                if (isPatting)
                {
                    if (mouseDownPoint.x - pat.x >= patRange)
                    {
                        mouseDownPoint.x = pat.x + patRange;
                    }
                    else if (mouseDownPoint.x - pat.x <= -patRange)
                    {
                        mouseDownPoint.x = pat.x - patRange;
                    }

                    mouseDownPoint.y = pat.y;

                    mouseDownPoint = RotationBase.transform.TransformPoint(mouseDownPoint);

                    patBone.SetPositionSkeletonSpace(mouseDownPoint);
                }
                else if (patEnding)
                {
                    if (math.abs(patBone.X - pat.x) <= 0.1f)
                    {
                        patEnding = false;
                        patBone.SetToSetupPose();
                    }
                    else
                    {
                        Vector3 tempPosition = Vector3.MoveTowards(patBone.GetWorldPosition(sprAnimation.transform),
                            RotationBase.transform.TransformPoint(pat), patSpeed * Time.deltaTime);
                        patBone.SetPositionSkeletonSpace(tempPosition);
                    }
                }

                if (isLooking)
                {
                    // Get current screen-space position of the eye origin
                    Vector3 screenLook = Camera.main.WorldToScreenPoint(RotationBase.transform.TransformPoint(look));

                    // Calculate offset direction from eye origin to mouse
                    Vector3 direction = (Vector3)mousePosition - screenLook;

                    // Clamp that direction vector to a maximum radius (in pixels)
                    Vector3 clampedDirection = Vector2.ClampMagnitude(direction, LookRange);

                    // Compute the target eye screen position
                    Vector3 targetScreenPos = screenLook + clampedDirection;

                    // Convert the target screen point to world space
                    Vector3 worldTarget =
                        Camera.main.ScreenToWorldPoint(new Vector3(targetScreenPos.x, targetScreenPos.y, screenLook.z));

                    // Smooth move the eye toward the target
                    Vector3 currentEyeWorld = lookBone.GetWorldPosition(sprAnimation.transform);
                    Vector3 smoothWorldTarget = Vector3.MoveTowards(currentEyeWorld, worldTarget,
                        LookSpeed * Time.smoothDeltaTime);

                    // Convert to skeleton space
                    lookBone.SetPositionSkeletonSpace(RotationBase.transform.InverseTransformPoint(smoothWorldTarget));
                }
                else if (lookEnding)
                {
                    Vector3 current = lookBone.GetWorldPosition(sprAnimation.transform);
                    Vector3 target = RotationBase.transform.TransformPoint(look);

                    if (Vector3.Distance(current, target) <= 0.01f)
                    {
                        lookEnding = false;
                        lookBone.SetToSetupPose();
                    }
                    else
                    {
                        Vector3 tempPosition = Vector3.MoveTowards(current, target, LookSpeed * Time.smoothDeltaTime);
                        lookBone.SetPositionSkeletonSpace(RotationBase.transform.InverseTransformPoint(tempPosition));
                    }
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

        public void Looking(bool isLooking)
        {
            if (isTalking || !allowInteraction)
                return;

            Log.Info($"Look At Mouse: {isLooking}");
            this.isLooking = isLooking;

            if (isLooking)
            {
                if (lookA != null)
                {
                    sprAnimation.AnimationState.AddEmptyAnimation(1, 0.5f, 0);
                    // sprAnimation.AnimationState.AddAnimation(1, lookA, false, 0);
                    sprAnimation.AnimationState.SetAnimation(1, lookA, false);
                }

                if (lookM != null)
                {
                    sprAnimation.AnimationState.AddEmptyAnimation(2, 0.5f, 0);
                    // sprAnimation.AnimationState.AddAnimation(2, lookM, false, 0);
                    sprAnimation.AnimationState.SetAnimation(2, lookM, false);
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
            {
                Camera.main.transform.localRotation = Quaternion.Euler(0, 0, patAngle);
                leftEye = SpineHelper.BoneScreenPosition(sprAnimation, settings.bones.eyeL);
                rightEye = SpineHelper.BoneScreenPosition(sprAnimation, settings.bones.eyeR);
            }
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

            // Pat Button
            Vector3 patButtonPosition = SpineHelper.GetMidpoint(halo, neck);
            Vector2 patButtonSize = SpineHelper.GetDistance(halo, neck) * Vector2.one;
            m_PatButton.transform.GetComponent<RectTransform>().sizeDelta = patButtonSize;
            m_PatButton.transform.localPosition = patButtonPosition;

            // Talk Button
            m_TalkButton.transform.GetComponent<RectTransform>().sizeDelta =
                SpineHelper.GetDistance(neck, hip) * (Vector2.one * 1.2f);
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

        /// <summary>
        /// Show Debug Window?
        /// </summary>
        /// <param name="debug"></param>
        private void IsDebug(bool debug)
        {
            if (!debug)
                return;

            m_PatButton.GetComponent<Image>().color = new Color(0, 255, 253, 0.35f);
            m_TalkButton.GetComponent<Image>().color = new Color(255, 0, 0, 0.35f);

            m_DebugText.text += "<b>Animations (Spr):</b>\n";
            foreach (Spine.Animation animation in sprAnimation.skeleton.Data.Animations)
                m_DebugText.text += $"● {animation.Name}\n";

            if (settings.bg.isSpine)
            {
                m_DebugText.text += "\n<b>Animations (Bg):</b>\n";
                foreach (Spine.Animation animation in bgAnimation.skeleton.Data.Animations)
                    m_DebugText.text += $"● {animation.Name}\n";
            }

            m_DebugText.text += "\n<b>Events:</b>\n";
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

                    // // Convert bone's world position to screen position
                    // Vector3 boneWorldPosition = sprAnimation.transform.TransformPoint(
                    //     bone.GetWorldPosition(sprAnimation.transform)
                    // );
                    // Vector3 screenPosition = Camera.main.WorldToScreenPoint(boneWorldPosition);
                    //
                    // // Convert screen position to UI canvas position
                    // RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    //     m_DebugWindow.transform as RectTransform,
                    //     screenPosition,
                    //     Camera.main,
                    //     out Vector2 localPoint
                    // );
                    _bones.Add(bone, boneIndicator);
                    boneIndicator.localPosition = GetBoneScreenPosition(sprAnimation, bone);
                }
            }
        }

        private Vector2 GetBoneScreenPosition(SkeletonAnimation skeletonAnimation, Bone bone)
        {
            // Convert bone's world position to screen position
            Vector3 boneWorldPosition = skeletonAnimation.transform.TransformPoint(
                bone.GetWorldPosition(skeletonAnimation.transform)
            );
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(boneWorldPosition);

            // Convert screen position to UI canvas position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_DebugWindow.transform as RectTransform,
                screenPosition,
                Camera.main,
                out var localPoint
            );

            return localPoint;
        }

        /// <summary>
        /// Calculate the total index number of voices available.
        /// </summary>
        /// <param name="fileNames"></param>
        /// <returns></returns>
        private int GetTalkTotalVoices(string[] fileNames)
        {
            int[] indexes = { -1, 0, 0 };
            foreach (string name in fileNames)
            {
                string[] nameSplit = name.Split('_');
                int nameSplitLength = nameSplit.Length;

                if (nameSplitLength > 1)
                {
                    if (int.TryParse(nameSplit[nameSplitLength - 1], out indexes[1]))
                    {
                        if (int.TryParse(nameSplit[nameSplitLength - 2], out indexes[2]))
                        {
                            if (indexes[2] > indexes[0])
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
