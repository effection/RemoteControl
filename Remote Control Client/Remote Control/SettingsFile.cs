using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using System.IO;
using System.Collections.Generic;
using System.IO.IsolatedStorage;

namespace Raspberry_Pi
{
    public class SettingsFile
    {
        public static SettingsFile Default { get; private set; }

        /// <summary>
        /// Loads this connection setting on start up.
        /// </summary>
        public string DefaultConnectionSetting { get; set; }

        /// <summary>
        /// Grouped settings.
        /// </summary>
        public List<ConnectionSetting> Settings { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SettingsFile()
        {
            Settings = new List<ConnectionSetting>();
        }

        static SettingsFile()
        {
            //Default settings
            var obj = new SettingsFile();
            obj.DefaultConnectionSetting = "Example";
            obj.Settings.Add(new ConnectionSetting
            {
                Name = "Example",
                UseCommonServer = new ConnectionSetting_UseCommonServer { Id = "000000000", Value = true },
                UseCommonConnection = new ConnectionSetting_UseCommonConnection { Protocol = Protocol.Udp, Port = 113001, Value = true },
                Services = new List<Service> 
                    { 
                        new Service { Name = "MouseService", Protocol = Protocol.Udp, Port = 113001, Server = null }
                    }
            });
            Default = obj;
        }

        /// <summary>
        /// Load settings.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static bool Load(IsolatedStorageFileStream reader)
        {
            SettingsFile file = null;
            TextReader r = new StreamReader(reader);
            try
            {
                XmlSerializer xml = new XmlSerializer(typeof(SettingsFile));
                file = (SettingsFile)xml.Deserialize(r);
            }
            catch
            {
                return false;
            }
            finally
            {
                r.Close();
            }
            Default = file;
            return true;
        }

        /// <summary>
        /// Save settings.
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        public bool Save(IsolatedStorageFileStream writer)
        {
            TextWriter w = new StreamWriter(writer);
            try
            {
                XmlSerializer xml = new XmlSerializer(typeof(SettingsFile));
                xml.Serialize(w, this);
            }
            catch
            {
                return false;
            }
            finally
            {
                w.Close();
            }

            return true;
        }
    }

    public class ConnectionSetting
    {
        /// <summary>
        /// Settings display name.
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// Use common server address between all services.
        /// </summary>
        public ConnectionSetting_UseCommonServer UseCommonServer { get; set; }

        /// <summary>
        /// Use common connection between all services.
        /// </summary>
        public ConnectionSetting_UseCommonConnection UseCommonConnection { get; set; }

        /// <summary>
        /// List of services.
        /// </summary>
        public List<Service> Services { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ConnectionSetting()
        {
            Services = new List<Service>();
        }
    }

    public class Service
    {
        /// <summary>
        /// Display name.
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// Type of service.
        /// </summary>
        [XmlAttribute]
        public ServiceType Type { get; set; }

        /// <summary>
        /// Network protocol used.
        /// </summary>
        public Protocol Protocol { get; set; }

        /// <summary>
        /// Port to connect on.
        /// </summary>
        public uint Port { get; set; }

        /// <summary>
        /// Optional: Service MAC address used to connect if seperate servers are used (must be paired).
        /// </summary>
        public string Server { get; set; }
    }

    public class ConnectionSetting_UseCommonServer
    {
        /// <summary>
        /// Mac address of server.
        /// </summary>
        [XmlAttribute]
        public string Id { get; set; }

        /// <summary>
        /// True or false to use the common server address.
        /// </summary>
        [XmlTextAttribute]
        public bool Value { get; set; }
    }

    public class ConnectionSetting_UseCommonConnection
    {
        /// <summary>
        /// Network protocol.
        /// </summary>
        [XmlAttribute]
        public Protocol Protocol { get; set; }

        /// <summary>
        /// Network port.
        /// </summary>
        [XmlAttribute]
        public uint Port { get; set; }

        /// <summary>
        /// True or false to use common connection between all services.
        /// </summary>
        [XmlTextAttribute]
        public bool Value { get; set; }
    }

    public enum ServiceType
    {
        /// <summary>
        /// Mouse device.
        /// </summary>
        Mouse,

        /// <summary>
        /// Keyboard input.
        /// </summary>
        Keyboard,

        /// <summary>
        /// Tv remote.
        /// </summary>
        Tv,

        /// <summary>
        /// Media player remote.
        /// </summary>
        Media,

        /// <summary>
        /// PS3 controller.
        /// </summary>
        PS3,

        /// <summary>
        /// XBOX360 controller
        /// </summary>
        Xbox360,

        /// <summary>
        /// House appliances controller.
        /// </summary>
        House
    }

    public enum Protocol
    {
        /// <summary>
        /// UDP.
        /// </summary>
        Udp,

        /// <summary>
        /// TCP.
        /// </summary>
        Tcp,

        /// <summary>
        /// UDP Multicast.
        /// </summary>
        UdpMulticast
    }

}
