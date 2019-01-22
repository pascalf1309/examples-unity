﻿using System;
using UnityEngine;
#if DOT_NET
using System.Net.WebSockets;
#elif UNITY_WEBGL && !UNITY_EDITOR
using AOT;
using System.Collections.Generic;
#else
using WebSocketSharp;
#endif

public class BrainCloudWebSocket
{
#if DOT_NET
#elif UNITY_WEBGL && !UNITY_EDITOR
	private NativeWebSocket NativeWebSocket;   
    private static Dictionary<int, BrainCloudWebSocket> webSocketInstances =
        new Dictionary<int, BrainCloudWebSocket>();
#else
    private WebSocket m_webSocket;
#endif

    public BrainCloudWebSocket(string url)
    {
#if DOT_NET
#elif UNITY_WEBGL && !UNITY_EDITOR
		NativeWebSocket = new NativeWebSocket(url);
		NativeWebSocket.SetOnOpen(NativeSocket_OnOpen);
		NativeWebSocket.SetOnMessage(NativeSocket_OnMessage);
		NativeWebSocket.SetOnError(NativeSocket_OnError);
		NativeWebSocket.SetOnClose(NativeSocket_OnClose);
		webSocketInstances.Add(NativeWebSocket.Id, this);
#else
        m_webSocket = new WebSocket(url);
        m_webSocket.ConnectAsync();
        m_webSocket.AcceptAsync();
        m_webSocket.OnOpen += WebSocket_OnOpen;
        m_webSocket.OnMessage += WebSocket_OnMessage;
        m_webSocket.OnError += WebSocket_OnError;
        m_webSocket.OnClose += WebSocket_OnClose;
#endif
    }

    public void Close()
    {
#if DOT_NET
#elif UNITY_WEBGL && !UNITY_EDITOR
        if (NativeWebSocket == null)
			return;
        webSocketInstances.Remove(NativeWebSocket.Id);
		NativeWebSocket.CloseAsync();
		NativeWebSocket = null;
#else
        if (m_webSocket == null)
            return;
        m_webSocket.CloseAsync();
        m_webSocket.OnOpen -= WebSocket_OnOpen;
        m_webSocket.OnMessage -= WebSocket_OnMessage;
        m_webSocket.OnError -= WebSocket_OnError;
        m_webSocket.OnClose -= WebSocket_OnClose;
        m_webSocket = null;
#endif
    }

#if DOT_NET
#elif UNITY_WEBGL && !UNITY_EDITOR
    [MonoPInvokeCallback(typeof(Action<int>))]
	public static void NativeSocket_OnOpen(int id) {
		if (webSocketInstances.ContainsKey(id) && webSocketInstances[id].OnOpen != null)
			webSocketInstances[id].OnOpen(webSocketInstances[id]);
	}

	[MonoPInvokeCallback(typeof(Action<int>))]
	public static void NativeSocket_OnMessage(int id) {
		if (webSocketInstances.ContainsKey(id))
        {
	    	byte[] data = webSocketInstances[id].NativeWebSocket.Receive();
	    	if (webSocketInstances[id].OnMessage != null)
	    		webSocketInstances[id].OnMessage(webSocketInstances[id], data);
        }
	}

	[MonoPInvokeCallback(typeof(Action<int>))]
	public static void NativeSocket_OnError(int id) {
		if (webSocketInstances.ContainsKey(id) && webSocketInstances[id].OnError != null)
			webSocketInstances[id].OnError(webSocketInstances[id], webSocketInstances[id].NativeWebSocket.Error);
	}

	[MonoPInvokeCallback(typeof(Action<int, int>))]
	public static void NativeSocket_OnClose(int code, int id) {    
		CloseError errorInfo = CloseError.Get(code);
		if (webSocketInstances.ContainsKey(id) && webSocketInstances[id].OnClose != null)
			webSocketInstances[id].OnClose(webSocketInstances[id], errorInfo.Code, errorInfo.Message);
	}
#else
    private void WebSocket_OnOpen(object sender, EventArgs e)
    {
        if (OnOpen != null)
            OnOpen(this);
    }

    private void WebSocket_OnMessage(object sender, MessageEventArgs e)
    {
        if (OnMessage != null)
            OnMessage(this, e.RawData);
    }

    private void WebSocket_OnError(object sender, ErrorEventArgs e)
    {
        if (OnError != null)
            OnError(this, e.Message);
    }

    private void WebSocket_OnClose(object sender, CloseEventArgs e)
    {
        if (OnClose != null)
            OnClose(this, e.Code, e.Reason);
    }
#endif

    public void SendAsync(byte[] packet)
    {
#if DOT_NET
#elif UNITY_WEBGL  && !UNITY_EDITOR
    	NativeWebSocket.SendAsync(packet);
#else
        m_webSocket.SendAsync(packet, null);
#endif
    }

    public delegate void OnOpenHandler(BrainCloudWebSocket accepted);
    public delegate void OnMessageHandler(BrainCloudWebSocket sender, byte[] data);
    public delegate void OnErrorHandler(BrainCloudWebSocket sender, string message);
    public delegate void OnCloseHandler(BrainCloudWebSocket sender, int code, string reason);

    public event OnOpenHandler OnOpen;
    public event OnMessageHandler OnMessage;
    public event OnErrorHandler OnError;
    public event OnCloseHandler OnClose;
}