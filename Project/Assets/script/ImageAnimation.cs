﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mugen;

// 动画驱动
[RequireComponent(typeof(Animation))]
public class ImageAnimation : MonoBehaviour {

    // 播放角色动画
    public bool PlayerPlayerAni(string playerName, PlayerState state, bool isLoop = true)
    {
        if (m_State == state)
            return true;

        ResetAnimation();
        if (string.IsNullOrEmpty(playerName))
            return false;

        PlayerDisplay displayer = GetComponent<PlayerDisplay>();
        if (displayer == null)
            return false;
        var loaderPlayer = displayer.LoaderPlayer;
        if (loaderPlayer == null)
            return false;

        var imgRes = loaderPlayer.ImageRes;
        if (imgRes == null)
            return false;
        var imgLib = imgRes.ImgLib;
        if (imgLib == null)
            return false;
        m_FrameList = imgLib.GetAnimationNodeList(state);
        bool ret = DoInitAnimation();
        if (ret)
        {
            m_IsLoop = isLoop;
            m_State = state;
        }
        return ret;
    }

    private bool DoInitAnimation()
    {
        if (m_FrameList == null || m_FrameList.Count <= 0)
        {
            ResetAnimation();
            return false;
        }

        InitAnimationClip();
        m_CurFrame = 0;
        DoChangeFrame();

        return true;
    }

    public ImageAnimateNode CurAniNode
    {
        get
        {
            ImageAnimateNode ret = new ImageAnimateNode();
            ret.frameIndex = -1;
            if (m_FrameList == null)
                return ret;
            if (m_CurFrame < 0 || m_CurFrame >= m_FrameList.Count)
                return ret;
            return m_FrameList[m_CurFrame];
        }
    }

    public void RefreshCurrent()
    {
        DoChangeFrame();
    }

    void DoEndFrame()
    {
        string evtName = "OnImageAnimationEndFrame";
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            CacheGameObject.SendMessage(evtName, this, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
        }
#else
		CacheGameObject.SendMessage(evtName, this, SendMessageOptions.DontRequireReceiver);
#endif
    }

    void DoChangeFrame()
    {
        string evtName = "OnImageAnimationFrame";
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            CacheGameObject.SendMessage(evtName, this, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
        }
#else
		CacheGameObject.SendMessage(evtName, this, SendMessageOptions.DontRequireReceiver);
#endif
    }

    private void InitAnimationClip()
    {
        var clip = this.AniClip;
        clip.frameRate = 30;
#if UNITY_EDITOR
#else
		clip.events = null;
#endif
        Animation ctl = this.CacheAnimation;
        bool isFound = false;
        var iter = ctl.GetEnumerator();
        while (iter.MoveNext())
        {
            AnimationClip c = iter.Current as AnimationClip;
            if (c != null)
            {
                if (c == clip)
                {
                    isFound = true;
                    break;
                }
            }
        }

        ctl.clip = null;
        if (!isFound)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                ctl.AddClip(clip, clip.name);
            }
            else
            {
                AnimationClip[] clips = new AnimationClip[1];
                clips[0] = clip;
                UnityEditor.AnimationUtility.SetAnimationClips(ctl, clips);
            }
#else
			ctl.AddClip(clip, clip.name);
#endif
        }

        ctl.clip = clip;

        List<AnimationEvent> evtList = new List<AnimationEvent>();

        string nextFrameStr = "NextFrame";
        string endFrameStr = "EndFrame";
        string firstFrameStr = "StartFrame";
        if (m_FrameList.Count >= 2)
        {
            float sumTime = 0;

            AnimationEvent evt = new AnimationEvent();
            evt.functionName = firstFrameStr;
            evt.messageOptions = SendMessageOptions.DontRequireReceiver;
            evt.time = 0;
            evtList.Add(evt);
            for (int i = 0; i < m_FrameList.Count; ++i)
            {
                ImageAnimateNode frame = m_FrameList[i];
                float evtTime = frame.AniTick * _cImageAnimationScale;
                sumTime += evtTime;
                AnimationEvent aniEvt = new AnimationEvent();
                if (i == m_FrameList.Count - 1)
                    aniEvt.functionName = endFrameStr;
                else
                    aniEvt.functionName = nextFrameStr;
                aniEvt.messageOptions = SendMessageOptions.DontRequireReceiver;
                aniEvt.time = sumTime;
                evtList.Add(aniEvt);
            }
        }
        else
        {
            // 直接針添加StartFrame EndFrame
            AnimationEvent evt = new AnimationEvent();
            evt.functionName = firstFrameStr;
            evt.messageOptions = SendMessageOptions.DontRequireReceiver;
            evt.time = 0;
            evtList.Add(evt);

            evt.functionName = endFrameStr;
            evt.messageOptions = SendMessageOptions.DontRequireReceiver;
            //evt.time = 1.0f/clip.frameRate;
            evt.time = Time.fixedDeltaTime;
            //	evt.time = _cLimitFrameDeltaTime;
            evtList.Add(evt);
        }
#if UNITY_EDITOR
        if (Application.isPlaying)
            clip.events = evtList.ToArray();
        else
            UnityEditor.AnimationUtility.SetAnimationEvents(clip, evtList.ToArray());
#else
		clip.events = evtList.ToArray();
#endif

    }

    public bool IsLoop
    {
        get
        {
            return m_IsLoop;
        }
    }

    public bool UpdateFrame(int frameIndex)
    {
        if (m_FrameList == null || m_FrameList.Count <= 0)
            return false;
        if (frameIndex < 0)
        {
            if (IsLoop)
                frameIndex = m_FrameList.Count - 1;
            else
                frameIndex = 0;
        }
        else if (frameIndex >= m_FrameList.Count)
        {
            if (IsLoop)
                frameIndex = 0;
            else
                frameIndex = m_FrameList.Count - 1;
        }
        int oldFrame = m_CurFrame;
        m_CurFrame = frameIndex;
        if (oldFrame != m_CurFrame)
            DoChangeFrame();
        return true;
    }

    public bool NextFrame()
    {
        return UpdateFrame(CurFrame + 1);
    }

    public void Stop()
    {
        if (m_FrameList == null || m_FrameList.Count <= 0)
            return;
        CacheAnimation.Stop();
    }

    public bool HasAniData
    {
        get
        {
            return (m_FrameList != null) && (m_FrameList.Count > 0);
        }
    }

    public bool IsPlaying
    {
        get
        {
            if (m_FrameList == null || m_FrameList.Count <= 0)
                return false;
            return CacheAnimation.isPlaying;
        }
    }

    public Animation CacheAnimation
    {
        get
        {
            if (m_Animation == null)
                m_Animation = GetComponent<Animation>();
            return m_Animation;
        }
    }

    public int CurFrame
    {
        get
        {
            return m_CurFrame;
        }
    }

    protected GameObject CacheGameObject
    {
        get
        {
            if (m_GameObj == null)
                m_GameObj = this.gameObject;
            return m_GameObj;
        }
    }

    protected AnimationClip AniClip
    {
        get
        {
            if (m_Clip == null)
            {
                m_Clip = new AnimationClip();
                m_Clip.legacy = true;
                m_Clip.name = _cPlayAnimationName;
            }
            return m_Clip;
        }
    }

    public void Clear()
    {
        if (m_Clip != null)
        {
            if (m_Animation != null)
            {
#if UNITY_EDITOR
                UnityEditor.AnimationUtility.SetAnimationClips(m_Animation, null);
#else
				bool isFound = false;
				var iter = m_Animation.GetEnumerator();
				while (iter.MoveNext())
				{
					AnimationClip c = iter.Current as AnimationClip;
					if (c != null)
					{
						if (c == mAniClip)
						{
							isFound = true;
							break;
						}
					}
				}

				if (isFound)
					m_Animation.RemoveClip(mAniClip);
#endif
                m_Animation.clip = null;
            }

            AppConfig.GetInstance().Loader.DestroyObject(m_Clip);
            m_Clip = null;
        }

        m_State = PlayerState.psNone;
        m_FrameList = null;
        m_IsLoop = false;
    }

    void OnDestroy()
    {
        Clear();
    }

    private void ResetAnimation()
    {
        m_CurFrame = -1;
    }

    public PlayerState State
    {
        get
        {
            return m_State;
        }
    }

    // 当前帧
    private static float _cImageAnimationScale = 0.03f;
    private int m_CurFrame = -1;
    private Animation m_Animation = null;
    private AnimationClip m_Clip = null;
    private GameObject m_GameObj = null;
    private PlayerState m_State = PlayerState.psNone;
    private List<ImageAnimateNode> m_FrameList = null;
    private bool m_IsLoop = false;
    // 动画文件名
    private static string _cPlayAnimationName = "Mugen Player";
}