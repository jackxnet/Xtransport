Imports System.Net
Imports System.Net.NetworkInformation

Public Class IPV6NICHelper

    Public Function ShowNetworkInterfaces() As IPAddress
        Dim ipv6addy As IPAddress
        Dim computerProperties As IPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties()
        Dim nics As NetworkInterface() = NetworkInterface.GetAllNetworkInterfaces()
        Console.WriteLine("Interface information for {0}.{1}     ", computerProperties.HostName, computerProperties.DomainName)

        If nics Is Nothing OrElse nics.Length < 1 Then
            Console.WriteLine("  No network interfaces found.")
            Return Nothing
        End If

        Console.WriteLine("  Number of interfaces .................... : {0}", nics.Length)

        For Each adapter As NetworkInterface In nics
            Dim properties As IPInterfaceProperties = adapter.GetIPProperties()
            Console.WriteLine("")
            Console.WriteLine(adapter.Description)
            Console.WriteLine(String.Empty.PadLeft(adapter.Description.Length, "="c))
            Console.WriteLine("  Interface type .......................... : {0}", adapter.NetworkInterfaceType)
            Console.WriteLine("  Physical Address ........................ : {0}", adapter.GetPhysicalAddress().ToString())
            Console.WriteLine("  Operational status ...................... : {0}", adapter.OperationalStatus)
            Dim versions As String = ""


            If adapter.Supports(NetworkInterfaceComponent.IPv4) Then
                versions = "IPv4"
            End If

            If adapter.Supports(NetworkInterfaceComponent.IPv6) Then

                If versions.Length > 0 Then
                    versions += " "
                End If

                versions += "IPv6"
            End If

            Console.WriteLine("  IP version .............................. : {0}", versions)
            ipv6addy = GETIPAddresses(properties)
            If ipv6addy IsNot Nothing Then
                Return ipv6addy
                Exit Function
            End If


            If adapter.NetworkInterfaceType = NetworkInterfaceType.Loopback Then
                Continue For
            End If

            Console.WriteLine("  DNS suffix .............................. : {0}", properties.DnsSuffix)
            'Dim label As String

            If adapter.Supports(NetworkInterfaceComponent.IPv4) Then
                Dim ipv4 As IPv4InterfaceProperties = properties.GetIPv4Properties()
                Console.WriteLine("  MTU...................................... : {0}", ipv4.Mtu)

                'If ipv4.UsesWins Then
                'Dim winsServers As IPAddressCollection = properties.WinsServersAddresses

                'If winsServers.Count > 0 Then
                'label = "  WINS Servers ............................ :"
                ''ShowIPAddresses(label, winsServers)
                'End If
                'End If
            End If

            'Console.WriteLine("  DNS enabled ............................. : {0}", properties.IsDnsEnabled)
            'Console.WriteLine("  Dynamically configured DNS .............. : {0}", properties.IsDynamicDnsEnabled)
            Console.WriteLine("  Receive Only ............................ : {0}", adapter.IsReceiveOnly)
            Console.WriteLine("  Multicast ............................... : {0}", adapter.SupportsMulticast)
            'ShowInterfaceStatistics(adapter)
            Console.WriteLine("")
        Next
        Return ipv6addy
    End Function


    Public Function GETIPAddresses(ByVal adapterProperties As IPInterfaceProperties) As IPAddress
        Dim dnsServers As IPAddressCollection = adapterProperties.DnsAddresses
        Dim ipv6addy As IPAddress = Nothing
        If dnsServers IsNot Nothing Then

            For Each dns As IPAddress In dnsServers
                Console.WriteLine("  DNS Servers ............................. : {0}", dns.ToString())
            Next
        End If

        'Dim anyCast As IPAddressInformationCollection = adapterProperties.AnycastAddresses

        'If anyCast IsNot Nothing Then

        '    For Each any As IPAddressInformation In anyCast
        '        Console.WriteLine("  Anycast Address .......................... : {0} {1} {2}", any.Address, If(any.IsTransient, "Transient", ""), If(any.IsDnsEligible, "DNS Eligible", ""))
        '    Next

        '    Console.WriteLine("")
        'End If

        'Dim multiCast As MulticastIPAddressInformationCollection = adapterProperties.MulticastAddresses

        'If multiCast IsNot Nothing Then

        '    For Each multi As IPAddressInformation In multiCast
        '        Console.WriteLine("  Multicast Address ....................... : {0} {1} {2}", multi.Address, If(multi.IsTransient, "Transient", ""), If(multi.IsDnsEligible, "DNS Eligible", ""))
        '    Next

        '    Console.WriteLine("")
        'End If

        Dim uniCast As UnicastIPAddressInformationCollection = adapterProperties.UnicastAddresses

        If uniCast IsNot Nothing Then
            Dim lifeTimeFormat As String = "dddd, MMMM dd, yyyy  hh:mm:ss tt"

            For Each uni As UnicastIPAddressInformation In uniCast
                Dim [when] As DateTime
                Console.WriteLine("  Unicast Address ......................... : {0}", uni.Address)
                'Console.WriteLine("     Prefix Origin ........................ : {0}", uni.PrefixOrigin)
                'Console.WriteLine("     Suffix Origin ........................ : {0}", uni.SuffixOrigin)
                'Console.WriteLine("     Duplicate Address Detection .......... : {0}", uni.DuplicateAddressDetectionState)

                'If uni.SuffixOrigin = SuffixOrigin.LinkLayerAddress And uni.PrefixOrigin = PrefixOrigin.RouterAdvertisement Then
                Dim c As Char = ":"
                Dim n As Integer = 0
                If uni.Address.AddressFamily = Sockets.AddressFamily.InterNetworkV6 Then

                    For Each c In uni.Address.ToString
                        n += 1
                        '   Console.WriteLine($"Number of colons is {n}")
                    Next
                    If n = 38 Then
                        Console.WriteLine(" Usable IP Address Detection .......... : {0}", uni.Address)
                        Return uni.Address
                        Exit Function
                    End If

                End If

                'End If

                '[when] = DateTime.UtcNow + TimeSpan.FromSeconds(uni.AddressValidLifetime)
                '[when] = [when].ToLocalTime()
                'Console.WriteLine("     Valid Life Time ...................... : {0}", [when].ToString(lifeTimeFormat, System.Globalization.CultureInfo.CurrentCulture))
                '[when] = DateTime.UtcNow + TimeSpan.FromSeconds(uni.AddressPreferredLifetime)
                '[when] = [when].ToLocalTime()
                'Console.WriteLine("     Preferred life time .................. : {0}", [when].ToString(lifeTimeFormat, System.Globalization.CultureInfo.CurrentCulture))
                '[when] = DateTime.UtcNow + TimeSpan.FromSeconds(uni.DhcpLeaseLifetime)
                '[when] = [when].ToLocalTime()
                Console.WriteLine("     DHCP Leased Life Time ................ : {0}", [when].ToString(lifeTimeFormat, System.Globalization.CultureInfo.CurrentCulture))
            Next

            Console.WriteLine("")
        End If
        If ipv6addy IsNot Nothing Then
            Return ipv6addy
        Else
            Return Nothing
        End If

    End Function












End Class
