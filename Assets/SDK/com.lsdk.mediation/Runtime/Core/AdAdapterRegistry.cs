using System;
using System.Collections.Generic;

namespace LSDK.Mediation
{
    /// <summary>
    /// Core không biết gì về SDK của từng network. Mỗi package network tự đăng ký factory của nó
    /// (qua RuntimeInitializeOnLoadMethod), MediationManager chỉ tạo adapter theo MediationConfig.
    /// </summary>
    public static class AdAdapterRegistry
    {
        private static readonly Dictionary<AdNetworkType, Func<IAdNetworkAdapter>> Factories =
            new Dictionary<AdNetworkType, Func<IAdNetworkAdapter>>();

        public static void Register(AdNetworkType network, Func<IAdNetworkAdapter> factory)
        {
            Factories[network] = factory;
        }

        public static bool IsRegistered(AdNetworkType network) => Factories.ContainsKey(network);

        public static IAdNetworkAdapter Create(AdNetworkType network)
        {
            return Factories.TryGetValue(network, out var factory) ? factory() : null;
        }
    }
}
