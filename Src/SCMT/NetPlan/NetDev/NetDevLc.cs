﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogManager;
using MAP_DEVTYPE_DEVATTRI = System.Collections.Generic.Dictionary<NetPlan.EnumDevType, System.Collections.Generic.List<NetPlan.DevAttributeInfo>>;

namespace NetPlan
{
	internal class NetDevLc : NetDevBase
	{
		#region 构造函数

		internal NetDevLc(string strTargetIp, MAP_DEVTYPE_DEVATTRI mapOriginData) : base(strTargetIp, mapOriginData)
		{

		}

		#endregion

		#region 虚函数重载区

		internal override bool DistributeToEnb(DevAttributeBase dev, bool bDlAntWcb = false)
		{
			// 下发之前需要打开布配开关
			if (!NPCellOperator.SendNetPlanSwitchToEnb(true, dev.m_strOidIndex, m_strTargetIp))
			{
				Log.Error($"打开本地小区{dev.m_strOidIndex}的布配开关失败");
				return false;
			}

			// 下发本地小区信息
			var bSucceedLc = base.DistributeToEnb(dev);

			// 下发完成后关闭布配开关。无论是否下发成功都要关闭开关
			var bSucceedSwitch = NPCellOperator.SendNetPlanSwitchToEnb(false, dev.m_strOidIndex, m_strTargetIp);

			return bSucceedLc && bSucceedSwitch;
		}

		#endregion

		#region 非虚函数区

		/// <summary>
		/// 重置本地小区相关的配置
		/// </summary>
		/// <param name="dev"></param>
		/// <param name="strLcId"></param>
		/// <returns></returns>
		public static bool ResetNetLcConfig(DevAttributeBase dev, string strLcId)
		{
			if (null == dev || string.IsNullOrEmpty(strLcId))
			{
				throw new ArgumentNullException();
			}

			var mapAttributes = dev.m_mapAttributes;
			for (var j = 1; j <= MagicNum.RRU_TO_BBU_PORT_CNT; j++)
			{
				var mibName = "netSetRRUPortSubtoLocalCellId";
				if (j > 1)
				{
					mibName += $"{j}";
				}

				if (!mapAttributes.ContainsKey(mibName))
				{
					Log.Error($"在天线阵安装规划表中没有找到名为{mibName}的节点，可能MIB版本错误");
					continue;
				}

				var lcValue = dev.GetNeedUpdateValue(mibName);
				if (null != lcValue && lcValue != strLcId) continue;

				dev.SetFieldLatestValue(mibName, "-1");

				if (RecordDataType.NewAdd != dev.m_recordType)
				{
					dev.SetDevRecordType(RecordDataType.Modified);
				}
			}

			return true;
		}

		public static bool ResetNetLcConfig(DevAttributeBase dev)
		{
			var mapAttributes = dev.m_mapAttributes;
			for (var j = 1; j <= MagicNum.RRU_TO_BBU_PORT_CNT; j++)
			{
				var mibName = "netSetRRUPortSubtoLocalCellId";
				if (j > 1)
				{
					mibName += $"{j}";
				}

				if (!mapAttributes.ContainsKey(mibName))
				{
					Log.Error($"在天线阵安装规划表中没有找到名为{mibName}的节点，可能MIB版本错误");
					continue;
				}

				dev.SetDevAttributeValue(mibName, "-1");
			}

			return true;
		}

		#endregion

	}
}
