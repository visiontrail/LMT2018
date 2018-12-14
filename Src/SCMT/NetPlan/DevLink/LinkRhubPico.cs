﻿using System;
using System.Collections.Generic;
using LogManager;

namespace NetPlan.DevLink
{
	public sealed class LinkRhubPico : NetPlanLinkBase
	{
		public LinkRhubPico() : base()
		{
			m_ethRecordType = EnumDevType.rhub_prru;
		}

		#region 虚函数区

		public override bool AddLink(WholeLink wholeLink, ref Dictionary<EnumDevType, List<DevAttributeInfo>> mapMibInfo)
		{
			mapOriginData = mapMibInfo;

			if (!CheckLinkIsValid(wholeLink, mapMibInfo, RecordNotExistInAdd))
			{
				MibInfoMgr.ErrorTip($"添加连接失败，原因：{m_strLatestError}");
				return false;
			}

			var picoClone = m_picoDev.DeepClone();

			if (RecordDataType.NewAdd != picoClone.m_recordType)
			{
				picoClone.SetDevRecordType(RecordDataType.Modified);
			}

			// 设置pico中连接rhub的信息
			var hubNo = int.Parse(m_rhubDev.m_strOidIndex.Trim('.'));
			if (!SetRhubInfoInPico(picoClone, m_nRhubEthPort, hubNo))
			{
				MibInfoMgr.ErrorTip($"添加连接失败，原因：{m_strLatestError}");
				return false;
			}

			// 判断rhub是否已经连接到板卡，如果已经连接到板卡，就设置pico中板卡的属性信息
			if (HasConnectedToBoard(m_rhubDev))
			{
				var bbi = GetBoardInfoFromRhub(RecordNotExistInAdd);
				if (null == bbi)
				{
					MibInfoMgr.ErrorTip($"添加连接失败，原因：{m_strLatestError}");
					return false;
				}

				if (!SetBoardBaseInfoInRru(bbi, picoClone, m_nPicoPort))
				{
					MibInfoMgr.ErrorTip($"添加连接失败，原因：{m_strLatestError}");
					return false;
				}

				if (!SetIrPortInfoInPico(m_rhubDev, picoClone, m_nPicoPort))
				{
					Log.Error("设置pico的接入板光口号和接入级数失败");
					return false;
				}

				// m_strEthRecordIndex只有在rhub已经连接过板卡后才会生成
				if (null == m_strEthRecordIndex)
				{
					MibInfoMgr.ErrorTip("添加连接失败，原因：以太网口规划表记录索引为null");
					return false;
				}

				var newRecord = new DevAttributeInfo(EnumDevType.rhub_prru, m_strEthRecordIndex);
				AddDevToMap(mapMibInfo, EnumDevType.rhub_prru, newRecord);

				Log.Debug($"添加类型为：{m_ethRecordType.ToString()}，索引为：{newRecord.m_strOidIndex}的记录成功");
			}

			mapMibInfo[EnumDevType.rru].Remove(m_picoDev);
			mapMibInfo[EnumDevType.rru].Add(picoClone);

			Log.Debug($"添加连接成功，连接详细信息：{wholeLink}");

			MibInfoMgr.InfoTip("添加连接成功");

			return true;
		}

		public override bool DelLink(WholeLink wholeLink, ref Dictionary<EnumDevType, List<DevAttributeInfo>> mapMibInfo)
		{
			mapOriginData = mapMibInfo;

			if (!CheckLinkIsValid(wholeLink, mapMibInfo, RecordExistInDel))
			{
				MibInfoMgr.ErrorTip($"删除连接失败，原因：{m_strLatestError}");
				return false;
			}

			var picoClone = m_picoDev.DeepClone();
			var bbi = new BoardBaseInfo();
			if (!SetBoardBaseInfoInRru(bbi, picoClone, m_nPicoPort))
			{
				MibInfoMgr.ErrorTip($"删除连接失败，原因：{m_strLatestError}");
				return false;
			}

			if (!SetRhubInfoInPico(picoClone, -1, -1))
			{
				MibInfoMgr.ErrorTip($"删除连接失败，原因：{m_strLatestError}");
				return false;
			}

			if (HasConnectedToBoard(m_rhubDev))
			{
				bbi = GetBoardInfoFromRhub(RecordExistInDel);
				if (null == bbi)
				{
					MibInfoMgr.ErrorTip($"删除连接失败，原因：{m_strLatestError}");
					return false;
				}

				var record = GetDevAttributeInfo(m_strEthRecordIndex, EnumDevType.rhub_prru);
				MibInfoMgr.DelDevFromMap(mapMibInfo, EnumDevType.rhub_prru, record);
				Log.Debug($"删除类型为：{m_ethRecordType.ToString()}，索引为：{record.m_strOidIndex}的记录成功");
			}

			if (RecordDataType.NewAdd != picoClone.m_recordType)
			{
				picoClone.SetDevRecordType(RecordDataType.Modified);
			}

			mapMibInfo[EnumDevType.rru].Remove(m_picoDev);
			mapMibInfo[EnumDevType.rru].Add(picoClone);

			Log.Debug($"删除连接成功，连接详细信息：{wholeLink}");

			MibInfoMgr.InfoTip("删除连接成功");

			return true;
		}

		public override bool CheckLinkIsValid(WholeLink wholeLink, Dictionary<EnumDevType, List<DevAttributeInfo>> mapMibInfo, IsRecordExist checkExist)
		{
			var rhubIndex = wholeLink.GetDevIndex(EnumDevType.rhub);
			if (null == rhubIndex)
			{
				m_strLatestError = "获取rhub设备的索引失败";
				return false;
			}

			m_nRhubEthPort = wholeLink.GetDevIrPort(EnumDevType.rhub, EnumPortType.rhub_to_pico);
			if (-1 == m_nRhubEthPort)
			{
				m_strLatestError = "获取rhub设备连接pico的端口失败";
				return false;
			}

			m_rhubDev = GetDevAttributeInfo(rhubIndex, EnumDevType.rhub);
			if (null == m_rhubDev)
			{
				m_strLatestError = $"根据rhub设备索引{rhubIndex}获取设备属性信息失败";
				return false;
			}

			var picoIndex = wholeLink.GetDevIndex(EnumDevType.rru);
			if (null == picoIndex)
			{
				m_strLatestError = "获取pico设备的索引失败";
				return false;
			}

			m_nPicoPort = wholeLink.GetDevIrPort(EnumDevType.rru, EnumPortType.pico_to_rhub);
			if (-1 == m_nPicoPort)
			{
				m_strLatestError = "获取pico设备连接rhub的端口失败";
				return false;
			}

			m_picoDev = GetDevAttributeInfo(picoIndex, EnumDevType.rru);
			if (null == m_picoDev)
			{
				m_strLatestError = $"根据pico索引{picoIndex}获取设备属性信息失败";
				return false;
			}

			return true;
		}

		public override DevAttributeInfo GetRecord(WholeLink wholeLink, Dictionary<EnumDevType, List<DevAttributeInfo>> mapMibInfo)
		{
			mapOriginData = mapMibInfo;

			if (!CheckLinkIsValid(wholeLink, mapMibInfo, RecordExistInDel))
			{
				return null;
			}

			if (HasConnectedToBoard(m_rhubDev))
			{
				var bbi = GetBoardInfoFromRhub(RecordExistInDel);
				if (null == bbi)
				{
					return null;
				}

				// m_strEthRecordIndex只有在rhub已经连接过板卡后才会生成
				if (null == m_strEthRecordIndex)
				{
					Log.Error("未能解析出eth record index");
					return null;
				}

				return GetDevAttributeInfo(m_strEthRecordIndex, m_ethRecordType);
			}

			return null;
		}

		private BoardBaseInfo GetBoardInfoFromRhub(IsRecordExist checkExist)
		{
			var boardSlot = NetDevRhub.GetRhubLinkToBoardSlotNo(m_rhubDev);
			if (null == boardSlot)
			{
				m_strLatestError = $"获取索引为{m_rhubDev.m_strOidIndex}的rhub设备连接的板卡插槽号失败";
				return null;
			}

			// 查询板卡相关的信息
			var boardIndex = $".0.0.{boardSlot}";
			var board = GetDevAttributeInfo(boardIndex, EnumDevType.board);
			if (null == board)
			{
				m_strLatestError = $"根据索引{boardIndex}未找到对应的板卡信息";
				return null;
			}

			var boardType = board.GetNeedUpdateValue("netBoardType");
			if (null == boardType || "-1" == boardType)
			{
				m_strLatestError = $"获取索引为{boardIndex}的板卡字段netBoardType值信息失败";
				return null;
			}

			// 查询以太网连接是否存在
			m_strEthRecordIndex = $"{boardIndex}{m_rhubDev.m_strOidIndex}.{m_nRhubEthPort}";
			if (!checkExist.Invoke(m_strEthRecordIndex, EnumDevType.rhub_prru))
			{
				var tmp = checkExist == RecordExistInDel ? "不" : "已";
				m_strLatestError = $"索引为{m_strEthRecordIndex}的以太网口规划表记录{tmp}存在";
				return null;
			}

			var bbi = new BoardBaseInfo("0", "0", boardSlot, boardType);
			return bbi;
		}

		private static bool HasConnectedToBoard(DevAttributeInfo rhub)
		{
			if (null == rhub)
			{
				throw new ArgumentNullException();
			}

			// 查询rhub设备是否已经建立到board的连接
			var boardSlot = NetDevRhub.GetRhubLinkToBoardSlotNo(rhub);
			return "-1" != boardSlot;
		}

		private bool SetRhubInfoInPico(DevAttributeInfo dev, int nEthPort, int nHubNo)
		{
			var mibName = $"netRRUOfp{m_nPicoPort}AccessEthernetPort";
			var bSucceed = dev.SetDevAttributeValue(mibName, nEthPort.ToString());
			if (!bSucceed)
			{
				m_strLatestError = $"设置索引为{dev.m_strOidIndex}的pico设备{mibName}字段值{nEthPort}失败";
				return false;
			}

			bSucceed = dev.SetDevAttributeValue("netRRUHubNo", nHubNo.ToString());
			if (!bSucceed)
			{
				m_strLatestError = $"设置索引为{dev.m_strOidIndex}的pico设备netRRUHubNo字段值{nHubNo}失败";
				return false;
			}

			return true;
		}

		// todo 需要细化
		private static bool SetIrPortInfoInPico(DevAttributeInfo hubdev, DevAttributeInfo picoDev, int nPicoIrPort)
		{
			// 从hub设备中找到任何一个光口连接的板卡光口号
			for (var i = 1; i < 5; i++)
			{
				var mibName = $"netRHUBOfp{i}AccessOfpPortNo";
				var value = hubdev.GetNeedUpdateValue(mibName);
				if (null == value || "-1" == value)
				{
					continue;
				}

				mibName = $"netRHUBOfp{i}AccessLinePosition";
				var apos = hubdev.GetNeedUpdateValue(mibName);
				if (null == apos || "-1" == apos)
				{
					continue;
				}

				var picoSlotMib = $"netRRUOfp{nPicoIrPort}AccessOfpPortNo";
				var picoAposMib = $"netRRUOfp{nPicoIrPort}AccessLinePosition";
				picoDev.SetFieldLatestValue(picoSlotMib, value);
				picoDev.SetFieldLatestValue(picoAposMib, apos);
				break;
			}

			return true;
		}

		/// <summary>
		/// 增加rhub到pico之间的连接以太网连接
		/// </summary>
		public bool AddEthPlanRecord(DevAttributeInfo rhubDev, DevAttributeInfo picoDev, Dictionary<EnumDevType, List<DevAttributeInfo>> mapAllData)
		{
			if (!HasConnectedToBoard(rhubDev))
			{
				Log.Error($"从索引为{rhubDev.m_strOidIndex}的rhub设备中查询rhub未连接到bbu");
				return false;
			}

			var mapPicoToRhub = NetDevRru.GetLinkedRhubInfoFromPico(picoDev);
			if (null == mapPicoToRhub || mapPicoToRhub.Count == 0)
			{
				Log.Error($"查询索引为{picoDev.m_strOidIndex}的pico设备连接rhub的连接信息为空");
				return false;
			}

			var boardSlot = NetDevRhub.GetRhubLinkToBoardSlotNo(rhubDev);
			if ("-1" == boardSlot)
			{
				Log.Error($"从索引为{rhubDev.m_strOidIndex}的rhub设备中查询连接bbu端口号返回-1");
				return false;
			}

			var bbuIdx = $".0.0.{boardSlot}";
			mapOriginData = mapAllData;

			foreach (var item in mapPicoToRhub)
			{
				var ridx = $"{bbuIdx}.{item.Value.strDevIndex.Trim('.')}.{item.Value.nPortNo}";
				if (RecordExistInDel(ridx, EnumDevType.rhub_prru))
				{
					Log.Error($"索引为{ridx}类型为rhub_prru的记录已经存在");
					continue;
				}

				var newRecord = new DevAttributeInfo(EnumDevType.rhub_prru, ridx);
				AddDevToMap(mapAllData, EnumDevType.rhub_prru, newRecord);

				SetIrPortInfoInPico(rhubDev, picoDev, item.Key);
			}

			return true;
		}

		#endregion 虚函数区

		#region 私有数据区

		private int m_nPicoPort;
		private int m_nRhubEthPort;
		private DevAttributeInfo m_picoDev;
		private DevAttributeInfo m_rhubDev;
		private string m_strEthRecordIndex;

		private EnumDevType m_ethRecordType;

		#endregion 私有数据区
	}
}