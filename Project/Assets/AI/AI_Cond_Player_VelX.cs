﻿using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mugen;
using XNode;
using System;

namespace XNode.Mugen
{
	[CreateNodeMenu("AI/条件/角色 VelX")]
	[Serializable]
	public class AI_Cond_Player_VelX: AI_BaseCondition
	{
		[SerializeField] public float velX;
		[SerializeField] public AI_Cond_Op op = AI_Cond_Op.Equal;

		public override string ToCondString(string luaPlayer)
		{
			var opStr = GetOpStr (op);
			string ret = string.Format ("trigger:VelX({0}){1}{2}", luaPlayer, opStr, velX.ToString());
			return ret;
		}
	}
}
