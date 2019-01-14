﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Utils;

namespace Mugen
{
	public class ImageFrame: DisposeObject
	{
		public ImageFrame(ImageLibrary parentLib, Texture2D tex, float offsetX, 
			float offsetY, string name, Texture2D localPalletTex = null)
		{
			mParentLib = parentLib;
			mLocalPalletTex = localPalletTex;
			SetTexture2D(tex, offsetX, offsetY, name);
		}

		public Sprite Data
		{
			get;
			protected set;
		}

		private void SetTexture2D(Texture2D tex,  float offsetX, float offsetY, string name)
		{
			if (Data != null)
			{
				if (Data.texture == tex)
					return;

				DestroyTexture();
				AppConfig.GetInstance().Loader.DestroyObject(Data);
				Data = null;
			}
			if (tex == null)
				return;

			Rect r = new Rect(0, 0, tex.width, tex.height);
			Vector2 offset = new Vector2(offsetX, offsetY);
			Data = Sprite.Create(tex, r, offset);  
			Data.name = name;
		}

		protected void DestroyTexture()
		{
			if (Data != null)
			{
				Texture2D tex = Data.texture;
				//	Data.texture = null;
				if (tex != null)
				{
					AppConfig.GetInstance().Loader.DestroyObject(tex);
				}
			}
		}

		protected override void OnFree(bool isManual)
		{
			DestroyTexture();
			AppConfig.GetInstance().Loader.DestroyObject(Data);
			Data = null;
		}

		public Texture2D LocalPalletTex
		{
			get
			{
				return mLocalPalletTex;
			}
		}

		public ImageLibrary ParentLib
		{
			get
			{
				return mParentLib;
			}
		}

		private ImageLibrary mParentLib = null;
		private Texture2D mLocalPalletTex = null;
	}

	public struct ImageAnimateNode
	{
		public PlayerState Group;
		public int frameIndex;
		public float AniTick;
		public ActionFlip flipTag; 

	}

	public class ImageLibrary: DisposeObject {
		public ImageLibrary(bool is32BitPallet = true)
		{
			mIs32BitPallet = is32BitPallet;
		}

		public bool Is32BitPallet
		{
			get
			{
				return mIs32BitPallet;
			}
		}

		public List<ImageFrame> GetImageFrameList(PlayerState state)
		{
			List<ImageFrame> ret;
			if (!mGroupImageMap.TryGetValue(state, out ret))
				return null;
			return ret;
		}

		public ImageFrame GetImageFrame(PlayerState state, int index)
		{
			List<ImageFrame> frameList = GetImageFrameList(state);
			if (frameList == null)
				return null;
			if (index < 0 || index >= frameList.Count)
				return null;
			return frameList[index];
		}

		public List<ImageAnimateNode> GetAnimationNodeList(PlayerState state)
		{
			List<ImageAnimateNode> ret;
			if (!mStateAniMap.TryGetValue(state, out ret))
				return null;
			return ret;
		}

		// 填充那些如果不存在則用orgState的第一幀填充
		private void FillNoAction(PlayerState targetState, PlayerState orgState, int step)
		{
			bool isFound = false;
			for (int i = 0; i <= 2; ++i)
			{
				PlayerState state = (PlayerState)((int)targetState + i * step);
				if (mStateAniMap.ContainsKey(state))
				{
					isFound = true;
					break;
				}
			}

			if (isFound)
				return;

			for (int i = 0; i <= 2; ++i)
			{
				PlayerState state = (PlayerState)((int)orgState + i * step);
				List<ImageAnimateNode> list;
				if (mStateAniMap.TryGetValue(state, out list) && list != null && list.Count > 0)
				{
					List<ImageAnimateNode> newList = new List<ImageAnimateNode>();
					newList.Add(list[0]);
					PlayerState newState = (PlayerState)((int)targetState + i * step);
					mStateAniMap.Add(newState, newList);
					break;
				}
			}
		}

		private bool LoadAir(string charName, AirConfig airCfg)
		{
			mStateAniMap.Clear();
			if (string.IsNullOrEmpty(charName) || airCfg == null || !airCfg.IsVaild)
				return false;
			
			//for (PlayerState action = 0; action < PlayerState.psPlayerStateCount; ++action)
            foreach (PlayerState action in PlayerStateEnumValues.GetValues())
            {
                if (action == PlayerState.psPlayerStateCount)
                    continue;

				BeginAction beginAction = airCfg.GetBeginAction(action);
				if (beginAction == null || beginAction.ActionFrameListCount <= 0)
					continue;

				List<ImageFrame> frameList = this.GetImageFrameList(action);
				if (frameList == null)
					continue;

				List<ImageAnimateNode> aniNodeList;
				if (!mStateAniMap.TryGetValue(action, out aniNodeList))
				{
					aniNodeList = new List<ImageAnimateNode>();
					mStateAniMap.Add(action, aniNodeList);
				}

				ActionFlip lastFlip = ActionFlip.afNone;
				for (int frame = 0; frame < beginAction.ActionFrameListCount; ++frame)
				{
					ActionFrame actFrame;
					if (beginAction.GetFrame(frame, out actFrame))
					{
						if (actFrame.Index >= 0 && actFrame.Index < frameList.Count)
						{
							ImageAnimateNode aniNode = new ImageAnimateNode();
							aniNode.AniTick = actFrame.Tick;
							//aniNode.flipTag = actFrame.Flip;
							aniNode.flipTag = lastFlip;
							lastFlip = actFrame.Flip;
							aniNode.frameIndex = actFrame.Index;
							aniNode.Group = action;

							aniNodeList.Add(aniNode);
						}
					}
				}
			}

			return true;
		}

		public bool LoadChar(string charName, AirConfig airCfg = null, string customSpriteName = "")
		{
			ClearAll ();
			if (string.IsNullOrEmpty (charName))
				return false;

			SffFile sf = new SffFile();
			if (!sf.LoadChar (charName, customSpriteName, false)) {
				ClearAll ();
				return false;
			}

			//for (PlayerState group = 0; group < PlayerState.psPlayerStateCount; ++group)
            foreach (PlayerState group in PlayerStateEnumValues.GetValues())
            {
                if (group == PlayerState.psPlayerStateCount)
                    continue;

				int image = 0;
				SFFSUBHEADER h;
				if (!sf.GetSubHeader((int)group, image, out h))
					continue;
				KeyValuePair<PCXHEADER, PCXDATA> d;
				if (!sf.GetPcxData((uint)group, (uint)image, out d))
					continue;
				float offX = ((float)(d.Key.x + h.x))/d.Key.widht ;//+ 1.0f;
				float offY = -((float)(d.Key.y + h.y))/d.Key.height + 1.0f;

				Texture2D tex = sf.GetIndexTexture((uint)group, (uint)image);
				while (tex != null)
				{
					ImageFrame frame = new ImageFrame(this, tex, offX, offY, charName, 
						d.Value.GetPalletTexture(mIs32BitPallet));

					AddImageFrame(group, frame);

					++image;
					if (!sf.GetSubHeader((int)group, image, out h))
						break;
					if (!sf.GetPcxData((uint)group, (uint)image, out d))
						break;
					offX = ((float)(d.Key.x + h.x))/d.Key.widht ;//+ 1.0f;
					offY = -((float)(d.Key.y + h.y))/d.Key.height + 1.0f;
					tex = sf.GetIndexTexture((uint)group, (uint)image);
				}
			}

			if (airCfg != null && !LoadAir (charName, airCfg)) {
				ClearAll ();
				return false;
			}

			return true;
		}

		protected override void OnFree(bool isManual)
		{
			ClearAll();
		}

		public void ClearAll()
		{
			mStateAniMap.Clear();
			DestroyPallets();
			DestroyGroupImages();
		}

		protected void DestroyPallets()
		{
			var iter = mPalletMap.GetEnumerator();
			while (iter.MoveNext())
			{
				AppConfig.GetInstance().Loader.DestroyObject(iter.Current.Value);
			}
			iter.Dispose();
			mPalletMap.Clear();
		}

		public Dictionary<string, Texture2D>.Enumerator GetPalletMapIter()
		{
			return mPalletMap.GetEnumerator();
		}

		protected void DestroyGroupImages()
		{
			var iter = mGroupImageMap.GetEnumerator();
			while (iter.MoveNext())
			{
				if (iter.Current.Value != null)
				{
					for (int i = 0; i < iter.Current.Value.Count; ++i)
					{
						ImageFrame frame = iter.Current.Value[i];
						if (frame != null)
							frame.Dispose();
					}
					iter.Current.Value.Clear();
				}
			}
			iter.Dispose();
			mGroupImageMap.Clear();
		}

		protected bool AddImageFrame(PlayerState state, ImageFrame frame)
		{
			if (frame == null)
				return false;
			List<ImageFrame> frameList;
			if (!mGroupImageMap.TryGetValue(state, out frameList))
			{
				frameList = new List<ImageFrame>();
				mGroupImageMap.Add(state, frameList);
			}

			frameList.Add(frame);
			return true;
		}

		public string GeneratorPalletFileName(string playerName, string palletName)
		{
			palletName = System.IO.Path.GetFileNameWithoutExtension(palletName);
			string fileName = string.Format("{0}@{1}/{2}.act.bytes", AppConfig.GetInstance().PlayerRootDir, playerName, palletName);
			return fileName;
		}

		public bool AddPalletTexture(string playerName, string palletName)
		{
			string fileName = GeneratorPalletFileName(playerName, palletName);
			return AddPalletTexture(palletName, fileName, mIs32BitPallet);
		}

		private bool AddPalletTexture(string palletName, string fileName, bool is32Bit)
		{
			if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(palletName))
				return false;
			if (mPalletMap.ContainsKey(palletName))
				return true;
			byte[] source = AppConfig.GetInstance().Loader.LoadBytes(fileName);
			Texture2D tex = SffFile.GeneratorActTexture(source, is32Bit);
			if (tex == null)
				return false;
			mPalletMap.Add(palletName, tex);
			return true;
		}

		public bool AddPalletTexture(string playerName, string palletName, bool is32Bit, out string key)
		{
			key = string.Empty;
			if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(palletName))
				return false;
			string fileName = GeneratorPalletFileName(playerName, palletName);
			bool ret = AddPalletTexture(palletName, fileName, is32Bit);
			if (ret)
				key = palletName;
			return ret;
		}

		public Dictionary<PlayerState, List<ImageFrame>>.Enumerator GetGroupMapIter()
		{
			return mGroupImageMap.GetEnumerator();
		}

		public Dictionary<PlayerState, List<ImageAnimateNode>>.Enumerator GetAniMapIter()
		{
			return mStateAniMap.GetEnumerator();
		}

		// fileName, palletTexture
		private Dictionary<string, Texture2D> mPalletMap = new Dictionary<string, Texture2D>();
		// groupNumber, ImageFrame List
		private Dictionary<PlayerState, List<ImageFrame>> mGroupImageMap = new Dictionary<PlayerState, List<ImageFrame>>();
		private bool mIs32BitPallet = true;
		private Dictionary<PlayerState, List<ImageAnimateNode>> mStateAniMap = new Dictionary<PlayerState, List<ImageAnimateNode>>();
	}
}