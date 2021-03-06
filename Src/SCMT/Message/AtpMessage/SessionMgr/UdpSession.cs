﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using AtpMessage.MsgDefine;
using MsgQueue;

namespace AtpMessage.SessionMgr
{
	/// <summary>
	/// atp和gtsa模式通信使用的是udp协议
	/// </summary>
	internal class UdpSession : IASession
	{
		private readonly UdpClient _udpClient;
		private IPEndPoint _ipTargetEp;

		private readonly SubscribeClient _subClient;
		private readonly string _prefix;

		private Thread _recvThread;

		private bool MsgSendCompleted { get; set; }

		public UdpSession(Target target)
		{
			_udpClient = new UdpClient();
			_ipTargetEp = new IPEndPoint(IPAddress.Parse(target.raddr), target.rport);
			_udpClient.Connect(_ipTargetEp);

			_recvThread = new Thread(RecvFromBoard);		//先启动接收线程，否则可能漏掉数据包
			_recvThread.Start();

			_prefix = $"{target.raddr}:{target.rport}";       //订阅这个消息是用于运行过程中给板卡发送信息
			_subClient = new SubscribeClient(CommonPort.PubServerPort);

			string topic = $"udp-send://{_prefix}";
			_subClient.AddSubscribeTopic(topic, OnSubscribe);
			_subClient.Run();

			string desc = "用于运行过程中给板卡发送信息，需要在创建链接后使用。收到网卡端的数据后，更换前缀为udp-recv://后发布原始数据";
			Type type = typeof(System.Array);
			TopicManager.GetInstance().AddTopic(new TopicInfo(topic, desc, type));
		}

		//异步发送信息回调函数
		public void SendCallback(IAsyncResult ar)
		{
			MsgSendCompleted = ar.IsCompleted;
		}

		//异步发送数据
		public void SendAsync(byte[] dataBytes)
		{
			MsgSendCompleted = false;
			try
			{
				_udpClient.BeginSend(dataBytes, dataBytes.Length, SendCallback, _udpClient);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}

			while (!MsgSendCompleted)
			{
				Thread.Sleep(100);
			}
		}

		public bool Init(string lip)
		{
			return true;
		}

		public bool Stop()
		{
			Dispose();
			return true;
		}

		//接收数据线程函数
		private void RecvFromBoard(object obj)
		{
			while(true)
			{
				try
				{
					var revBytes = _udpClient.Receive(ref _ipTargetEp);
					var header = GetHeaderFromBytes.GetHeader(revBytes);
					PublishHelper.PublishMsg($"udp-recv://{_prefix}", revBytes);
				}
				catch (SocketException e)
				{
					if (SocketError.Interrupted == e.SocketErrorCode)
					{
						break;
					}
				}
			}
		}

		private void OnSubscribe(SubscribeMsg msg)
		{
			SendAsync(msg.Data);
		}

		public void Dispose()
		{
			_udpClient?.Close();
			_recvThread?.Join(100);
			_subClient?.Stop();
			_subClient?.Dispose();
		}
	}
}
