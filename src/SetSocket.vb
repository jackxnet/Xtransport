Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading.Channels
Imports System.Threading.Thread
Imports xPxP.xNetPacket
Imports xPxP.ENUMS
Imports System.Threading

Public Class SetSocket
    Private mysocket As UdpClient
    Private localip As String
    Private localport As Integer
    Private startport As Integer
    Private stopport As Integer
    Private newport = 30000
    Private xtransport As Transporter
    Private ToOtherHostnodes As New List(Of nodeclient)
    Public routingservice = False
    Private waitt = 25
    Private pburst = 10
    Public Event dataready(b As Byte())
    Public Event otherdataready(b As Byte())
    Public hostservice = False
    ' Public debugon = False
    Public MYHOSTCODE As Integer

    Private xclient As nodeclient
    Private usingipv6 = False
    Public xtransporters As New List(Of Transporter)
    Public cts As Boolean = False


    Public Sub setmtu(m)
        mtu = m
    End Sub


    Public Sub settoipv6()
        usingipv6 = True

    End Sub

    Public Sub setdebug(onoroff)
        debugon = onoroff
    End Sub

    Public Function getquesize()
        Return xtransport.sendframebuffer.Count
    End Function


    Public Sub setspeed(wt As Integer, pb As Integer)
        waitt = wt
        pburst = pb

    End Sub

    Public Sub setportrange(pstart, pstop)
        startport = pstart
        stopport = pstop
        newport = startport
    End Sub

    'Private Sub host_service()
    '    While canceltoken = False
    '        For Each node In xtransporters
    '            While Not node.data_ready.IsEmpty
    '                Dim b As Byte() = Nothing
    '                Dim r = node.data_ready.TryDequeue(b)
    '                If r = True Then
    '                    'RaiseEvent dataready(b)
    '                    GQues.hostdatain.Enqueue(b)
    '                End If
    '            End While

    '        Next
    '        Sleep(10)
    '    End While

    'End Sub


    Public Sub routeunicast(localid, b)
        For Each node In xtransporters
            If node.nodeid = localid Then
                node.send_que(b)
                Exit For
            End If
        Next
    End Sub

    Public Sub routemulticast(callerid, b)
        For Each node In xtransporters
            If node.hostport <> callerid Then
                node.send_que(b)
            End If
        Next
    End Sub

    Public Sub routechannel(callerid, channelid, b)
        For Each node In xtransporters
            If node.nodeid <> callerid And node.channel = channelid Then
                node.send_que(b)
            End If
        Next
    End Sub

    Public Sub routeport(port, b)
        For Each node In xtransporters
            If node.xport = port Then
                node.send_que(b)
            End If
        Next
    End Sub

    Public Sub addtraffic(b As Byte())
        For Each node In xtransporters
            node.send_que(b)
        Next
    End Sub


    Public Sub sendchannel(b As Byte(), c As Integer, mode As TRANSPORT_MODE)


        For Each node In xtransporters
            If cts = True Then Exit Sub
            If debugon = 1 Then Console.WriteLine($"working node {node.hostport} of total nodes {xtransporters.Count}")

            If node.channel = c Then
                If mode = TRANSPORT_MODE.REALTIME Then
                    Try
                        If node.sendframebuffer.IsEmpty Then node.send_que(b)
                    Catch ex As Exception
                        Debug.WriteLine("Send error")
                    End Try
                Else
                    'Debug.Print($"Current Queue is {node.sendframebuffer.Count}")
                    Try
                        node.send_que(b)
                    Catch ex As Exception
                        Debug.WriteLine("Send error")
                    End Try
                End If

            End If

        Next

    End Sub




    Public Sub InitializeReceiver(ByVal lip As String, ByVal lport As Integer)
        If usingipv6 Then
            mysocket = New UdpClient(AddressFamily.InterNetworkV6)
        Else
            mysocket = New UdpClient
        End If
        ' mysocket.ExclusiveAddressUse = False
        localip = lip
        localport = lport
        Dim lendPoint As IPEndPoint = New IPEndPoint(IPAddress.Parse(lip), lport)
        mysocket.Client.Bind(lendPoint)

        'If hostservice = True Then Task.Run(Sub() host_service())
        Task.Run(Sub() mysocket.BeginReceive(New AsyncCallback(AddressOf Receive), mysocket))

    End Sub



    Public Sub Receive(ByVal ar As IAsyncResult)
        Dim udpClient = TryCast(ar.AsyncState, UdpClient)
        Try
            Dim endPoint As IPEndPoint = Nothing
            Dim data = udpClient.EndReceive(ar, endPoint)
            Dim message = Encoding.ASCII.GetString(data)
            Dim messages = Split(message, ":")
            Console.WriteLine(message)
            Dim nodeid = messages(0)
            Dim channelid = messages(1)
            Dim token = messages(2)
            If token = "REQUESTPORT" Then

                xtransport = New Transporter
                xtransport.setspeed(waitt, pburst)
                xtransport.isanode = False
                If usingipv6 = True Then xtransport.usingipv6 = True
                xtransport.Set_Factory_Ports(newport, IPAddress.Parse(localip))
                xtransport.hostport = newport
                xtransport.nodeid = nodeid
                xtransport.channel = channelid
                If messages.Count = 4 Then xtransport.isotherhost = messages(3)

                Console.WriteLine("Starting DXP Service...." & nodeid & " on Port .. " & newport & " On channel " & channelid & " IpAddy " & localip)
                xtransport.Service_Start(usingipv6)
                xtransporters.Add(xtransport)
                message = newport.ToString
                data = Encoding.UTF8.GetBytes(message)
                Task.Run(Sub() udpClient.Send(data, data.Length, endPoint))
                newport += 1
                If newport > stopport Then newport = startport

            End If
        Catch ex As Exception
        Finally
            Task.Run(Sub() UdpClient.BeginReceive(AddressOf Receive, UdpClient))
        End Try

    End Sub


    Public Sub checkfordead()
        Dim ts As TimeSpan
        Try
            For t = xtransporters.Count - 1 To 0 Step -1
                ts = (Now() - xtransporters(t).returnlastseen)
                If ts.Seconds > 15 Then
                    Console.WriteLine($"Killing xTransport ID Number .. { xtransporters(t).nodeid} at Port { xtransporters(t).hostport} on Channel { xtransporters(t).channel}")
                    xtransporters(t).service_stop()
                    xtransporters(t) = Nothing
                    xtransporters.RemoveAt(t)

                End If
            Next
            'GC.Collect()
        Catch ex As Exception
            Console.WriteLine($"CheckforDead Broke {ex.Message}")
        End Try


    End Sub



End Class

