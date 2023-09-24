using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BA2LW.Utils;
using Spine;
using Spine.Unity;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace BA2LW.Core
{
    [AddComponentMenu("BA2LW/Core/Main Control")]
    public class MainControl : MonoBehaviour
    {
#region Fields
        [Header("Spine")]
        [SerializeField]
        GameObject m_CharacterBase;

        [SerializeField]
        GameObject m_BackgroundBase,
            m_RotationBase;

        [SerializeField]
        Shader m_SpineShader;

        [SerializeField, Range(0.01f, 0.02f)]
        float m_SpineScaleMultiplier = 0.0115f;
        SkeletonAnimation sprAnimation,
            bgAnimation;
        Bone lookBone,
            patBone;

        [Header("Components")]
        [SerializeField]
        Button m_PatButton;

        [SerializeField]
        Button m_TalkButton;

        [SerializeField]
        AudioSource m_BGMAudioSource,
            m_SFXAudioSource,
            m_VoiceAudioSource;

        // Talk
        bool isTalking;
        Dictionary<string, AudioClip> voiceList = new Dictionary<string, AudioClip>();
        int voiceIndex = 1,
            secondVoiceIndex = 1,
            totalVoice = 4;

        // Look
        bool isLooking,
            lookEnding;
        float lookSpeed = 4,
            lookRange = 1f;
        Vector3 look;
        string lookA,
            lookM,
            lookEndA,
            lookEndM;

        // Pat
        bool isPatting,
            isFirstPat,
            patEnding;
        float patSpeed = 2,
            patRange = 0.5f;
        Vector3 pat;
        string patA,
            patM,
            patEndA,
            patEndM;

        [Header("UI")]
        [SerializeField]
        ScrollRect m_DebugWindow;

        [SerializeField]
        TextMeshProUGUI m_DebugText;

        [SerializeField]
        RectTransform m_BoneIndicatorPrefab;

        SettingsManager settingsManager;
        Setting settings;
#endregion

#region Initialization
        void Awake()
        {
            settingsManager = SettingsManager.Instance;
            m_DebugText.text = string.Empty;
        }

        void Start()
        {
            settings = settingsManager.settings;
            InitComponents();
            Initialize();
        }

        void InitComponents()
        {
            m_BGMAudioSource.gameObject.SetActive(settings.bgm.enable);
            m_SFXAudioSource.gameObject.SetActive(settings.sfx.enable);
            m_VoiceAudioSource.gameObject.SetActive(settings.talk != null);
            m_DebugWindow.gameObject.SetActive(settings.debug);
        }

        async void Initialize()
        {
            Debug.Log("Initializing...");

            // Init properties value
            totalVoice = settings.talk.maxIndex;
            patRange = settings.pat.range;
            lookRange = settings.lookRange;

            // Setup BGM Audio Source if enabled
            if (settings.bgm.enable)
            {
                Debug.Log("Setup & Cache BGM...");
                string bgmPath = Path.Combine(settingsManager.dataPath, settings.bgm.clip);

                // Cache the audio clip...
                m_BGMAudioSource.clip = await WebRequestHelper.GetAudioClip(bgmPath);
                m_BGMAudioSource.volume = settings.bgm.volume;
                m_BGMAudioSource.loop = true;
            }

            // Setup SFX Audio Source if enabled
            if (settings.sfx.enable)
            {
                Debug.Log("Setup & Cache SFX...");
                string sfxPath = Path.Combine(settingsManager.dataPath, settings.sfx.name);

                // Cache the audio clip...
                m_SFXAudioSource.clip = await WebRequestHelper.GetAudioClip(sfxPath);
                m_SFXAudioSource.volume = settings.sfx.volume;
                m_SFXAudioSource.loop = false;
            }

            Debug.Log("Get & Cache Character Voices...");

            string voicePath = Path.Combine(settingsManager.dataPath, "Voice");
            DirectoryInfo directoryInfo = new DirectoryInfo(voicePath);
            FileInfo[] files = directoryInfo.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Name.Contains("MemorialLobby"))
                {
                    AudioClip clip = await WebRequestHelper.GetAudioClip(files[i].FullName);
                    voiceList.Add(files[i].Name.Replace(".ogg", ""), clip);
                }
            }

            Debug.Log("Setup Character Spine...");

            // Instantiate character spine
            sprAnimation = await SpineHelper.InstantiateSpine(
                settingsManager.dataPath,
                settings.student,
                settings.imageList,
                m_CharacterBase,
                m_SpineShader,
                settings.scale,
                m_SpineScaleMultiplier
            );

            // Play idle animation continuously
            sprAnimation.AnimationState.AddAnimation(0, "Idle_01", true, 0);

            // If has spine background
            if (settings.bg.isSpine)
            {
                Debug.Log("Setup Background Spine...");

                // Instantiate background spine
                bgAnimation = await SpineHelper.InstantiateSpine(
                    settingsManager.dataPath,
                    settings.bg.name,
                    settings.bg.imageList,
                    m_BackgroundBase,
                    m_SpineShader,
                    settings.scale,
                    m_SpineScaleMultiplier
                );

                // Play idle animation continuously
                bgAnimation.AnimationState.AddAnimation(0, "Idle_01", true, 0);

                if (settings.debug)
                {
                    foreach (Spine.Animation a in bgAnimation.skeleton.Data.Animations)
                        m_DebugText.text += a.Name + "\n";
                }

                if (settings.bg.state.more)
                    bgAnimation.AnimationState.SetAnimation(2, settings.bg.state.name, true);
            }

            // Play Audio Source after all Spine initialized
            if (settings.bgm.enable)
                m_BGMAudioSource.Play();
            if (settings.sfx.enable)
                m_SFXAudioSource.Play();

            // Debug Window
            if (settings.debug)
            {
                Color grey50 = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                m_PatButton.GetComponent<Image>().color = grey50;
                m_TalkButton.GetComponent<Image>().color = grey50;

                m_DebugText.text += "<b>Events:</b>\n";
                foreach (EventData e in sprAnimation.skeleton.Data.Events)
                    m_DebugText.text += $"{e.Name}\n";

                m_DebugText.text += "<b>Bones:</b>\n";
                foreach (Bone b in sprAnimation.Skeleton.Bones)
                {
                    string tmp = b.Data.Name.ToLower();

                    if (tmp.Contains("eye") || tmp.Contains("halo") || tmp.Contains("neck"))
                        m_DebugText.text += $"{b.Data.Name}\n";
                }
            }

            sprAnimation.AnimationState.Event += HandleEvent;

            void HandleEvent(TrackEntry trackEntry, Spine.Event e)
            {
                if (settings.talk.onlyTalk)
                {
                    if (e.Data.Name == "Talk")
                    {
                        foreach (string k in voiceList.Keys)
                        {
                            if (k.EndsWith("MemorialLobby_" + (voiceIndex - 1)))
                            {
                                Debug.Log(k);
                                m_VoiceAudioSource.clip = voiceList[k];
                                m_VoiceAudioSource.Play();
                                break;
                            }
                            else if (
                                k.EndsWith(
                                    "MemorialLobby_" + (voiceIndex - 1) + "_" + secondVoiceIndex
                                )
                            )
                            {
                                Debug.Log(k);
                                m_VoiceAudioSource.clip = voiceList[k];
                                m_VoiceAudioSource.Play();
                                secondVoiceIndex++;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (string k in voiceList.Keys)
                    {
                        if (e.Data.Name.Contains(k))
                        {
                            m_VoiceAudioSource.clip = voiceList[k];
                            m_VoiceAudioSource.Play();
                            break;
                        }
                    }
                }
            }

            sprAnimation.AnimationState.Complete += delegate(TrackEntry trackEntry)
            {
                if (trackEntry.TrackIndex == 4 && trackEntry.ToString().Contains("Talk_"))
                {
                    isTalking = false;
                    Debug.Log("4 End: " + trackEntry.ToString());
                }
            };

            Vector3 eyeL = SpineHelper.BoneScreenPosition(sprAnimation, settings.bone.eyeL);
            Vector3 eyeR = SpineHelper.BoneScreenPosition(sprAnimation, settings.bone.eyeR);

            SetPatAndTalkButton(eyeL, eyeR);
            SetLook();

            // Attempt to show bone tooltip
            // foreach (Bone bone in sprAnimation.Skeleton.Bones)
            // {
            //     string boneName = bone.Data.Name;
            //     string boneNameLower = boneName.ToLower();

            //     if (boneNameLower.Contains("eye") || boneNameLower.Contains("halo") || boneNameLower.Contains("neck"))
            //     {
            //         RectTransform boneIndicator = Instantiate(m_BoneIndicatorPrefab);
            //         boneIndicator.SetParent(m_DebugWindow.transform.parent);

            //         boneIndicator.gameObject.name = boneName;
            //         boneIndicator.GetComponentInChildren<TextMeshProUGUI>().text = boneName;
            //         boneIndicator.transform.localPosition = bone.GetLocalPosition();
            //         // bone.GetWorldPosition(sprAnimation.transform);
            //     }
            // }
            m_DebugWindow.Rebuild(CanvasUpdate.LatePreRender); // Update layout
        }
        #endregion

        void Update()
        {
            Vector2 mousePosition = Input.mousePosition;
            Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
            Vector2 downPoint = m_RotationBase.transform.InverseTransformPoint(
                worldMousePosition
            );

            if (!isTalking)
            {
                // Patting...
                if (isPatting)
                {
                    // if (downPoint.x - pat.x >= patRange)
                    //     downPoint.x = pat.x + patRange;
                    // else if (downPoint.x - pat.x <= -patRange)
                    //     downPoint.x = pat.x - patRange;
                    // downPoint.x = Mathf.Clamp(downPoint.x, pat.x - patRange, pat.x + patRange);
                    // downPoint.y = pat.y;
                    // downPoint = m_RotationBase.transform.TransformPoint(downPoint);

                    // patBone.SetPositionSkeletonSpace(downPoint);

                    downPoint = m_RotationBase.transform.TransformPoint(
                        new Vector2(
                            Mathf.Clamp(downPoint.x, pat.x - patRange, pat.x + patRange),
                            pat.y
                        )
                    );

                    patBone.SetPositionSkeletonSpace(downPoint);
                }
                else if (patEnding)
                {
                    // If the absolute difference between patBone.X and pat.x,
                    // is <= to the threshold or tolerance...
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
                    float sx = (downPoint.y - look.y) / (downPoint.x - look.x);
                    float sy = (downPoint.x - look.x) / (downPoint.y - look.y);

                    if (downPoint.x - look.x >= lookRange && Math.Abs(sx) <= 1)
                    {
                        downPoint.y = look.y + lookRange * sx;
                        downPoint.x = look.x + lookRange;
                    }
                    else if (downPoint.x - look.x <= -lookRange && Math.Abs(sx) <= 1)
                    {
                        downPoint.y = look.y - lookRange * sx;
                        downPoint.x = look.x - lookRange;
                    }
                    else if (downPoint.y - look.y >= lookRange && Math.Abs(sx) > 1)
                    {
                        downPoint.y = look.y + lookRange;
                        downPoint.x = look.x + lookRange * sy;
                    }
                    else if (downPoint.y - look.y <= -lookRange && Math.Abs(sx) > 1)
                    {
                        downPoint.y = look.y - lookRange;
                        downPoint.x = look.x - lookRange * sy;
                    }

                    downPoint = m_RotationBase.transform.TransformPoint(downPoint);
                    lookBone.SetPositionSkeletonSpace(downPoint);
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
                        Vector3 tmpP = Vector3.MoveTowards(
                            lookBone.GetWorldPosition(sprAnimation.transform),
                            m_RotationBase.transform.TransformPoint(look),
                            lookSpeed * Time.deltaTime
                        );
                        lookBone.SetPositionSkeletonSpace(tmpP);
                    }
                }
            }
        }

        public void SetTalking()
        {
            if (!isTalking)
            {
                isTalking = true;
                if (voiceIndex > totalVoice)
                    voiceIndex = 1;

                secondVoiceIndex = 1;
                sprAnimation.AnimationState.AddEmptyAnimation(3, 0.2f, 0);
                sprAnimation.AnimationState.AddEmptyAnimation(4, 0.2f, 0);
                sprAnimation.AnimationState.AddAnimation(3, "Talk_0" + voiceIndex + "_A", false, 0);
                sprAnimation.AnimationState.AddAnimation(4, "Talk_0" + voiceIndex + "_M", false, 0);
                sprAnimation.AnimationState.AddEmptyAnimation(3, 0.2f, 0);
                sprAnimation.AnimationState.AddEmptyAnimation(4, 0.2f, 0);
                voiceIndex++;
            }
        }

        public void SetPatting(bool b)
        {
            if (!isTalking)
            {
                Debug.Log("isPatting: " + b);
                isPatting = b;
                if (b)
                {
                    if (!isFirstPat)
                    {
                        isFirstPat = true;
                        if (patA != null)
                        {
                            sprAnimation.AnimationState.SetAnimation(1, patA, false);
                        }
                        if (patM != null)
                        {
                            sprAnimation.AnimationState.SetAnimation(2, patM, false);
                            if (settings.pat.somethingWrong)
                            {
                                sprAnimation.AnimationState.AddEmptyAnimation(2, 0, 0);
                            }
                        }
                    }
                }
                else
                {
                    if (patEndA != null)
                    {
                        sprAnimation.AnimationState.AddAnimation(1, patEndA, false, 0);
                    }
                    if (patEndM != null)
                    {
                        sprAnimation.AnimationState.AddAnimation(2, patEndM, false, 0);
                    }
                    sprAnimation.AnimationState.AddEmptyAnimation(1, 0.2f, 0);
                    sprAnimation.AnimationState.AddEmptyAnimation(2, 0.2f, 0);

                    patEnding = true;
                    isFirstPat = false;
                }
            }
        }

        void SetPatAndTalkButton(Vector3 l, Vector3 r)
        {
            // PatButton
            float patAngle = SpineHelper.GetAngle(l, r);
            m_RotationBase.transform.localRotation = Quaternion.Euler(0, 0, patAngle);

            if (settings.rotation)
            {
                Camera.main.transform.localRotation = Quaternion.Euler(0, 0, patAngle);
                l = Camera.main.WorldToScreenPoint(
                    sprAnimation.skeleton
                        .FindBone(settings.bone.eyeL)
                        .GetWorldPosition(sprAnimation.transform)
                );
                r = Camera.main.WorldToScreenPoint(
                    sprAnimation.skeleton
                        .FindBone(settings.bone.eyeR)
                        .GetWorldPosition(sprAnimation.transform)
                );
            }
            else
            {
                m_PatButton.transform.localEulerAngles = new Vector3(0, 0, patAngle);
                m_TalkButton.transform.localEulerAngles = new Vector3(0, 0, patAngle);
            }

            Vector3 halo = Camera.main.WorldToScreenPoint(
                sprAnimation.skeleton
                    .FindBone(settings.bone.halo)
                    .GetWorldPosition(sprAnimation.transform)
            );

            Vector3 betweenPoint = SpineHelper.GetMidpoint(l, r);
            float weight = SpineHelper.GetDistance(l, r);
            float hight = SpineHelper.GetDistance(halo, betweenPoint);
            m_PatButton.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(
                weight * 3,
                hight
            );
            m_PatButton.transform.position = betweenPoint;

            // TalkButton
            Vector3 neck = Camera.main.WorldToScreenPoint(
                sprAnimation.skeleton
                    .FindBone(settings.bone.neck)
                    .GetWorldPosition(sprAnimation.transform)
            );
            m_TalkButton.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(
                weight * 3,
                hight
            );
            m_TalkButton.transform.position = neck;

            foreach (Spine.Animation i in sprAnimation.skeleton.Data.Animations)
            {
                if (i.Name.StartsWith("Pat_0"))
                {
                    if (i.Name.EndsWith("A"))
                        patA = i.Name;
                    else if (i.Name.EndsWith("M"))
                        patM = i.Name;
                }
                else if (i.Name.StartsWith("PatEnd"))
                {
                    if (i.Name.EndsWith("A"))
                        patEndA = i.Name;
                    else if (i.Name.EndsWith("M"))
                        patEndM = i.Name;
                }
            }

            patBone = sprAnimation.skeleton.FindBone("Touch_Point");
            pat = m_RotationBase.transform.InverseTransformPoint(
                patBone.GetWorldPosition(sprAnimation.transform)
            );
        }

        public void SetLooking(bool b)
        {
            if (!isTalking)
            {
                isLooking = b;
                Debug.Log("isLooking: " + b);
                if (b)
                {
                    if (lookA != null)
                    {
                        sprAnimation.AnimationState.SetAnimation(1, lookA, false);
                    }
                    if (lookM != null)
                    {
                        sprAnimation.AnimationState.SetAnimation(2, lookM, false);
                    }
                }
                else
                {
                    if (lookEndA != null)
                    {
                        sprAnimation.AnimationState.AddAnimation(1, lookEndA, false, 0);
                    }
                    if (lookEndM != null)
                    {
                        sprAnimation.AnimationState.AddAnimation(2, lookEndM, false, 0);
                    }
                    sprAnimation.AnimationState.AddEmptyAnimation(1, 0.2f, 0);
                    sprAnimation.AnimationState.AddEmptyAnimation(2, 0.2f, 0);

                    lookEnding = true;
                }
            }
        }

        void SetLook()
        {
            foreach (Spine.Animation i in sprAnimation.skeleton.Data.Animations)
            {
                if (i.Name.StartsWith("Look_0"))
                {
                    if (i.Name.EndsWith("A"))
                        lookA = i.Name;
                    else if (i.Name.EndsWith("M"))
                        lookM = i.Name;
                }
                else if (i.Name.StartsWith("LookEnd"))
                {
                    if (i.Name.EndsWith("A"))
                        lookEndA = i.Name;
                    else if (i.Name.EndsWith("M"))
                        lookEndM = i.Name;
                }
            }

            lookBone = sprAnimation.skeleton.FindBone("Touch_Eye");
            look = m_RotationBase.transform.InverseTransformPoint(
                lookBone.GetWorldPosition(sprAnimation.transform)
            );
        }
    }
}
