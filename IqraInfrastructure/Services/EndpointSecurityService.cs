using IqraCore.Entities.Business;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace IqraInfrastructure.Services
{
    public class EndpointSecurityService
    {
        private readonly ILogger<EndpointSecurityService> _logger;
        private readonly HashSet<IPAddress> _localMachineIPs = new();
        private List<EndpointBlacklistEntry> _blacklistEntries = new();
        private readonly object _blacklistLock = new();

        public EndpointSecurityService(ILogger<EndpointSecurityService> logger)
        {
            _logger = logger;
            DetectLocalNetworkIPs();
        }

        /// <summary>
        /// Auto-detect all local machine IP addresses on startup.
        /// </summary>
        private void DetectLocalNetworkIPs()
        {
            try
            {
                // Add loopback
                _localMachineIPs.Add(IPAddress.Loopback);       // 127.0.0.1
                _localMachineIPs.Add(IPAddress.IPv6Loopback);   // ::1

                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.OperationalStatus != OperationalStatus.Up)
                        continue;

                    var ipProperties = networkInterface.GetIPProperties();
                    foreach (var unicastAddress in ipProperties.UnicastAddresses)
                    {
                        _localMachineIPs.Add(unicastAddress.Address);
                    }
                }

                _logger.LogInformation("EndpointSecurityService detected {Count} local IP addresses.", _localMachineIPs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting local network IPs for endpoint security.");
            }
        }

        /// <summary>
        /// Refresh the blacklist from the provided entries (called from repository/admin updates).
        /// </summary>
        public void RefreshBlacklist(List<EndpointBlacklistEntry> entries)
        {
            lock (_blacklistLock)
            {
                _blacklistEntries = entries.Where(e => e.IsActive).ToList();
            }
            _logger.LogInformation("EndpointSecurityService refreshed blacklist with {Count} active entries.", _blacklistEntries.Count);
        }

        /// <summary>
        /// Checks whether a target URL is allowed to be called.
        /// Returns true if the URL is safe, false if blocked.
        /// </summary>
        public async Task<(bool isAllowed, string? reason)> IsEndpointAllowedAsync(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    return (false, "URL is empty or null.");

                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                    return (false, $"Invalid URL format: {url}");

                // Check scheme - only allow http and https
                if (uri.Scheme != "http" && uri.Scheme != "https")
                    return (false, $"Unsupported URL scheme: {uri.Scheme}. Only HTTP and HTTPS are allowed.");

                string hostname = uri.Host;

                // Check 1: Block common localhost patterns
                if (IsLocalhostHostname(hostname))
                    return (false, $"Requests to localhost ({hostname}) are not allowed.");

                // Check 2: Resolve DNS and validate resolved IPs
                IPAddress[] resolvedAddresses;
                try
                {
                    resolvedAddresses = await Dns.GetHostAddressesAsync(hostname);
                }
                catch (SocketException)
                {
                    return (false, $"Unable to resolve hostname: {hostname}");
                }

                foreach (var address in resolvedAddresses)
                {
                    // Check against private/reserved IP ranges
                    if (IsPrivateOrReservedIP(address))
                        return (false, $"Endpoint resolves to a private/reserved IP address ({address}). Requests to internal networks are not allowed.");

                    // Check against detected local machine IPs
                    if (_localMachineIPs.Contains(address))
                        return (false, $"Endpoint resolves to a local machine IP ({address}). Requests to the local server are not allowed.");
                }

                // Check 3: Check against DB-managed blacklist
                List<EndpointBlacklistEntry> currentBlacklist;
                lock (_blacklistLock)
                {
                    currentBlacklist = _blacklistEntries.ToList();
                }

                foreach (var entry in currentBlacklist)
                {
                    if (MatchesBlacklistEntry(url, hostname, resolvedAddresses, entry))
                        return (false, $"Endpoint is blocked by security policy: {entry.Reason}");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking endpoint security for URL: {Url}", url);
                return (false, $"Error validating endpoint security: {ex.Message}");
            }
        }

        private static bool IsLocalhostHostname(string hostname)
        {
            return hostname.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                   hostname.Equals("127.0.0.1", StringComparison.Ordinal) ||
                   hostname.Equals("::1", StringComparison.Ordinal) ||
                   hostname.Equals("[::1]", StringComparison.Ordinal) ||
                   hostname.Equals("0.0.0.0", StringComparison.Ordinal);
        }

        private static bool IsPrivateOrReservedIP(IPAddress address)
        {
            byte[] bytes = address.GetAddressBytes();

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                // 10.0.0.0/8
                if (bytes[0] == 10) return true;
                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                // 127.0.0.0/8 (loopback)
                if (bytes[0] == 127) return true;
                // 169.254.0.0/16 (link-local)
                if (bytes[0] == 169 && bytes[1] == 254) return true;
                // 0.0.0.0
                if (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 0) return true;
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // ::1 (loopback)
                if (IPAddress.IsLoopback(address)) return true;
                // fe80::/10 (link-local)
                if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80) return true;
                // fc00::/7 (unique local)
                if ((bytes[0] & 0xFE) == 0xFC) return true;
            }

            return false;
        }

        private static bool MatchesBlacklistEntry(string fullUrl, string hostname, IPAddress[] resolvedAddresses, EndpointBlacklistEntry entry)
        {
            switch (entry.PatternType)
            {
                case EndpointBlacklistPatternType.Exact:
                    return fullUrl.Equals(entry.Pattern, StringComparison.OrdinalIgnoreCase) ||
                           hostname.Equals(entry.Pattern, StringComparison.OrdinalIgnoreCase);

                case EndpointBlacklistPatternType.Wildcard:
                    string regexPattern = "^" + Regex.Escape(entry.Pattern).Replace("\\*", ".*") + "$";
                    return Regex.IsMatch(hostname, regexPattern, RegexOptions.IgnoreCase) ||
                           Regex.IsMatch(fullUrl, regexPattern, RegexOptions.IgnoreCase);

                case EndpointBlacklistPatternType.CIDR:
                    return resolvedAddresses.Any(addr => IsInCIDRRange(addr, entry.Pattern));

                default:
                    return false;
            }
        }

        private static bool IsInCIDRRange(IPAddress address, string cidr)
        {
            try
            {
                var parts = cidr.Split('/');
                if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var networkAddress) || !int.TryParse(parts[1], out int prefixLength))
                    return false;

                if (address.AddressFamily != networkAddress.AddressFamily)
                    return false;

                byte[] addressBytes = address.GetAddressBytes();
                byte[] networkBytes = networkAddress.GetAddressBytes();

                int fullBytes = prefixLength / 8;
                int remainingBits = prefixLength % 8;

                for (int i = 0; i < fullBytes; i++)
                {
                    if (addressBytes[i] != networkBytes[i]) return false;
                }

                if (remainingBits > 0 && fullBytes < addressBytes.Length)
                {
                    int mask = 0xFF << (8 - remainingBits);
                    if ((addressBytes[fullBytes] & mask) != (networkBytes[fullBytes] & mask))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
