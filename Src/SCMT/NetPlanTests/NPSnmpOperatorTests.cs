﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetPlan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonUtility;
using MIBDataParser.JSONDataMgr;
using ONE_DEV_ATTRI_INFO = System.Collections.Generic.List<NetPlan.MibLeafNodeInfo>;

namespace NetPlan.Tests
{
	[TestClass()]
	public class NPSnmpOperatorTests
	{
		[TestMethod()]
		public void QueryNetBoardTest()
		{
			bool stop = false;
			CSEnbHelper.SetCurEnbAddr("172.27.245.92");
			var db = Database.GetInstance();
			db.initDatabase("172.27.245.92");
			db.resultInitData = result =>
			{
				if (result)
				{
					var boardInfoList = new List<List<MibLeafNodeInfo>>();
					var ret = NPSnmpOperator.ExecuteNetPlanCmd("GetNetBoard", ref boardInfoList);
					Assert.IsTrue(ret);
					Assert.AreEqual(boardInfoList.Count, 2);
					stop = true;
				}
			};

			while (!stop)
			{
				Thread.Sleep(1000);
			}
		}

		[TestMethod()]
		public void InitNetPlanInfoTest()
		{
			bool stop = false;
			CSEnbHelper.SetCurEnbAddr("172.27.245.92");
			var db = Database.GetInstance();
			db.initDatabase("172.27.245.92");
			db.resultInitData = result =>
			{
				if (result)
				{
					Dictionary<string, List<ONE_DEV_ATTRI_INFO>> allEnbNetPlanInfo;
					var ret = NPSnmpOperator.InitNetPlanInfo(out allEnbNetPlanInfo);
					Assert.IsTrue(ret);
					Assert.AreEqual(allEnbNetPlanInfo.Count, 1);
					stop = true;
				}
			};

			while (!stop)
			{
				Thread.Sleep(1000);
			}
		}
	}
}