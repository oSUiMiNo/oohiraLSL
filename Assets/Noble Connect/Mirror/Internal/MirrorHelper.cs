using System;
using System.Net;
using System.Reflection;
using Mirror;
#if LITENETLIB_TRANSPORT
using LiteNetLib;
#endif

namespace NobleConnect.Mirror
{
    public class MirrorHelper
    {
        const string TRANSPORT_WARNING_MESSAGE = "You must use a transport that supports UDP in order to use Mirror with NobleConnect.\n I recommend the default KcpTransport.";

        public static Transport GetTransport()
        {
            var transport = Transport.active;
            var transportType = transport.GetType();
            if (transportType == typeof(LatencySimulation))
            {
                transport = (transport as LatencySimulation).wrap;
            }

            return transport;
        }

        public static Type GetTransportType()
        {
            var transport = GetTransport();
            var transportType = transport.GetType();
            return transportType;
        }

        public static void SetTransportPort(ushort port)
        {
            var transport = GetTransport();
            var transportType = GetTransportType();
#if LITENETLIB_TRANSPORT
            if (transportType == typeof(LiteNetLibTransport))
            {
                var liteNet = (LiteNetLibTransport)transport;
                liteNet.port = (ushort)port;
            }
#endif
#if IGNORANCE
            if (transportType.IsSubclassOf(typeof(IgnoranceTransport.Ignorance)) || transportType == typeof(IgnoranceTransport.Ignorance))
            {
                var ignorance = (IgnoranceTransport.Ignorance)transport;
                ignorance.port = port;
            }
#endif
            if (transportType.IsSubclassOf(typeof(kcp2k.KcpTransport)) || transportType == typeof(kcp2k.KcpTransport))
            {
                var ignorance = (kcp2k.KcpTransport)transport;
                ignorance.Port = port;
            }
        }

        public static bool HasUDPTransport()
        {
            var transportType = GetTransportType();
#if LITENETLIB_TRANSPORT
            if (transportType == typeof(LiteNetLibTransport))
            {
                return true;
            }
#endif
#if IGNORANCE
            if (transportType.IsSubclassOf(typeof(IgnoranceTransport.Ignorance)) || transportType == typeof(IgnoranceTransport.Ignorance))
            {
                return true;
            }
#endif
            if (transportType.IsSubclassOf(typeof(kcp2k.KcpTransport)) || transportType == typeof(kcp2k.KcpTransport))
            {
                return true;
            }

            return false;
        }

        public static ushort GetTransportPort()
        {
            var transport = GetTransport();
            var transportType = GetTransportType();
#if LITENETLIB_TRANSPORT
            if (transportType == typeof(LiteNetLibTransport))
            {
                var liteNet = (LiteNetLibTransport)transport;
                return liteNet.port;
            }
#endif
#if IGNORANCE
            if (transportType.IsSubclassOf(typeof(IgnoranceTransport.Ignorance)) || transportType == typeof(IgnoranceTransport.Ignorance))
            {
                var ignorance = (IgnoranceTransport.Ignorance)transport;
                return ignorance.port;
            }
#endif
            if (transportType.IsSubclassOf(typeof(kcp2k.KcpTransport)) || transportType == typeof(kcp2k.KcpTransport))
            {
                var kcp = (kcp2k.KcpTransport)transport;
                return kcp.Port;
            }

            throw new Exception(TRANSPORT_WARNING_MESSAGE);
        }

        public static IPEndPoint GetClientEndPoint(NetworkConnection conn)
        {
            var transport = GetTransport();
            var transportType = GetTransportType();
#if LITENETLIB_TRANSPORT
            if (transportType == typeof(LiteNetLibTransport))
            {
                var liteNet = (LiteNetLibTransport)transport;
                return liteNet.ServerGetClientIPEndPoint(conn.connectionId);
            }
#endif
#if IGNORANCE
            if (transportType.IsSubclassOf(typeof(IgnoranceTransport.Ignorance)) || 
                transportType == typeof(IgnoranceTransport.Ignorance))
            {
                var ignorance = ((IgnoranceTransport.Ignorance)transport);
                var connectionLookupDictField = typeof(IgnoranceTransport.Ignorance).GetField("ConnectionLookupDict", BindingFlags.NonPublic | BindingFlags.Instance);
                var connectionLookupDict = (Dictionary<int, IgnoranceCore.PeerConnectionData>)connectionLookupDictField.GetValue(ignorance);
                IgnoranceCore.PeerConnectionData result;
                if (connectionLookupDict.TryGetValue(conn.connectionId, out result))
                {
                    return new IPEndPoint(IPAddress.Parse(result.IP), result.Port);
                }
            }
#endif
            if (transportType.IsSubclassOf(typeof(kcp2k.KcpTransport)) ||
                transportType == typeof(kcp2k.KcpTransport))
            {
                var kcp = ((kcp2k.KcpTransport)transport);
                var kcpServerField = typeof(kcp2k.KcpTransport).GetField("server", BindingFlags.NonPublic | BindingFlags.Instance);
                var kcpServer = (kcp2k.KcpServer)kcpServerField.GetValue(kcp);
                if (kcpServer.connections.TryGetValue(conn.connectionId, out kcp2k.KcpServerConnection result))
                {
                   return (IPEndPoint)result.remoteEndPoint;
                }
            }

            throw new Exception(TRANSPORT_WARNING_MESSAGE);
        }
    }
}