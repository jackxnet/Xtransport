Imports System.Collections.Concurrent
Imports System.IO
Imports System.Net
Imports System.Threading
Imports System.Threading.Thread
Public Class HostClient
    Public xsock As New Transporter
    Public Event dataready(b As Byte())
    Public hostnode As Boolean = True
    Private wtime As Integer
    Private pburst As Integer
    Private ff As FragFrame
    Public nodeid As String
    Public channel As UInteger
    Public hostport As Integer
    Public isanode As Boolean = False
    Private canceltoken As Boolean



    Public Function returnlastseen() As DateTime
        Return xsock.lastseen
    End Function

    Private Sub recframe(b As Byte())
        RaiseEvent dataready(b)
    End Sub


    Public Sub setspeed(wt As Integer, pb As Integer)
        wtime = wt
        pburst = pb

    End Sub

    Public Sub send_que(b As Byte())
        xsock.sendframebuffer.Enqueue(ff.frag(b))
    End Sub

    Public Sub Set_Factory_localPorts(localport As Integer, localip As IPAddress, maxtransunit As Integer)
        xsock.lendPoint = New IPEndPoint(localip, localport)
        xsock.rendPoint = New IPEndPoint(IPAddress.Any, 0)
        xsock.xip = localip
        xsock.xport = localport
        xsock.mtu = maxtransunit
    End Sub


    Public Sub Set_Factory_Ports(remoteport As Integer, remoteip As IPAddress, maxtransunit As Integer)
        'lendPoint = New IPEndPoint(IPAddress.Any, 0)
        xsock.xip = remoteip
        xsock.xport = remoteport
        xsock.mtu = maxtransunit

    End Sub

    Public Sub Set_Factory_Ports(localport As Integer, localip As IPAddress)
        xsock.lendPoint = New IPEndPoint(localip, localport)
        xsock.rendPoint = New IPEndPoint(IPAddress.Any, 0)
        xsock.xip = localip
        xsock.xport = localport


    End Sub


    Public Sub Set_Factory_Ports(localport As Integer, localip As IPAddress, remoteport As Integer, remoteip As IPAddress, maxtransunit As Integer)
        xsock.lendPoint = New IPEndPoint(localip, localport)
        xsock.rendPoint = New IPEndPoint(remoteip, remoteport)
        xsock.xip = remoteip
        xsock.xport = remoteport
        xsock.mtu = maxtransunit

    End Sub

    Public Sub service_stop()
        xsock.canceltoken = True

    End Sub


    Public Sub service_start()
        xsock.canceltoken = False
        xsock.lastseen = Now()
        ff = New FragFrame
        'xsock = New Transporter
        xsock.setpacketspeed(wtime, pburst)
        xsock.Service_Start()
        If isanode Then xsock.startwatchdog()


    End Sub



End Class
