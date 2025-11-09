Imports xnethost
Imports xPxP
Imports System.Threading.Thread
Imports xPxP.ENUMS
Imports System.Xml
Imports xPxP.xNetPacket
Imports Emgu.CV
Imports Emgu.CV.Structure
Imports Emgu.CV.CvEnum
Imports xnethost.serversocks
Imports xnethost.GDictionaries
Imports Windows.Win32.System
Imports System.IO
Imports System.Net
Imports System.Reflection.Metadata
Imports Emgu.CV.Features2D
Public Class FileService
    Implements IServicePlugin
    Public canceltoken As Boolean
    Private msgr As messengerService

    Public Sub addhostservice(params) Implements IServicePlugin.addhostservice
        Dim hs As New HostService
        Dim p = Split(params, ",")
        hs.id = p(10)
        hs.DoProcess()
    End Sub

    Public Sub drophostservice(params) Implements IServicePlugin.drophostservice

    End Sub


    Public Sub addclientservice(params) Implements IServicePlugin.addclientservice

    End Sub

    Public Sub dropclientservice(params) Implements IServicePlugin.dropclientservice

    End Sub



    Public Sub addservice(params) Implements IServicePlugin.addservice

    End Sub

    Public Sub dropservice(params) Implements IServicePlugin.dropservice
    End Sub

    Public Sub DoProcess() Implements IServicePlugin.DoProcess
        Console.WriteLine("Plugin FileService Running")
        Task.Run(Sub() dataready_node())
    End Sub



    Private Sub dataready_node()
        While canceltoken = False
            While data_ready_node.Count > 0
                Dim rf As return_frame = Nothing
                Dim r = data_ready_node.TryDequeue(rf)
                If r = True Then
                    If rf.id = msgr.id Then
                        msgr.processdata(rf.b)
                    End If

                    'do work with new data
                End If
            End While
            Sleep(10)
        End While
    End Sub


End Class

Public Class messengerService
    Public id As Integer
    Public canceltoken As Boolean
    Public nc As nodeclient
    Public xc As New XClient

    Public Sub buildxclient(params)
        Dim sp = Split(params, ",")
        xc.clienttype = DirectCast([Enum].Parse(GetType(SERVICE_TYPE), sp(0)), SERVICE_TYPE)
        xc.hostid = sp(1)
        xc.hostip = sp(2)
        xc.hostport = sp(3)
        xc.channel = sp(4)
        xc.netlatency = sp(5)
        xc.packetburst = sp(6)
        xc.localid = sp(7)
        xc.localname = sp(8)
        xc.useipv6 = sp(9)
        xc.broadcast = sp(10)
        id = sp(7)
    End Sub


    Public Sub startservice()
        nc = New nodeclient
        nc.config(xc)
        nc.startclient(xc.hostip)
    End Sub



    Public Sub processdata(b)
        Dim xf As New xnetmessage_frame
        Dim xnet As New xNetPacket
        xf = xnet.xDecodemessage(b)

        If xf.controlcodes = CONTROL_TYPE.REQUEST Then

            If xf.messagetype = MESSAGE_TYPE.HOSTINFO Then

                Dim params = xnet.hostinfotoparams(hostinfo)

            End If

        End If



    End Sub

End Class

Public Class OtherService


End Class


Public Class HostService
    Public id As Integer
    Public canceltoken As Boolean

    Public Sub DoProcess()
        Task.Run(Sub() dataready_host())
    End Sub

    Private Sub dataready_host()
        While canceltoken = False
            Try
                While data_ready_host.Count > 0
                    Dim b As Byte() = Nothing
                    Dim r = data_ready_host.TryDequeue(b)
                    If r = True Then
                        'Console.WriteLine($"Receiving File  size of file {b.Length}")
                        processdata(b)
                    End If
                End While
                Sleep(10)
            Catch ex As Exception
                Console.WriteLine($"datareadyhost Broke {ex.Message}")
            End Try

        End While
    End Sub




    Private Sub recxpayload(b As Byte())
        Dim xnet As New xNetPacket
        Dim xf As New xnetpayload_frame
        xf = xnet.xDecodepayload(b)
        If xf.payloadtype = PAYLOAD_TYPE.FILE Then
            'Dim filename = Filepaths.filepath & xf.paynorm
            Dim subpath = Split(xf.paynorm, "/")
            If Not IO.Directory.Exists(Filepaths.filepath & subpath(0)) Then
                IO.Directory.CreateDirectory(Filepaths.filepath & subpath(0))
            End If
            Dim filename = Filepaths.filepath & xf.paynorm
            Console.WriteLine($"Receiving File {filename} size of file {xf.payload.Length}")
            'Exit Sub
            ' Task.Run(Sub()

            Dim Fs = New FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)
            Fs.Write(xf.payload) ' // just feed it the contents verbatim
                Fs.Flush()
                Fs.Close()
                xf.payload = Nothing
                Fs.Dispose()
                ' GC.Collect()
                '           End Sub)
            End If

        ' xf.Dispose()
        'xnet.Dispose()


    End Sub




    Private Sub sendfile(filename As String, callhost As Integer, callid As Integer, port As Integer)

        Dim FileStream = New System.IO.FileStream(Filepaths.filepath & filename, System.IO.FileMode.Open, System.IO.FileAccess.Read)
        Dim binreader = New System.IO.BinaryReader(FileStream)
        Dim payload = binreader.ReadBytes(FileStream.Length)

        Console.WriteLine($"Sending file {filename} on port {port}")
        Dim x As New xNetPacket
        Dim xframe As Byte() = x.xEncodepayload(ROUTE_CODE.PORT, callhost, callid, hostinfo.channel, hostinfo.hostid, id, MESSAGE_TYPE.PAYLOAD, 0, 0, 0, "SendFile", PAYLOAD_TYPE.FILE, 0, filename, payload)
        serversock.routeport(port, xframe)
    End Sub


    Public Sub processdata(b)
        Dim xnet As New xNetPacket
        Dim xf As New xnetmessage_frame
        xf = xnet.xDecodemessage(b)

        If xf.messagetype = MESSAGE_TYPE.PAYLOAD Then
            recxpayload(b)
            Exit Sub
        End If


        If xf.messagetype = MESSAGE_TYPE.FILEREQUEST Then
            sendfile(xf.normstring, xf.hostid, xf.callerid, xf.channelid)
        End If

        If xf.messagetype = MESSAGE_TYPE.DISCIO Then
            sendfile(xf.normstring, xf.hostid, xf.callerid, xf.channelid)
        End If
    End Sub

End Class


Public Class ClientService
    Public id As Integer
    Public Sub StartService()

    End Sub

End Class



