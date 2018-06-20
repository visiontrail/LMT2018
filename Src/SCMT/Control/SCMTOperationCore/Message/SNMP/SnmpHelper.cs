﻿/*----------------------------------------------------------------
// Copyright (C) 2017 大唐移动通信设备有限公司 版权所有;
//
// 文件名：SnmpHelper.cs
// 文件功能描述：Snmp报文类;
// 创建人：;
// 版本：V1.0
// 创建标识：创建文件;
// 时间：
//----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using SnmpSharpNet;
using System.Threading.Tasks;
using LogManager;
using System.Globalization;
using System.Text;

namespace SCMTOperationCore.Message.SNMP
{
	/// <summary>
	/// 抽象SNMP报文，以便后续扩展SNMPV3;
	/// </summary>
	public abstract class SnmpHelper
	{
		public Dictionary<string, string> m_Response { get; set; }        // Get返回的结果;
		public string m_DestIPAddr { get; set; }                              // 代理目标IP地址
		public long m_DestPort { get { return 161; } set { m_DestPort = value; } }  //目的端口
		public string m_Community { get; set; }                           // 代理目标的Community
		public long m_TrapPort { get { return 162; } set { m_TrapPort = value; } }  //管理端Trap监听端口
		public long m_TimeOut { get { return 1000; } set { m_TimeOut = value; } }   //设置的超时时间(10s超时，以10ms为单位)
		public string m_ErrorStatus { get; set; }                         // 错误码;
		protected SnmpV2Packet m_Result { get; set; }                     // 返回结果;
		public SnmpVersion m_Version { get { return SnmpVersion.Ver2; } set { m_Version = value; } } // SNMP版本,当前基站使用SNMP固定为Ver.2;
		public List<string> PduList { get; set; }                         // Snmp报文的Pdu列表

		public UdpTarget m_target { get; set; }

		public AgentParameters m_Param { get; set; }

		public SnmpHelper(string commnuity, string destIpAddr)
		{
			this.m_DestIPAddr = destIpAddr;
			this.m_Community = commnuity;

			ConnectToAgent(m_Community, m_DestIPAddr);
		}

		public SnmpHelper(string commnuity, string destIpAddr, long destPort, long trapPort
			, SnmpVersion version, long timeOut)
		{
			this.m_Community = commnuity;
			this.m_DestIPAddr = destIpAddr;
			this.m_DestPort = destPort;
			this.m_TrapPort = trapPort;
			this.m_Version = version;
			this.m_TimeOut = timeOut;

			ConnectToAgent(m_Community, m_DestIPAddr);
		}

   

		/// <summary>
		/// GetRequest的对外接口
		/// </summary>
		/// <param name="PduList">需要查询的Pdu列表</param>
		/// <param name="Community">需要设置的Community</param>
		/// <param name="IpAddress">需要设置的IP地址</param>
		/// <returns></returns>
		public abstract Dictionary<string, string> GetRequest(List<string> PduList, string Community, string IpAddress);

		/// <summary>
		/// GetRequest的对外接口，客户端通过异步回调获取数据;
		/// </summary>
		/// <param name="callback">异步回调方法</param>
		/// <param name="PduList">需要查询的Pdu列表</param>
		/// <param name="Community">需要设置的Community</param>
		/// <param name="IpAddress">需要设置的IP地址</param>
		public abstract void GetRequest(AsyncCallback callback, List<string>PduList, string Community, string IpAddress);

		/// <summary>
		/// GetRequest的对外接口，入参只有Pdulist
		/// </summary>
		/// <param name="PduList"></param>
		/// <returns></returns>
		public abstract SnmpV2Packet GetRequest(List<string> PduList);

		/// <summary>
		/// Get请求
		/// </summary>
		/// <param name="pdu"></param>
		/// <returns></returns>
		public abstract SnmpV2Packet GetRequest(Pdu pdu);


		public abstract bool GetRequestAsync(Pdu pdu, SnmpAsyncResponse callback);

		/// <summary>
		/// SetRequest的对外接口
		/// </summary>
		/// <param name="PduList">需要查询的Pdu列表</param>
		/// <param name="Community">需要设置的Community</param>
		/// <param name="IpAddress">需要设置的IP地址</param>
		public abstract void SetRequest(Dictionary<string, string> PduList, string Community, string IpAddress);

		/// <summary>
		/// Set请求
		/// </summary>
		/// <param name="pdu"></param>
		/// <returns></returns>
		public abstract SnmpV2Packet SetRequest(Pdu pdu);

		public abstract bool SetRequestAsync(Pdu pdu, SnmpAsyncResponse callback);

		/// <summary>
		/// SetRequest的对外接口;
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="PduList"></param>
		public abstract void SetRequest(AsyncCallback callback, List<string>PduList);


		public abstract SnmpPacket GetNextRequest(Pdu pdu);

		/// <summary>
		/// GetRequest的对外接口;
		/// </summary>
		/// <param name="callback">获得结果后的异步回调</param>
		/// <param name="PduList">最开始的入参</param>
		public abstract void GetNextRequest(AsyncCallback callback, List<string> PduList);

		/// <summary>
		/// 连接代理;
		/// </summary>
		/// <param name="Community"></param>
		/// <param name="IpAddr"></param>
		/// <returns></returns>
		protected UdpTarget ConnectToAgent(string Community, string IpAddr)
		{
			OctetString community = new OctetString(Community);
			m_Param = new AgentParameters(community);
			m_Param.Version = SnmpVersion.Ver2;
			IpAddress agent = new IpAddress(IpAddr);

			// 创建代理(基站);
			m_target = new UdpTarget((IPAddress)agent, 161, 2000, 1);

			return m_target;
		}

		/// <summary>
		/// GetNext;
		/// </summary>
		protected Dictionary<string, string> GetNext(ref List<string> oid)
		{
			SimpleSnmp SnmpMsg = new SimpleSnmp(this.m_DestIPAddr, this.m_Community);
			Dictionary<string, string> Res = new Dictionary<string, string>();
			List<string> NextOids = new List<string>();
			Dictionary<Oid, AsnType> TempRes = new Dictionary<Oid, AsnType>();
			string[] oidargs;

			if(oid.Count != 0)
			{
				oidargs = oid.ToArray();
				TempRes = SnmpMsg.GetNext(this.m_Version, oidargs);
				if (TempRes != null)
				{
					oid.Clear();
					foreach (KeyValuePair<Oid, AsnType> entry in TempRes)
					{
						Res.Add(entry.Key.ToString(), entry.Value.ToString());
						oid.Add(entry.Key.ToString());                                // 将Next信息回填;
					}
				}
				else
				{
					oid.Clear();
				}
			}
			
			return Res;
		}

		/// <summary>
		/// 将SNMP的DateTime类型转换为字符串
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public static String SnmpDateTime2String(OctetString v)
		{
			byte[] bts = v.ToArray();

			byte[] format_str = new byte[128];   //保存格式化过后的时间字符串  

			int year;

			int month;

			int day;

			int hour;

			int minute;

			int second;

			//int msecond;        
			year = bts[0] * 256 + bts[1]; // 前2个字节为年份
			month = bts[2];
			day = bts[3];
			hour = bts[4];
			minute = bts[5];
			second = bts[6];
			//msecond = bts[7];  
			//以下为格式化字符串  
			int index = 3;
			int temp = year;

			for (; index >= 0; index--)
			{
				format_str[index] = (byte)(48 + (temp - temp / 10 * 10));
				temp /= 10;
			}

			format_str[4] = (Byte)'-';
			index = 6;
			temp = month;
			for (; index >= 5; index--)
			{
				format_str[index] = (byte)(48 + (temp - temp / 10 * 10));
				temp /= 10;
			}

			format_str[7] = (Byte)'-';
			index = 9;
			temp = day;
			for (; index >= 8; index--)
			{

				format_str[index] = (byte)(48 + (temp - temp / 10 * 10));

				temp /= 10;

			}
			format_str[10] = (Byte)' ';
			index = 12;
			temp = hour;
			for (; index >= 11; index--)
			{
				format_str[index] = (byte)(48 + (temp - temp / 10 * 10));
				temp /= 10;
			}

			format_str[13] = (Byte)':';
			index = 15;
			temp = minute;
			for (; index >= 14; index--)
			{

				format_str[index] = (byte)(48 + (temp - temp / 10 * 10));

				temp /= 10;

			}

			format_str[16] = (Byte)':';
			index = 18;
			temp = second;
			for (; index >= 17; index--)
			{
				format_str[index] = (byte)(48 + (temp - temp / 10 * 10));

				temp /= 10;

			}

			//int i = 6;  
			//while (i >= 0)  
			//{  
			//    Console.WriteLine("{0}", bts[i]);  
			//    i--;  
			//}  

			string strTmp = System.Text.Encoding.Default.GetString(format_str);

			return strTmp; // new String(format_str);  

		}

		/// <summary>
		/// 将日期字符串转换为byte数组，并转换为网络字节序
		/// </summary>
		/// <param name="strDateTime"></param>
		/// <returns></returns>
		public static byte[] SnmpStrDateTime2Bytes(string strDateTime)
		{
			DateTime dt = String2DateTime(strDateTime);

			return SnmpDateTime2Bytes(dt);
		}

		/// <summary>
		/// 将DateTime转换为byte数组，并转换为网络字节序
		/// </summary>
		/// <param name="dt"></param>
		/// <returns></returns>
		public static byte[] SnmpDateTime2Bytes(DateTime dt)
		{
			byte[] bytes = new byte[8];
			if (dt != null)
			{
				// 年
				ushort year = (ushort)dt.Year;
				byte[] bytesYear = BitConverter.GetBytes(year);
				if (BitConverter.IsLittleEndian) // 转换为网络字节序
				{
					Array.Reverse(bytesYear);
				}
				Buffer.BlockCopy(bytesYear, 0, bytes, 0, 2);

				bytes[2] = Convert.ToByte(dt.Month.ToString(), 10);
				bytes[3] = Convert.ToByte(dt.Day.ToString(), 10);
				bytes[4] = Convert.ToByte(dt.Hour.ToString(), 10);
				bytes[5] = Convert.ToByte(dt.Minute.ToString(), 10);
				bytes[6] = Convert.ToByte(dt.Second.ToString(), 10);
			}

			return bytes;
		}

		/// <summary>
		/// 将日期字符串转换为DateTime类型
		/// </summary>
		/// <param name="strDateTime"></param>
		/// <param name="strFormat"></param>
		/// <returns></returns>
		public static DateTime String2DateTime(string strDateTime, string strFormat = "yyyy-MM-dd HH:mm:ss")
		{
			DateTime dt;

			DateTimeFormatInfo dtForm = new DateTimeFormatInfo();
			dtForm.ShortDatePattern = strFormat;

			dt = Convert.ToDateTime(strDateTime, dtForm);

			return dt;
		}

		/// <summary>
		/// 将IP地址转换为byte数组
		/// </summary>
		/// <param name="strIpAddr"></param>
		/// <returns></returns>
		public static byte[] SnmpStrIpAddr2Bytes(string strIpAddr)
		{
			byte[] bytes = new byte[4];
			if (!string.IsNullOrEmpty(strIpAddr))
			{
				IpAddress ipAddr = new IpAddress(strIpAddr);
				bytes = BitConverter.GetBytes(ipAddr.ToUInt32()); //ipAddr.ToArray();
			}

			return bytes;
		}

		/// <summary>
		/// Mac地址字符串转换为byte数组
		/// </summary>
		/// <param name="strMacAddr"></param>
		/// <returns></returns>
		public static byte[] StrMacAddr2Bytes(string strMacAddr)
		{
			return StrHex2Bytes(strMacAddr);
		}

		/// <summary>
		/// 十六进制字串转换为bytes数组
		/// </summary>
		/// <param name="strHex"></param>
		/// <returns></returns>
		public static byte[] StrHex2Bytes(string strHex)
		{
			if (string.IsNullOrEmpty(strHex))
			{
				return null;
			}
			strHex = strHex.Replace(":", "");
			strHex = strHex.Replace(" ", "");

			int length = strHex.Length;
			byte[] bytes = new byte[length / 2];

			for (int i = 0; i < length; i += 2)
			{
				bytes[i / 2] = Convert.ToByte(strHex.Substring(i, 2), 16);
			}

			return bytes;
		}

		/// <summary>
		/// 将Unsigned32Array转换为byte数组
		/// </summary>
		/// <param name="strUnsigned32"></param>
		/// <returns></returns>
		public static byte[] Unsigned32Array2Bytes(string strUnsigned32)
		{
			string strTmp = strUnsigned32;
			// 删除左右大括号
			strTmp = strTmp.Replace("{", "");
			strTmp = strTmp.Replace("}", "");
			// 获取数值
			string[] strValArray = strTmp.Split(',');

			// 返回值
			byte[] bytes = new byte[strValArray.Length * 4];

			// 将数值转换为byte数组
			int index = 0;
			foreach (string strVal in strValArray)
			{
				if (string.IsNullOrEmpty(strVal))
				{
					break;
				}

				uint intVal = UInt32.Parse(strVal);
				byte[] bytesTmp = BitConverter.GetBytes(intVal);
				if (BitConverter.IsLittleEndian) // 转换为网络序
				{
					Array.Reverse(bytesTmp);
				}
				Buffer.BlockCopy(bytesTmp, 0, bytes, index * 4, sizeof(UInt32));
				index++;
			}

			return bytes;
		}

		/// <summary>
		/// 从Octet String类型转为U32数组的字符串表示
		/// </summary>
		/// <param name="octStr"></param>
		/// <returns></returns>
		public static string OctetStrToU32Array(OctetString octStr)
		{
			// 数组个数
			int valCount = octStr.Length / sizeof(UInt32);
			if (valCount <= 0)
			{
				return "";
			}

			StringBuilder sb = new StringBuilder();
			sb.Append("{");

			byte[] bytes = octStr.ToArray();

			// 每四个字节转换为一个UInt32数字
			for (int i = 0; i < valCount;)
			{
				// 拷贝4个字节，转换为整数
				byte[] bsTmp = new byte[sizeof(UInt32)];
				Buffer.BlockCopy(bsTmp, 0, bytes, i * 4, sizeof(UInt32));
				// 大小端转换
				if (BitConverter.IsLittleEndian)
				{
					Array.Reverse(bsTmp);
				}

				UInt32 intTmp = BitConverter.ToUInt32(bsTmp, 0);
				sb.Append(intTmp);

				i++;
				if (i < valCount)
				{
					sb.Append(",");
				}
			}

			sb.Append("}");

			return sb.ToString();
		}

		/// <summary>
		/// 从Octet String类型转为S32数组的字符串表示
		/// </summary>
		/// <param name="octStr"></param>
		/// <returns></returns>
		public static string OctetStrToS32Array(OctetString octStr)
		{
			// 数字长度
			int valCount = octStr.Length / sizeof(Int32);
			if (valCount <= 0)
			{
				return "";
			}

			StringBuilder sb = new StringBuilder();
			sb.Append("{");

			byte[] bytes = octStr.ToArray();

			// 每4个字节转换为一个Int32数字
			for (int i = 0; i < valCount;)
			{
				byte[] bsTmp = new byte[sizeof(Int32)];
				Buffer.BlockCopy(bsTmp, 0, bytes, i * 4, sizeof(Int32));
				// 大小端转换
				if (BitConverter.IsLittleEndian)
				{
					Array.Reverse(bsTmp);
				}

				Int32 intTmp = BitConverter.ToInt32(bsTmp, 0);
				sb.Append(intTmp);

				i++;
				if (i < valCount)
				{
					sb.Append(",");
				}
			}

			sb.Append("}");

			return sb.ToString();
		}

		/// <summary>
		/// MncMccType类型转换为byte数组
		/// </summary>
		/// <param name="strMncMccVal"></param>
		/// <returns></returns>
		public static byte[] MncMccType2Bytes(string strMncMccVal)
		{
			// TODO: 原来方法要减48，目前没减确定对不对？？？
			return System.Text.Encoding.ASCII.GetBytes(strMncMccVal);
		}

		/// <summary>
		/// 将OctetString转换为MncMccType字符串
		/// </summary>
		/// <param name="octStr"></param>
		/// <returns></returns>
		public static string OctetStr2MncMccTypeStr(OctetString octStr)
		{
			if (null == octStr || octStr.Length <= 0)
			{
				return "";
			}

			StringBuilder sb = new StringBuilder();
			byte[] bytes = octStr.ToArray();

			foreach (byte bt in bytes)
			{
				if (bt == 255)
				{
					// 无效字段不需要记录
					break;
				}

				sb.Append((int)bt);
			}

			return sb.ToString();
		}


	}
	/// <summary>
	/// 异步获取SNMP结果参数;
	/// </summary>
	public class SnmpHelperResult : IAsyncResult
	{
		/// <summary>
		/// Key：oid;
		/// value：数值;
		/// </summary>
		private Dictionary<string, string> m_Result;

		public void SetSNMPReslut(Dictionary<string, string> res)
		{
			this.m_Result = res;
		}

		public object AsyncState
		{
			get
			{
				return m_Result;
			}
		}

		public WaitHandle AsyncWaitHandle
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool CompletedSynchronously
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool IsCompleted
		{
			get
			{
				throw new NotImplementedException();
			}
		}


	}

	/// <summary>
	/// Snmp报文V2c版本
	/// </summary>
	public class SnmpHelperV2c : SnmpHelper
	{
		public SnmpHelperV2c(string Commnuity, string ipaddr) : base(Commnuity, ipaddr)
		{
		}


		/// <summary>
		/// GetRequest
		/// </summary>
		/// <param name="PduList">需要查询的Pdu列表</param>
		/// <param name="Community">需要设置的Community</param>
		/// <param name="IpAddr">需要设置的IP地址</param>
		/// <returns>返回一个字典,键值为OID,值为MIB值;</returns>
		public override Dictionary<string, string> GetRequest(List<string> PduList, string Community, string IpAddr)
		{
			Dictionary<string, string> rest = new Dictionary<string, string>();
			OctetString community = new OctetString(Community);
			AgentParameters param = new AgentParameters(community);
			param.Version = SnmpVersion.Ver2;

			// 创建代理(基站);
			UdpTarget target = ConnectToAgent(Community, IpAddr);

			// 填写Pdu请求;
			Pdu pdu = new Pdu(PduType.Get);
			foreach (string pdulist in PduList)
			{
				pdu.VbList.Add(pdulist);
			}
			
			// 接收结果;
			m_Result = (SnmpV2Packet)target.Request(pdu, param);

			// 如果结果为空,则认为Agent没有回响应;
			if (m_Result != null)
			{
				// ErrorStatus other then 0 is an error returned by 
				// the Agent - see SnmpConstants for error definitions
				if (m_Result.Pdu.ErrorStatus != 0)
				{
					// agent reported an error with the request
					Console.WriteLine("Error in SNMP reply. Error {0} index {1}",
						m_Result.Pdu.ErrorStatus,
						m_Result.Pdu.ErrorIndex);

					rest.Add(m_Result.Pdu.ErrorIndex.ToString(), m_Result.Pdu.ErrorStatus.ToString());
				}
				else
				{
					for (int i = 0; i < m_Result.Pdu.VbCount; i++)
					{
						rest.Add(m_Result.Pdu.VbList[i].Oid.ToString(), m_Result.Pdu.VbList[i].Value.ToString());
					}

				}
			}
			else
			{
				Console.WriteLine("No response received from SNMP agent.");
			}

			target.Close();
			return rest;
		}

		/// <summary>
		/// 带有异步回调的GetResponse;
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="PduList"></param>
		/// <param name="Community"></param>
		/// <param name="IpAddr"></param>
		public override void GetRequest(AsyncCallback callback, List<string> PduList, string Community, string IpAddr)
		{
			// 当确认全部获取SNMP数据后，调用callback回调;
			SnmpHelperResult res = new SnmpHelperResult();
			OctetString community = new OctetString(Community);
			AgentParameters param = new AgentParameters(community);
			Dictionary<string, string> rest = new Dictionary<string, string>();
			param.Version = SnmpVersion.Ver2;

			// 创建代理(基站);
			UdpTarget target = ConnectToAgent(Community,IpAddr);

			// Pdu请求形式Get;
			Pdu pdu = new Pdu(PduType.Get);
			foreach (string pdulist in PduList)
			{
				pdu.VbList.Add(pdulist);
			}
			
			Task tsk = Task.Factory.StartNew(()=> {
				
				// 接收结果;
				m_Result = (SnmpV2Packet)target.Request(pdu, param);

				if (m_Result != null)
				{
					// ErrorStatus other then 0 is an error returned by 
					// the Agent - see SnmpConstants for error definitions
					if (m_Result.Pdu.ErrorStatus != 0)
					{
						// agent reported an error with the request
						Console.WriteLine("Error in SNMP reply. Error {0} index {1}",
							m_Result.Pdu.ErrorStatus,
							m_Result.Pdu.ErrorIndex);

						rest.Add(m_Result.Pdu.ErrorIndex.ToString(), m_Result.Pdu.ErrorStatus.ToString());
						res.SetSNMPReslut(rest);
						Thread.Sleep(3111);
						callback(res);
					}
					else
					{
						for (int i = 0; i < m_Result.Pdu.VbCount; i++)
						{
							rest.Add(m_Result.Pdu.VbList[i].Oid.ToString(), m_Result.Pdu.VbList[i].Value.ToString());
							res.SetSNMPReslut(rest);
							Thread.Sleep(3111);
							callback(res);
						}

					}
				}
				else
				{
					Console.WriteLine("No response received from SNMP agent.");
				}

				target.Close();
			});
			
		}

		/// <summary>
		/// 只需要填入Pdulist的GetResponse;
		/// </summary>
		/// <param name="VbList"></param>
		/// <returns></returns>
		public override SnmpV2Packet GetRequest(List<string> VbList)
		{
			if (m_target == null)
			{
				Log.Error("SNMP m_target = null.");
				return null;
			}

			// Log msg
			string logMsg;
			SnmpV2Packet result = null;

			Pdu pdu = new Pdu(PduType.Get);

			foreach(string oid in VbList)
			{
				pdu.VbList.Add(oid);
			}

			try
			{
				result = (SnmpV2Packet)m_target.Request(pdu, m_Param);
				if (null != result)
				{
					if (result.Pdu.ErrorStatus != 0)
					{
						logMsg = string.Format("Error in SNMP reply. Error {0} index {1}"
							, result.Pdu.ErrorStatus, result.Pdu.ErrorIndex);
						Log.Error(logMsg);
					}
					else
					{
						foreach(Vb vb in result.Pdu.VbList)
						{
							logMsg = string.Format("ObjectName={0}, Type={1}, Value={2}"
								, vb.Oid.ToString(), SnmpConstants.GetTypeName(vb.Value.Type), vb.Value.ToString());
							Log.Debug(logMsg);
						}
					}
				}
				else
				{
					Log.Error("No response received from SNMP agent.");
				}
			}
			catch ( Exception e)
			{
				Log.Error(e.Message.ToString());
				throw e;
			}
		   

			return result;

		}

		public override SnmpV2Packet GetRequest(Pdu pdu)
		{
			if (m_target == null)
			{
				Log.Error("SNMP m_target = null.");
				return null;
			}

			if (pdu == null)
			{
				Log.Error("SNMP请求参数pdu为空");
				return null;
			}

			// Log msg
			string logMsg;
			SnmpV2Packet result = null;

			pdu.Type = PduType.Get;
			try
			{
				result = (SnmpV2Packet)m_target.Request(pdu, m_Param);
				if (null != result)
				{
					if (result.Pdu.ErrorStatus != 0)
					{
						logMsg = string.Format("Error in SNMP reply. Error {0} index {1}"
							, result.Pdu.ErrorStatus, result.Pdu.ErrorIndex);
						Log.Error(logMsg);
					}
					else
					{
						foreach (Vb vb in result.Pdu.VbList)
						{
							logMsg = string.Format("ObjectName={0}, Type={1}, Value={2}"
								, vb.Oid.ToString(), SnmpConstants.GetTypeName(vb.Value.Type), vb.Value.ToString());
							//Log.Debug(logMsg);
						}
					}
				}
				else
				{
					Log.Error("No response received from SNMP agent.");
				}
			}
			catch (Exception e)
			{
				Log.Error(e.Message.ToString());
				throw e;
			}


			return result;

		}

		/// <summary>
		/// 异步GetRequest
		/// </summary>
		/// <param name="pdu"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		public override bool GetRequestAsync(Pdu pdu, SnmpAsyncResponse callback)
		{
			bool rs = false;
			if (m_target == null)
			{
				Log.Error("SNMP m_target = null.");
				return false;
			}

			if (pdu == null)
			{
				Log.Error("SNMP请求参数pdu为空");
				return false;
			}
			pdu.Type = PduType.Get;

			try
			{
				rs = m_target.RequestAsync(pdu, m_Param, callback);
				if (!rs)
				{
					Log.Error("SNMP异步请求错误");
				}
				
			}
			catch (Exception e)
			{
				Log.Error(e.Message.ToString());
				throw e;
			}

			return rs;
		}


		/// <summary>
		/// GetNextRequest
		/// </summary>
		/// <param name="pdu"></param>
		/// <returns></returns>
		public override SnmpPacket GetNextRequest(Pdu pdu)
		{
			if (m_target == null)
			{
				Log.Error("SNMP m_target = null.");
				return null;
			}

			if (pdu == null)
			{
				Log.Error("SNMP请求参数pdu为空");
				return null;
			}

			pdu.Type = PduType.GetNext;
			// Log msg
			string logMsg;
			SnmpV2Packet result = null;

			try
			{
				result = (SnmpV2Packet)m_target.Request(pdu, m_Param);
				if (null != result)
				{
					if (result.Pdu.ErrorStatus != 0)
					{
						logMsg = string.Format("Error in SNMP reply. Error {0}, index {1}"
							, result.Pdu.ErrorStatus, result.Pdu.ErrorIndex);
						//Log.Error(logMsg);
						
					}
					else
					{
						foreach (Vb vb in result.Pdu.VbList)
						{
							logMsg = string.Format("ObjectName={0}, Type={1}, Value={2}"
								, vb.Oid.ToString(), SnmpConstants.GetTypeName(vb.Value.Type), vb.Value.ToString());
							//Log.Debug(logMsg);
						}
					}
				}
				else
				{
					Log.Error("No response received from SNMP agent.");
				}
			}
			catch (Exception e)
			{
				Log.Error(e.Message.ToString());
				throw e;
			}

			return result;
		}
		/// <summary>
		/// SNMP Set
		/// </summary>
		/// <param name="pdu"></param>
		/// <param name="community"></param>
		/// <returns></returns>
		public override SnmpV2Packet SetRequest(Pdu pdu)
		{
			if (m_target == null)
			{
				Log.Error("SNMP m_target = null.");
				return null;
			}

			if (pdu == null)
			{
				Log.Error("Parameter pdu=null.");
				return null;
			}
			pdu.Type = PduType.Set;

			// log msg
			string logMsg;

			SnmpV2Packet response;

			try
			{
				response = (SnmpV2Packet)m_target.Request(pdu, m_Param);
			}
			catch (Exception e)
			{
				Log.Error(e.Message.ToString());
				throw e;
			}

			if (response == null)
			{
				Log.Error("SNMP Set 命令错误");
			}
			else
			{
				if (response.Pdu.ErrorStatus != 0)
				{
					logMsg = string.Format("SNMP agent returned ErrorStatus {0} on index {1}"
						, response.Pdu.ErrorStatus, response.Pdu.ErrorIndex);
				}
				else
				{
					foreach(Vb vb in response.Pdu.VbList)
					{
						logMsg = string.Format("ObectName={0}, Type={1}, Value={2}"
							, vb.Oid.ToString(), vb.Value.Type.ToString(), vb.Value.ToString());
					}
				}
			}

			return response;
		}
  

		/// <summary>
		/// SetRequest的SnmpV2c版本
		/// </summary>
		/// <param name="PduList">需要查询的Pdu列表</param>
		/// <param name="Community">需要设置的Community</param>
		/// <param name="IpAddress">需要设置的IP地址</param>
		public override void SetRequest(Dictionary<string, string> PduList, string Community, string IpAddress)
		{
			// Prepare target
			UdpTarget target = new UdpTarget((IPAddress)new IpAddress(IpAddress));
			// Create a SET PDU
			Pdu pdu = new Pdu(PduType.Set);
			foreach (var list in PduList)
			{
				pdu.VbList.Add(new Oid(list.Key), new OctetString(list.Value));
			}

			// Set Agent security parameters
			AgentParameters aparam = new AgentParameters(SnmpVersion.Ver2, new OctetString(Community));
			// Response packet
			SnmpV2Packet response;
			try
			{
				// Send request and wait for response
				response = target.Request(pdu, aparam) as SnmpV2Packet;
			}
			catch (Exception ex)
			{
				// If exception happens, it will be returned here
				Console.WriteLine(String.Format("Request failed with exception: {0}", ex.Message));
				target.Close();
				return;
			}
			// Make sure we received a response
			if (response == null)
			{
				Console.WriteLine("Error in sending SNMP request.");
			}
			else
			{
				// Check if we received an SNMP error from the agent
				if (response.Pdu.ErrorStatus != 0)
				{
					Console.WriteLine(String.Format("SNMP agent returned ErrorStatus {0} on index {1}",
						response.Pdu.ErrorStatus, response.Pdu.ErrorIndex));
				}
				else
				{
					// Everything is ok. Agent will return the new value for the OID we changed
					Console.WriteLine(String.Format("Agent response {0}: {1}",
						response.Pdu[0].Oid.ToString(), response.Pdu[0].Value.ToString()));
				}
			}
		}


		/// <summary>
		/// 异步SetRequest
		/// </summary>
		/// <param name="pdu"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		public override bool SetRequestAsync(Pdu pdu, SnmpAsyncResponse callback)
		{
			bool rs = false;
			if (m_target == null)
			{
				Log.Error("SNMP m_target = null.");
				return false;
			}

			if (pdu == null)
			{
				Log.Error("SNMP请求参数pdu为空");
				return false;
			}
			pdu.Type = PduType.Set;

			try
			{
				rs = m_target.RequestAsync(pdu, m_Param, callback);
				if (!rs)
				{
					Log.Error("SNMP异步请求错误");
				}

			}
			catch (Exception e)
			{
				Log.Error(e.Message.ToString());
				throw e;
			}

			return rs;
		}

		public override void SetRequest(AsyncCallback callback, List<string> PduList)
		{
			throw new NotImplementedException();
		}
		
		/// <summary>
		/// GetNext的对外接口
		/// 该函数会在第一次收到客户端请求后;
		/// 在每次收到基站的GetResponse之后，都会调用客户端注册的回调函数
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="PduList"></param>
		public override void GetNextRequest(AsyncCallback callback, List<string> PduList)
		{
			SnmpHelperResult res = new SnmpHelperResult();
			if ((PduList.Count == 0) && (PduList == null))      // 如果PduList内容为空，则不进行处理;
			{
				return;
			}
			Task tsk = Task.Factory.StartNew(() =>
			{
				Dictionary<string, string> NextRest = new Dictionary<string, string>();
				List<List<string>> AllList = new List<List<string>>();
				bool GetNextorNot = true;

				AllList.Add(PduList);
				while(GetNextorNot)
				{

					NextRest = this.GetNext(ref PduList);           // 得到返回结果;
					if (PduList.Count == 0)
					{
						GetNextorNot = false;
					}
					res.SetSNMPReslut(NextRest);
					callback(res);
				}
				return;
			});
		}


		

	}

}