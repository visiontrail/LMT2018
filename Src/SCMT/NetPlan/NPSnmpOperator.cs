﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using CommonUtility;
using LinkPath;
using LmtbSnmp;
using LogManager;
using MIBDataParser;
using MIBDataParser.JSONDataMgr;
using DIC_DOUBLE_STR = System.Collections.Generic.Dictionary<string, string>;
using ONE_DEV_ATTRI_INFO = System.Collections.Generic.List<NetPlan.MibLeafNodeInfo>;

namespace NetPlan
{
	/// <summary>
	/// 网规snmp相关的操作
	/// </summary>
	public class NPSnmpOperator
	{
		#region 公共接口

		/// <summary>
		/// 设置NR网元布配控制开关状态
		/// </summary>
		/// <param name="bOpen">true:打开开关，false:关闭开关</param>
		/// <param name="strIndex">索引</param>
		/// <param name="targetIp">目标基站地址</param>
		/// <returns>true:设置成功,false:设置失败</returns>
		public static bool SetNetPlanSwitch(bool bOpen, string strIndex, string targetIp)
		{
			if (string.IsNullOrEmpty(strIndex) || string.IsNullOrEmpty(targetIp))
			{
				Log.Error("设置网规开关功能传入参数错误");
				return false;
			}

			var strIndexTemp = strIndex.Trim('.');      // 去掉索引字符串前后的.
			strIndexTemp = $".{strIndexTemp}";

			var name2Value = new DIC_DOUBLE_STR();
			name2Value["nrNetLocalCellCtrlConfigSwitch"] = (bOpen ? "1" : "0");

			const string cmd = "SetNRNetwokPlanControlSwitch";
			long reqId;
			var pdu = new CDTLmtbPdu();

			var ret = CDTCmdExecuteMgr.CmdSetSync(cmd, out reqId, name2Value, strIndexTemp, targetIp, ref pdu);

			return (0 == ret);
		}

		/// <summary>
		/// 传入命令名查询网规信息
		/// </summary>
		/// <param name="strCmdName">要执行的命令名</param>
		/// <param name="result">出参：查询结果，key:oid, value: real value </param>
		/// <returns>true:查询成功,false:查询失败</returns>
		public static bool QueryNetPlanInfo(string strCmdName, out DIC_DOUBLE_STR result, out List<string> indexList)
		{
			var targetIp = CSEnbHelper.GetCurEnbAddr();
			if (null == targetIp)
			{
				Log.Error($"未选中基站");
				result = null;
				indexList = null;
				return false;
			}

			if (string.IsNullOrEmpty(strCmdName))
			{
				throw new CustomException("传入命令名为null");
			}

			long reqId;
			var ret = CDTCmdExecuteMgr.GetInstance().CmdGetNextSync(strCmdName, out reqId, out indexList, out result, targetIp);

			if (0 == ret)
				return true;

			result = null;
			return false;
		}

		// 执行网规相关的命令。其实可以用到所有的命令之上
		public static bool ExecuteNetPlanCmd(string cmdName, ref List<ONE_DEV_ATTRI_INFO> sameTypeDevInfoList)
		{
			if (string.IsNullOrEmpty(cmdName))
			{
				return false;
			}

			DIC_DOUBLE_STR result;	// key:完全oid，包括前缀和索引，value:该项对应的真实值
			List<string> indexList;
			if (!QueryNetPlanInfo(cmdName, out result, out indexList))
			{
				return false;
			}

			// TODO 下面的操作有重复计算，需要处理
			var targetIp = CSEnbHelper.GetCurEnbAddr();
			var cmdInfo = SnmpToDatabase.GetCmdInfoByCmdName(cmdName, targetIp);
			if (null == cmdInfo)
			{
				return false;
			}

			var parentName = cmdInfo.m_tableName;
			var tbl = Database.GetInstance().GetMibDataByTableName(parentName, targetIp);
			if (null == tbl)
			{
				return false;
			}

			var indexGrade = tbl.indexNum;		// 索引级数
			var childList = tbl.childList;

			// 存储同一类设备的所有信息
			if (null == sameTypeDevInfoList)
			{
				sameTypeDevInfoList = new List<ONE_DEV_ATTRI_INFO>();
			}

			// 区分标量和表量
			if (indexGrade == 0)	// 索引级数为0，认为是标量表
			{
				// 存储一个设备的所有属性信息
				var oneDevAttributes = GetScalarMibInfo(childList, result);
				if (oneDevAttributes.Count > 0)
				{
					sameTypeDevInfoList.Add(oneDevAttributes);
				}

				return true;
			}

			// 表量表
			// 此处在indexList中循环，一个indexList元素就是一行数据，也就是一个完整的属性。
			foreach (var index in indexList)
			{
				var oneDevAttributes = GetTableMibInfo(index, childList, result);

				// 一个设备的所有信息查询完成，保存数据
				if (oneDevAttributes.Count > 0)
				{
					sameTypeDevInfoList.Add(oneDevAttributes);
				}
			}

			return true;
		}

		/// <summary>
		/// 初始化网规信息
		/// 调用时机：连接基站后，第一次进入网规页面
		/// </summary>
		/// <returns></returns>
		public static bool InitNetPlanInfo(out Dictionary<string, List<ONE_DEV_ATTRI_INFO>> allEnbNetPlanInfo)
		{
			var mibEntryList =  NPECmdHelper.GetInstance().GetAllMibEntryAndCmds("EMB6116");
			if (null == mibEntryList)
			{
				Log.Error($"查询所有的MIB入口及对应命令失败");
				allEnbNetPlanInfo = null;
				return false;
			}

			var enbIp = CSEnbHelper.GetCurEnbAddr();

			allEnbNetPlanInfo = new Dictionary<string, List<ONE_DEV_ATTRI_INFO>>();

			// 调用所有的Get函数，查询所有的信息
			foreach (var entry in mibEntryList)
			{
				var getCmdList = entry.Get;
				if (getCmdList.Count == 0)
				{
					continue;
				}

				var temp = new List<ONE_DEV_ATTRI_INFO>();

				// 同一个mib入口下可能有多个get命令，这些命令查询的结果要进行合并处理，因为同属于一张表，只不过每次查询了不同的部分
				foreach (var cmd in getCmdList)
				{
					var oneCmdMibInfo = new List<ONE_DEV_ATTRI_INFO>();
					if (!ExecuteNetPlanCmd(cmd, ref oneCmdMibInfo))
					{
						Log.Error($"查询表{entry.MibEntry}信息失败");
						return false;
					}
					MergeSameEntryData(ref temp, oneCmdMibInfo);
				}

				// 合并完成后，直接保存数据
				if (temp.Count > 0)
				{
					allEnbNetPlanInfo.Add(entry.MibEntry, temp);
				}
			}

			return true;
		}
		#endregion

		#region 私有接口

		/// <summary>
		/// 根据Oid从mapOidAndValue中获取真实值后创建一个MibLeafNodeInfo对象
		/// </summary>
		/// <param name="strOid"></param>
		/// <param name="mapOidAndValue"></param>
		private static MibLeafNodeInfo GetMibLeafNodeWithRealValue(string strOid, DIC_DOUBLE_STR mapOidAndValue, MibLeaf mibLeaf)
		{
			if (string.IsNullOrEmpty(strOid))
			{
				return null;
			}

			// 判断是否存在于result中
			if (!mapOidAndValue.ContainsKey(strOid))
			{
				return null;
			}

			// 该表项有值，就处理
			var info = new MibLeafNodeInfo
			{
				m_strRealValue = mapOidAndValue[strOid],
				mibAttri = mibLeaf
			};

			return info;
		}

		/// <summary>
		/// 获取标量MIB信息
		/// </summary>
		/// <returns></returns>
		private static ONE_DEV_ATTRI_INFO GetScalarMibInfo(List<MibLeaf> childList, DIC_DOUBLE_STR result)
		{
			var devAttributes = new ONE_DEV_ATTRI_INFO();

			if (null == childList || null == result)
			{
				return devAttributes;
			}

			var mibPrefix = SnmpToDatabase.GetMibPrefix().Trim('.');

			// 对于标量的处理，在oid后追加.0
			foreach (var childLeaf in childList)
			{
				if (0 == childLeaf.isMib)
				{
					// 过滤掉假MIB
					continue;
				}

				var childFullOid = $"{mibPrefix}.{childLeaf.childOid.Trim('.')}.0";
				var info = GetMibLeafNodeWithRealValue(childFullOid, result, childLeaf);
				if (null != info)
				{
					info.m_strIndex = ".0";
					devAttributes.Add(info);
				}
			}

			return devAttributes;
		}

		/// <summary>
		/// 获取标量表代表的一行信息
		/// </summary>
		/// <param name="strIndex"></param>
		/// <param name="childList"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		private static ONE_DEV_ATTRI_INFO GetTableMibInfo(string strIndex, List<MibLeaf> childList, DIC_DOUBLE_STR result)
		{
			var devAttributes = new ONE_DEV_ATTRI_INFO();

			if (string.IsNullOrEmpty(strIndex) || null == childList || null == result)
			{
				return devAttributes;
			}

			var mibPrefix = SnmpToDatabase.GetMibPrefix().Trim('.');
			foreach (var childLeaf in childList)
			{
				if (0 == childLeaf.isMib)
				{
					// 跳过假mib
					continue;
				}

				// 判断是否是索引，如果是索引，就不会出现在result中
				if (childLeaf.IsIndex.Equals("True", StringComparison.OrdinalIgnoreCase))
				{
					// 根据子节点的顺序截取索引中的一段作为该项的值
					var realValue = MibStringHelper.GetRealValueFromIndex(strIndex, childLeaf.childNo);
					if (null == realValue)
					{
						continue;
					}

					var info = new MibLeafNodeInfo
					{
						m_strRealValue = realValue,
						m_strIndex = strIndex,
						mibAttri = childLeaf,
						m_bReadOnly = true          // 索引，只读
					};

					devAttributes.Add(info);
				}
				else
				{
					var childFullOid = $"{mibPrefix}.{childLeaf.childOid.Trim('.')}.{strIndex.Trim('.')}";
					var info = GetMibLeafNodeWithRealValue(childFullOid, result, childLeaf);
					if (null != info)
					{
						info.m_strIndex = strIndex;
						devAttributes.Add(info);
					}
				}
			}
			// TODO 注意：此处没有考虑result中是否会有剩余数据

			return devAttributes;
		}

		/// <summary>
		/// 合并同一张表下面执行不同命令时得到的结果。
		/// e.g. netRRUEntry表有3个Get命令，在界面上呈现rru的信息时，需要所有的属性
		/// 需要执行3次get命令，但是这3个get命令属于同一张表，就需要把所有的命令返回的结果合并到一起，最终得到完整的信息
		/// </summary>
		/// <param name="existData">上一个命令查询的信息</param>
		/// <param name="newData">新的命令查询得到的结果</param>
		/// <param name="indexGrade">索引级数</param>
		private static void MergeSameEntryData(ref List<ONE_DEV_ATTRI_INFO> existData, List<ONE_DEV_ATTRI_INFO> newData)
		{
			if (null == existData || null == newData)
			{
				return;
			}

			// 最开始existData中没有数据，直接把newData中的数据存入
			if (existData.Count == 0)
			{
				existData.AddRange(newData);
				return;
			}

			// 归并
			var mapExist = new Dictionary<string, ONE_DEV_ATTRI_INFO>();
			foreach (var existDev in existData)
			{
				// 根据索引级数得到索引字符串
				if (existDev.Count == 0)
				{
					throw new CustomException("为什么还有属性数量为0的元素");
				}

				var index = existDev[0].m_strIndex;
				mapExist.Add(index, existDev);
			}

			foreach (var nd in newData)
			{
				var newIndex = nd[0].m_strIndex;
				if (mapExist.ContainsKey(newIndex))
				{
					var tl = mapExist[newIndex];
					tl.AddRange(nd);
					mapExist[newIndex] = tl.Distinct().ToList();	// 去重，然后保存新的列表
				}
				else
				{
					mapExist[newIndex] = nd;	// 已有的结构中不包含已有的索引，就直接保存数据
				}
			}

			existData = mapExist.Values.ToList();
		}
		#endregion
	}
}
