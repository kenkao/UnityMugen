﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Mugen;

[CustomEditor(typeof(PlayerDisplay))]
public class PlayerDisplayEditor : Editor {
    private List<PlayerState> m_VaildStateList = null;
    private string[] m_VaildStateNameList = null;
    private PlayerDisplay m_LastDisplay = null;
    private int m_StateSelected = -1;

    private void InitPlayerDisplay()
    {
        m_VaildStateList = null;
        m_VaildStateNameList = null;
        m_StateSelected = -1;
        if (m_LastDisplay == null)
            return;
        var player = m_LastDisplay.GPlayer;
        if (player == null)
        {
            // 可能还没有加载完数据
            m_LastDisplay = null;
            return;
        }
        if (player.AirCfg != null && player.AirCfg.IsVaild)
        {
            var iter = player.AirCfg.GetStateIter();
            while (iter.MoveNext())
            {
                if (m_VaildStateList == null)
                    m_VaildStateList = new List<PlayerState>();
                m_VaildStateList.Add(iter.Current.Key);
            }
            iter.Dispose();
            if (m_VaildStateList  != null && m_VaildStateList.Count > 0)
            {
                m_VaildStateNameList = new string[m_VaildStateList.Count];
                for (int i = 0; i < m_VaildStateList.Count; ++i)
                {
                    var state = m_VaildStateList[i];
                    m_VaildStateNameList[i] = state.ToString();
                }
            }
        }
    }

    private void DrawPlayerDisplay()
    {
        if (m_LastDisplay == null || m_VaildStateList == null || m_VaildStateNameList == null)
            return;
        int newSelected = EditorGUILayout.Popup("角色动作", m_StateSelected, m_VaildStateNameList);
        if (m_StateSelected != newSelected)
        {
            m_StateSelected = newSelected;
        }
    }

	public override void OnInspectorGUI ()
    {
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            if (m_LastDisplay != this.target)
            {
                m_LastDisplay = this.target as PlayerDisplay;
                InitPlayerDisplay();
            }

            DrawPlayerDisplay();

        }

    }
}