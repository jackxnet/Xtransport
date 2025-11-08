Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports System.Threading.Thread
Imports System.Timers
Imports xPxP.ENUMS

Public Class nodeclient : Implements IDisposable
    Public clientip As String = Nothing
    ' Private sock = New Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp)

    Private XControl As UdpClient                           'Client for sending data
    Private receivingThread As Thread                            'Create a separate thread to listen for incoming data, helps to prevent the form from freezing up
    Private xserveraddress As IPAddress
    Private usingipv6 = False
    Private xservstring As String
    Public lendPoint As IPEndPoint
    Public initialized = False

    Private mysocket As UdpClient
    Public xtransport As Transporter
    Public clientport As Integer
    ' Private headers As PXPFactory.pxPackets
    Dim porttimerDelegate As TimerCallback = AddressOf checkportstatus
    Private porttimer As Threading.Timer
    Private cport As Integer = 0
    Private xnodetimeout As TimeSpan
    Public Event dataready(b As Byte())
    Private netlatency As Integer = 100
    Private packetburst As Integer = 10
    Private trafficdelay As Integer = 0
    Public hostport = 7001
    Public localid = 10000000
    Public channel = 500
    Public hostid As Integer = 1000
    Public localname As String = "defaultclient"
    Public isanotherhost As Boolean = False
    Public isconx As Boolean = False
    Public broadcaster As Boolean = False
    Public cts As Boolean = False



    Public Sub setmtu(m)
        mtu = m
    End Sub
    Public Sub setspeed(wt As Integer, pb As Integer)
        netlatency = wt
        packetburst = pb

    End Sub

    Public Sub config(nid, hport, netlat, pkburst, chnnl, hstid, name, ipv6, broadcast)
        localid = nid
        hostid = hstid
        hostport = hport
        netlatency = netlat
        packetburst = pkburst
        channel = chnnl
        localname = name
        usingipv6 = ipv6
        broadcaster = broadcast

    End Sub

    Public Sub config(xconx As XClient)
        hostid = xconx.hostid
        hostport = xconx.hostport
        channel = xconx.channel
        netlatency = xconx.netlatency
        packetburst = xconx.packetburst
        localid = xconx.localid
        localname = xconx.localname
        usingipv6 = xconx.useipv6
        broadcaster = xconx.broadcast
    End Sub

    Public Sub cmdsetchannel(c As Integer)
        channel = c
        xtransport.channel = c
        Dim b(4) As Byte
        b(0) = TRANSPORT_CMD.SET_CHANNEL
        Dim bch = BitConverter.GetBytes(c)
        Array.Copy(bch, 0, b, 1, 4)
        xtransport.Send_Packet(b)
        Console.WriteLine($"Changing Channel to {c}")

    End Sub

    Public Sub cmdcanceltokenTrue()
        Dim b(4) As Byte
        b(0) = TRANSPORT_CMD.SET_CANCELTOKEN_TRUE
        Dim bch = BitConverter.GetBytes(123456)
        Array.Copy(bch, 0, b, 1, 4)
        xtransport.Send_Packet(b)
        Console.WriteLine($"Set Cancel Token to True")

    End Sub

    Public Sub service_stop()
        cmdcanceltokenTrue()
        Sleep(netlatency * 4)
        xtransport.service_stop()
        xtransport.Dispose()
        cts = True
        porttimer.Dispose()

    End Sub

    'Private Sub dispatcher_run()
    '    While xtransport Is Nothing
    '        Sleep(1)
    '    End While

    '    While xtransport.xsock Is Nothing
    '        Sleep(1)
    '    End While

    '    While canceltoken = False
    '        Select Case isconx
    '            Case True
    '                If Not data_ready.IsEmpty Then
    '                    Dim b As Byte() = Nothing
    '                    Dim r = data_ready.TryDequeue(b)
    '                    If r = True Then
    '                        GQues.conxin.Enqueue(b)
    '                        'RaiseEvent dataready(b)
    '                    End If
    '                End If
    '            Case False
    '                If Not data_ready.IsEmpty Then
    '                    Dim b As Byte() = Nothing
    '                    Dim r = data_ready.TryDequeue(b)
    '                    If r = True Then
    '                        GQues.nodedatain.Enqueue(b)
    '                        'RaiseEvent dataready(b)
    '                    End If
    '                End If
    '        End Select


    '        If Not GQues.nodedataout.IsEmpty Then
    '            Select Case isconx
    '                Case True
    '                    Dim b As Byte() = Nothing
    '                    Dim r = GQues.conxout.TryDequeue(b)
    '                    If r = True Then
    '                        xtransport.send_que(b)
    '                    End If
    '                Case False
    '                    Dim b As Byte() = Nothing
    '                    Dim r = GQues.nodedataout.TryDequeue(b)
    '                    If r = True Then
    '                        xtransport.send_que(b)
    '                    End If
    '            End Select

    '        End If

    '        Sleep(1)
    '    End While


    'End Sub



    Public Sub settoipv6()
        usingipv6 = True
    End Sub


    Private Sub initialize(xserver As String)
        xserveraddress = IPAddress.Parse(xserver)
        xservstring = xserver
        If usingipv6 Then
            XControl = New UdpClient(AddressFamily.InterNetworkV6)
            lendPoint = New IPEndPoint(IPAddress.IPv6Any, 0)
            XControl.Client.Bind(lendPoint)
        Else
            XControl = New UdpClient
            lendPoint = New IPEndPoint(IPAddress.Any, 0)
            XControl.Client.Bind(lendPoint)
        End If
        initialized = True
    End Sub

    Private Sub restartxnode()
        If initialized = False Then initialize(xservstring)
        GetNewPorts(hostport)

    End Sub


    Private Sub checkfornewport()

        If cport = 0 Then
            Console.WriteLine("RESTARTING xNET PORT")
            restartxnode()
        Else
            Console.WriteLine("SETTING UP XNet")
            xnoderestart()
            porttimer = New Threading.Timer(porttimerDelegate, Nothing, 2500, 2500)
        End If

    End Sub


    Private Sub xnodeStop()

        xtransport.service_stop()
        clientport = 0

    End Sub


    Private Sub checkportstatus()

        If cts = True Then Exit Sub

        If xtransport IsNot Nothing Then
            xnodetimeout = Now() - xtransport.returnlastseen
            If xnodetimeout.TotalSeconds > 10 Then xnodeStop()
            'Console.WriteLine("cps " & xnodetimeout.TotalSeconds)
        Else
            clientport = 0
        End If

        If clientport <> 0 Then Exit Sub
        GetNewPorts(hostport)

    End Sub

    Private Sub GetNewPorts(pnum As Integer)

        Try
            If cts = True Then Exit Sub
            Console.WriteLine("Getting New Port Number")
            If usingipv6 Then
                mysocket = New UdpClient(AddressFamily.InterNetworkV6)
                lendPoint = New IPEndPoint(IPAddress.IPv6Any, 0)
                mysocket.Client.Bind(lendPoint)
            Else
                mysocket = New UdpClient
                lendPoint = New IPEndPoint(IPAddress.Any, 0)
                mysocket.Client.Bind(lendPoint)

            End If
            Dim start As ThreadStart = New ThreadStart(AddressOf Receive1)
            receivingThread = New Thread(start)
            receivingThread.IsBackground = True
            receivingThread.Start()
            Dim data() As Byte = Encoding.ASCII.GetBytes($"{localid}:{channel}:REQUESTPORT:{isanotherhost}") 'Convert string to bytes
            Dim rendPoint As IPEndPoint = New IPEndPoint(xserveraddress, pnum)
            mysocket.Send(data, data.Length, rendPoint)
        Catch ex As exception
            console.writeline($"Error Grabbing New Port {ex.message}")
        End Try




    End Sub


    Private Sub Receive1()
        If cts = True Then Exit Sub

        'Throw New NotImplementedException()
        Try
            Dim rendPoint As IPEndPoint = New IPEndPoint(xserveraddress, 0)
            Dim rendPoint6 As IPEndPoint = New IPEndPoint(xserveraddress.AddressFamily, 0)
            Dim data As Byte()
            If usingipv6 Then
                data = mysocket.Receive(rendPoint6)
            Else
                data = mysocket.Receive(rendPoint)
            End If

            clientport = Encoding.UTF8.GetString(data)
            Console.WriteLine("Got New Port Number" & clientport)

        Catch ex As Exception
            Console.WriteLine("Got Error Connect on Port " & clientport)

        End Try

        mysocket.Close()
        xnoderestart()

    End Sub

    Public Sub settrafficshaper(dlay As Integer)
        trafficdelay = dlay
    End Sub



    Private Sub xnoderestart()

        If cts = True Then Exit Sub
        If xtransport IsNot Nothing Then xtransport.service_stop()
        xtransport = New Transporter
        xtransport.Set_Factory_Ports(CInt(clientport), xserveraddress, mtu)
        xtransport.isanode = True
        xtransport.config(localid, hostport, netlatency, packetburst, channel, hostid, localname, broadcaster)
        If trafficdelay > 0 Then

            xtransport.setspeed(netlatency, packetburst, trafficdelay)
        Else
            xtransport.setspeed(netlatency, packetburst)

        End If
        'AddHandler data_ready, AddressOf getbyte
        xtransport.Service_Start(usingipv6)
        'xtransporters.Add(xtransport)
    End Sub



    Public Sub startclient(xserver As String)
        initialize(xserver)
        Console.WriteLine("Starting up porttimer")
        'Task.Sta(Sub() porttimer = New Threading.Timer(porttimerDelegate, Nothing, 500, 1500))
        porttimer = New Threading.Timer(porttimerDelegate, Nothing, 500, 5000)

    End Sub

    Public Sub setdebug(onoff)
        If onoff = 1 Then
            debugon = True
        Else
            debugon = False
        End If

    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        GC.Collect()
    End Sub


End Class
