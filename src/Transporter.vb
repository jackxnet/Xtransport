Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Concurrent
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading.Thread
Imports System.Threading
Imports System.Timers
Imports System.Diagnostics
Imports xPxP.ENUMS

Public Class Transporter : Implements IDisposable

    Public xip As IPAddress
    Public xport As Integer
    Public rendPoint As IPEndPoint
    'Public rendpoint As New IPEndPoint(xserveraddress.AddressFamily, 0)
    Public lendPoint As IPEndPoint


    Dim packettype As Integer
    'Public Event receive_frame(b As Byte())
    Private waittime = 50
    Private fragstosend = 100
    Public lastseen As DateTime = Nothing
    'Public receiveip As IPAddress = IPAddress.Parse("192.168.0.175")
    Public xsock As UdpClient
    Public sendframebuffer As New ConcurrentQueue(Of List(Of Byte()))
    Public currentframe As Integer = 0
    Private sender As SendFrame

    'watchdog stuff
    Dim porttimerDelegate As TimerCallback = AddressOf sendping
    Private porttimer As Threading.Timer
    Private ping = Encoding.UTF8.GetBytes("PING")
    Public pingdelay As Integer = 1000
    Private dframe As New data_frame
    Private currentRframe As Integer = 0

    Public hostid As Integer
    Public localname As String = "Defaultname"
    Public nodeid As Integer
    Public channel As UInteger
    Public hostport As Integer
    Public isanode As Boolean = True
    Public isotherhost As Boolean = False
    Public usingipv6 As Boolean = False
    Public broadcast As Boolean = False

    Private ff As FragFrame
    Private singlelastseen = 0
    Private setid = False
    Public cts As Boolean = False
    Public trafficshaper = False
    Public trafficdelay = 0




    Public Function returnlastseen() As DateTime
        Return lastseen
    End Function

    'Private Sub recframe(b As Byte())
    'RaiseEvent dataready(b)
    ' End Sub


    Public Sub setspeed(wt As Integer, pb As Integer)
        waittime = wt
        fragstosend = pb

    End Sub


    Public Sub setspeed(wt As Integer, pb As Integer, trfcdelay As Integer)
        waittime = wt
        fragstosend = pb
        trafficshaper = True
        trafficdelay = trfcdelay

    End Sub

    Public Sub send_que(b As Byte())
        sendframebuffer.Enqueue(ff.frag(b))
    End Sub




    Public Sub Set_Factory_localPorts(localport As Integer, localip As IPAddress, maxtransunit As Integer)
        lendPoint = New IPEndPoint(localip, localport)
        If usingipv6 Then
            rendPoint = New IPEndPoint(IPAddress.IPv6Any, 0)
        Else
            rendPoint = New IPEndPoint(IPAddress.Any, 0)
        End If
        '
        xip = localip
        xport = localport
        mtu = maxtransunit
    End Sub


    Public Sub Set_Factory_Ports(remoteport As Integer, remoteip As IPAddress, maxtransunit As Integer)
        If usingipv6 Then
            lendPoint = New IPEndPoint(IPAddress.IPv6Any, 0)
        Else
            lendPoint = New IPEndPoint(IPAddress.Any, 0)
        End If
        xip = remoteip
        xport = remoteport
        mtu = maxtransunit

    End Sub

    Public Sub Set_Factory_Ports(localport As Integer, localip As IPAddress)
        lendPoint = New IPEndPoint(localip, localport)

        If usingipv6 Then
            rendPoint = New IPEndPoint(IPAddress.IPv6Any, 0)
        Else
            rendPoint = New IPEndPoint(IPAddress.Any, 0)
        End If

        xip = localip
        xport = localport


    End Sub


    Public Sub Set_Factory_Ports(localport As Integer, localip As IPAddress, remoteport As Integer, remoteip As IPAddress, maxtransunit As Integer)
        lendPoint = New IPEndPoint(localip, localport)
        'If usingipv6 Then
        ' rendPoint = New IPEndPoint(IPAddress.IPv6Any, 0)
        ' Else
        rendPoint = New IPEndPoint(IPAddress.Any, 0)
        '  End If
        xip = remoteip
        xport = remoteport
        mtu = maxtransunit

    End Sub

    Public Sub service_stop()
        If porttimer Is Nothing Then : Else porttimer.Dispose() : End If
        cts = True
        Me.Dispose()
    End Sub

    Public Sub send_que(b, t)
        Try
            sendframebuffer.Enqueue(ff.frag(b))
        Catch ex As System.IndexOutOfRangeException
            Console.WriteLine("SendQue Broke")
        End Try
    End Sub



    Public Sub Service_Start(usingipv6 As Boolean)
        Try
            If usingipv6 Then
                Console.WriteLine("Using IPv6")
                xsock = New UdpClient(AddressFamily.InterNetworkV6)
                If isanode Then lendPoint = New IPEndPoint(IPAddress.IPv6Any, 0)
                xsock.Client.Bind(lendPoint)

            Else
                Console.WriteLine("Using IPv4")
                xsock = New UdpClient()
                If isanode Then lendPoint = New IPEndPoint(IPAddress.Any, 0)
                xsock.Client.Bind(lendPoint)
            End If
            Console.WriteLine($"Starting Service {1.4} with mtu {mtu}")
            xsock.Client.ReceiveBufferSize = 1024 * 1024 * 1024
            xsock.Client.SendBufferSize = 4096 * 4096

            If rendPoint Is Nothing Then rendPoint = New IPEndPoint(xip, xport)
            sender = New SendFrame
            dframe = New data_frame
            dframe.framenumber = -1
            sender.allsent = False
            sender.framenumber = -1
            lastseen = Now()
            ff = New FragFrame
            'xsock = New Transporter
            If isanode Then startwatchdog()
            Task.Run(Sub() rec_service())
            Task.Run(Sub() send_service())
            If broadcast = True And isanode = True Then Task.Run(Sub() broadcaster())
        Catch ex As Exception
            Console.WriteLine($"Transport Refused to start {ex.Message}")
        Catch ex As System.NullReferenceException
            Console.WriteLine($"Transport Refused to start {ex.Message}")

        End Try


    End Sub


    Private Async Sub rec_service()

        Try
            Do  'Setup an infinite loop
                If cts = True Then
                    If debugon Then Console.WriteLine("RecService got canceltoken")
                    Exit Sub
                End If
                While xsock.Client.Available


                    Dim data = xsock.Receive(rendPoint) '
                    'Dim data = Await xsock.ReceiveAsync()
                    'rendPoint = data.RemoteEndPoint


                    'Console.WriteLine($"Data size {Data.Length}")


                    Select Case data.Length
                        Case 4
                            If Encoding.UTF8.GetString(data) = "PING" Then
                                lastseen = Now()
                                'Console.WriteLine("Got Ping")
                                sendpong()
                            Else
                                lastseen = Now()
                                'Console.WriteLine("Got Pong")

                            End If

                        Case 5
                            Select Case data(0)
                                Case TRANSPORT_CMD.SET_ID
                                    nodeid = BitConverter.ToInt32(data, 1)
                                    Console.WriteLine($"Got nodeid {nodeid} on port {xport}")
                                Case TRANSPORT_CMD.SET_CHANNEL
                                    channel = BitConverter.ToInt32(data, 1)
                                    Console.WriteLine($"Setting Channel to {channel} for localid {nodeid} on port {xport}")
                                Case TRANSPORT_CMD.SET_CANCELTOKEN_TRUE
                                    Console.WriteLine($"Got Kill Command -- Killing Transporter {nodeid} on port {xport}")
                                    service_stop()
                                Case TRANSPORT_CMD.SET_CANCELTOKEN_FALSE
                                    ' service_stop()
                            End Select

                        Case 8
                            updatesequence(data)

                        Case Else

                            receivefrag(data)


                    End Select


                End While
                Sleep(1)
            Loop
        Catch ex As System.IndexOutOfRangeException
            Console.WriteLine("SendQue Broke")


        Catch ex As SocketException
            Debug.WriteLine($"Reciever Broke with exception {ex.Message} IpAddy {rendPoint.Address.ToString} and port {rendPoint.Port}")
            Debug.WriteLine(ex.ErrorCode)
            Task.Run(Sub() rec_service())
        End Try

    End Sub

    Public Sub send_service()
        Try
            Do
                'Dim s = Now
                If cts = True Then
                    If debugon Then Console.WriteLine("SendService got canceltoken")
                    Exit Do
                End If

                If sender.allsent = True Or sender.framenumber = -1 Then
                    checkframeque()
                Else
                    If currentframe = sender.framenumber Then
                        send()
                    End If
                End If
                Sleep(waittime)

                ' Dim td As TimeSpan = Now() - s
                ' Debug.WriteLine($"Time to beat {td.TotalMilliseconds}")
            Loop
        Catch ex As System.IndexOutOfRangeException
            Console.WriteLine("SendService Broke")

        Catch ex As Exception
            If cts = False Then
                Console.WriteLine("Restarting Sendservice")
                Task.Run(Sub() send_service())
            Else
                Console.WriteLine("Killing Sendservice")
            End If

        End Try

    End Sub


    Public Sub checkframeque()

        If cts = True Then
            If debugon Then Console.WriteLine("CheckFrameQUE got canceltoken")
            Exit Sub
        End If

        Try

            If Not sendframebuffer.IsEmpty Then
                'Console.WriteLine($"buffer count = {sendframebuffer.Count}")
                Dim ns As List(Of Byte()) = Nothing
                Dim c = sendframebuffer.TryDequeue(ns)
                If c = True Then
                    'Console.WriteLine($"Frames left to send {sendframebuffer.Count}")
                    sender.dispose()
                    sender = New SendFrame
                    sender.allsent = False
                    sender.framenumber = BitConverter.ToInt32(ns(0), 0)
                    currentframe = sender.framenumber
                    sender.data = ns
                    sender.totalfrags = ns.Count
                    ReDim sender.seqlist(sender.totalfrags - 1)
                End If

            End If
        Catch ex As System.IndexOutOfRangeException
            Console.WriteLine("Checkframe Broke")

        Catch ex As Exception
            Debug.WriteLine($"Checkframe Broke {ex.Message}")
        End Try
    End Sub

    Private Sub updatesequence(data)
        Try
            Dim frame = BitConverter.ToInt32(data, 0)
            Dim seq = BitConverter.ToInt32(data, 4)
            If sender.framenumber = frame And currentframe = frame Then
                sender.updatefrag(seq)
            End If
        Catch ex As System.IndexOutOfRangeException
            Console.WriteLine("updatesequence Broke")

        Catch ex As Exception
            Debug.WriteLine($"updatesequence Broke {ex.Message}")

        End Try

    End Sub

    Public Sub spinlock(microseconds As Long)
        Dim sw As Stopwatch = Stopwatch.StartNew()
        Dim targetTicks As Long = microseconds * Stopwatch.Frequency \ 1000000

        While sw.ElapsedTicks < targetTicks
            Threading.Thread.Sleep(0)
            ' Spin
        End While
    End Sub


    Public Sub send()
        If cts = True Then
            If debugon Then Console.WriteLine("Send Function got canceltoken")
            Exit Sub
        End If
        Try
            sender.lastfrag = sender.checkforlastfrags()
            If sender.lastfrag >= sender.totalfrags Then
                sender.allsent = True
                Exit Sub
            End If
            'SyncLock sendlock
            ' Dim whatsleft = FormatNumber(sender.lastfrag / sender.totalfrags * 100, 0)
            ' Console.WriteLine($"Upload Percent left {whatsleft}%")
            Dim nextt As Integer = sender.lastfrag + fragstosend

            If nextt > sender.totalfrags - 1 Then
                nextt = sender.totalfrags - 1
            End If
            If trafficshaper = True Then
                For t = sender.lastfrag To nextt
                    Send_Packet(sender.data(t))
                    spinlock(trafficdelay)
                Next
            Else
                For t = sender.lastfrag To nextt
                    Send_Packet(sender.data(t))
                Next

            End If
        Catch ex As System.IndexOutOfRangeException
            Console.WriteLine("Send Broke")

        Catch ex As Exception
            Debug.WriteLine($"Send Broke {ex.Message}")

        End Try
    End Sub

    Private Sub sendanylastpackets(data)

        Try
            'If debugon = 1 Then Console.WriteLine($"sending packet size  {data.Length} with que of {sendpacket.Count} ")
            If xsock IsNot Nothing And rendPoint.Address.ToString <> "0.0.0.0" Then
                xsock.Send(data, data.Length, rendPoint)
            Else
                Debug.Print("xsocket dead in transmitting")
            End If
        Catch ex As Exception
            Console.WriteLine($"Sendlastpacket Broke {ex.Message}")

        End Try

    End Sub
    Public Async Sub Send_Packet(data)
        Try
            If cts = True Then
                If debugon Then Console.WriteLine("Send_Packet got canceltoken")
                Sleep(1000)
                sendanylastpackets(data)
                Exit Sub
            End If


            'If debugon = 1 Then Console.WriteLine($"sending packet size  {data.Length} with que of {sendpacket.Count} ")
            If xsock IsNot Nothing And rendPoint.Address.ToString <> "0.0.0.0" Then
                xsock.Send(data, data.Length, rendPoint)
            Else
                Debug.Print("xsocket dead in transmitting")
            End If


        Catch ex As System.IndexOutOfRangeException
            Console.WriteLine("SendPacket Out of Range")


        Catch ex As Exception
            Console.WriteLine($"Send_packet Broke {ex.Message}")
        End Try

    End Sub

    Private Sub sendping()
        If cts = True Then
            If debugon Then Console.WriteLine("Ping Timer got canceltoken")
            porttimer.Dispose()
            Exit Sub
        End If
        Try
            Send_Packet(ping)
            If setid = False Then
                Dim b(4) As Byte
                b(0) = 240
                Dim nid = BitConverter.GetBytes(nodeid)
                Array.Copy(nid, 0, b, 1, 4)
                Send_Packet(b)
                Console.WriteLine($"Sent nodeid {nodeid}")
                setid = True
            End If
        Catch ex As Exception
            Debug.Print($"Ping Failed: {ex.Message}")
        End Try
    End Sub


    Private Sub sendpong()
        Try
            Send_Packet(Encoding.UTF8.GetBytes("PONG"))

        Catch ex As Exception
            Debug.Print($"Pong Failed fail: {ex.Message}")
        End Try
    End Sub

    Private Function stripheader(b As Byte())
        Try
            Dim trimb(b.Length - 21) As Byte
            Array.Copy(b, 20, trimb, 0, trimb.Length)
            Return trimb
        Catch ex As Exception
            Debug.WriteLine($"stripheader Broke {ex.Message}")
        End Try


    End Function



    Public Sub receivefrag(nb)
        If cts = True Then
            If debugon Then Console.WriteLine("RecieveFrag got canceltoken")
            Exit Sub
        End If

        Dim fr = BitConverter.ToInt32(nb, 0) 'frame number
        Dim tf = BitConverter.ToInt32(nb, 8) 'total frags
        Dim snum = BitConverter.ToInt32(nb, 4) 'fragnumber


        Try

            'Console.WriteLine($"grabbed frame {tf}")

            If tf = 1 Then
                If fr > singlelastseen Then
                    If isanode Then
                        Dim rm As New return_frame
                        rm.setframe(nodeid, stripheader(nb))
                        data_ready_node.Enqueue(rm)
                        'Console.WriteLine($"loading Node Que {fr}")
                    Else
                        data_ready_host.Enqueue(stripheader(nb))
                        'Console.WriteLine($"loading Host Que {fr}")
                    End If

                    singlelastseen = fr
                    Dim ack(7) As Byte
                    Array.Copy(nb, 0, ack, 0, 4)
                    Array.Copy(nb, 4, ack, 4, 4)
                    Send_Packet(ack)
                    ' Debug.WriteLine($"frame at {fr}")
                End If
                Exit Sub
            End If




            If fr > dframe.framenumber Then
                If dframe.issent = True Or dframe.datafrags Is Nothing Then
                    dframe.inidata(tf, nb)
                    dframe.issent = False
                    singlelastseen = fr
                    Dim ack(7) As Byte
                    Array.Copy(nb, 0, ack, 0, 4)
                    Array.Copy(nb, 4, ack, 4, 4)
                    Send_Packet(ack)
                End If
            End If
            If fr = dframe.framenumber Then
                dframe.fragseen(snum) = 1
                dframe.insertdata(nb, snum)
                If dframe.isfinished Then
                    If dframe.issent = False Then
                        dframe.issent = True
                        'Console.WriteLine($"Got frame number {dframe.framenumber}")
                        If isanode Then
                            Dim rm As New return_frame
                            rm.setframe(nodeid, dframe.tdata)
                            data_ready_node.Enqueue(rm)
                        Else
                            data_ready_host.Enqueue(dframe.tdata)
                        End If
                    End If
                    ' dframe.clear()
                End If
                Dim ack(7) As Byte
                Array.Copy(nb, 0, ack, 0, 4)
                Array.Copy(nb, 4, ack, 4, 4)
                Send_Packet(ack)
            End If


        Catch ex As System.IndexOutOfRangeException
            Console.WriteLine("RecFrag Out of Range")
            Console.WriteLine($"Frame {fr} totalfrags {tf} fragnum {snum}")

        Catch ex As Exception
            Console.WriteLine($"Receive frag broke {ex.Message}")

        End Try


    End Sub

    Public Sub startwatchdog()
        porttimer = New Threading.Timer(porttimerDelegate, Nothing, 1000, pingdelay)
    End Sub

    Public Sub config(nid, hport, netlat, pkburst, chnnl, hstid, name, broadcastonoff)
        nodeid = nid
        hostid = hstid
        hostport = hport
        waittime = netlat
        fragstosend = pkburst
        channel = chnnl
        localname = name
        broadcast = broadcastonoff

    End Sub





    Public Sub broadcaster()
        Do
            If cts = True Then
                If debugon Then Console.WriteLine("Broadcaster got canceltoken")
                Exit Sub
            End If
            Dim xpack As New xnetmessage_frame
            xpack.routingcode = ROUTE_CODE.CHANNEL
            xpack.messagetype = MESSAGE_TYPE.HELLO
            xpack.controlcodes = 0
            xpack.callerhost = hostid
            xpack.callerid = nodeid
            xpack.channelid = channel
            xpack.normstring = localname
            Dim x As New xNetPacket
            send_que(x.xencodemessageframe(xpack))
            Sleep(10000)
        Loop
    End Sub

    Public Sub setdebug(onoroff)
        debugon = onoroff
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If debugon Then Console.WriteLine($"Disposing of {hostid} {nodeid} ")
        If xsock IsNot Nothing Then
            xsock.Dispose()
            sendframebuffer.Clear()
            sender = Nothing
            dframe = Nothing
        End If

        'GC.Collect()
    End Sub
End Class


