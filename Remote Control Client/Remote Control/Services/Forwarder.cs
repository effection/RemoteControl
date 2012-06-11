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
using GalaSoft.MvvmLight.Ioc;
using System.Collections.Generic;
using System.Text;

namespace Raspberry_Pi.Services
{
    public static class Forwarder
    {
        #region Properties

        /// <summary>
        /// If true all services will use the same connection.
        /// </summary>
        public static bool UseCommonConnection { get; set; }

        /// <summary>
        /// This connection will be used for all services if UseCommonConnection is true;
        /// </summary>
        public static Network.IConnection CommonConnection { get; set; }

        /// <summary>
        /// If true all connections connect to the same address.
        /// </summary>
        /// <remarks>Can still use seperate protocols or ports.</remarks>
        public static bool UseSameServer { get; set; }

        /// <summary>
        /// Address of server.
        /// </summary>
        public static string CommonServerAddress { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// Forwarding service instances.
        /// </summary>
        private static Dictionary<Type, object> services = new Dictionary<Type, object>();

        /// <summary>
        /// Connection associated with each service.
        /// </summary>
        private static Dictionary<Type, Network.IConnection> connections = new Dictionary<Type, Network.IConnection>();

        #endregion

        static Forwarder()
        {
            //Add connection service
            var service = new ConnectionService();
            service.ServerSelected += new EventHandler<ServerSelectedEventArgs>(ConnectionService_ServerSelected);

            RegisterService<ConnectionService>(service, null);
        }

        #region Services

        /// <summary>
        /// Register new forwarding service.
        /// </summary>
        /// <typeparam name="T">Type of service.</typeparam>
        /// <param name="instance">Service instance.</param>
        /// <param name="connection">Optional service specific connection tied with UseCommonConnection.</param>
        public static void RegisterService<T>(T instance, Network.IConnection connection = null)
        {
            if (!services.ContainsKey(typeof(T)))
            {
                services.Add(typeof(T), instance);
                if(connection != null)
                    connections.Add(typeof(T), connection);
            }
        }

        /// <summary>
        /// Remove a service and connection information.
        /// </summary>
        /// <typeparam name="T">Type of service to remove.</typeparam>
        public static void RemoveService<T>()
        {
            services.Remove(typeof(T));
            connections.Remove(typeof(T));
        }

        /// <summary>
        /// Get a forwarding service of type T.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <returns>Service instance.</returns>
        public static T GetService<T>()
        {
            object service = null;
            services.TryGetValue(typeof(T), out service);
            return (T)service;
        }

        /// <summary>
        /// Get the connection for service of type T.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <returns>Service or null.</returns>
        public static Network.IConnection GetConnectionForService<T>()
        {
            if (UseCommonConnection)
                return CommonConnection;

            Network.IConnection connection = null;
            connections.TryGetValue(typeof(T), out connection);
            return connection;
        }

        /// <summary>
        /// Update connection information for service of type T.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="connection">New connection.</param>
        public static void SetConnectionForService<T>(Network.IConnection connection)
        {
            if (connections.ContainsKey(typeof(T)))
                connections[typeof(T)] = connection;
        }

        #endregion

        /// <summary>
        /// Restart all services except ConnectionService to to take into account the new server address.
        /// </summary>
        /// /// <param name="callback">Result callback.</param>
        public static void RestartServices(Action<Type, bool> callback)
        {
            if (UseSameServer)
            {
                //Update connection address
                foreach (var connection in connections)
                {
                    connection.Value.Close();
                    connection.Value.Address = CommonServerAddress;
                }
                //Reopen all connections with new server address
                OpenConnections(callback);
            }
            else
            {
                CloseConnections();
                OpenConnections(callback);
            }
        }

        /// <summary>
        /// Open all connections for services.
        /// </summary>
        /// <param name="callback">Result callback.</param>
        public static void OpenConnections(Action<Type, bool> callback)
        {
            if (UseCommonConnection)
            {
                if (CommonConnection != null)
                {
                    CommonConnection.Open((success) =>
                        {
                            callback(null, success);
                        });
                }
                else
                    callback(null, false);
            }
            else
            {
                foreach (var connection in connections)
                {
                    if (connection.Key == typeof(ConnectionService))
                        continue;

                    connection.Value.Open((success) =>
                        {
                            callback(connection.Key, success);
                        });
                }
            }
        }

        /// <summary>
        /// Close all connections for services.
        /// </summary>
        public static void CloseConnections()
        {
            if (UseCommonConnection)
            {
                if (CommonConnection != null)
                    CommonConnection.Close();
            }
            else
            {
                foreach (var connection in connections)
                {
                    if (connection.Key == typeof(ConnectionService))
                        continue;
                    connection.Value.Close();
                }
            }

        }

        /// <summary>
        /// Service picked that we should communicate with.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnectionService_ServerSelected(object sender, ServerSelectedEventArgs e)
        {
            CommonServerAddress = e.Address;
        }
    }
}
