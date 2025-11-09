Imports System.IO
Imports System.Threading
Imports System.Threading.Thread
Imports xPxP
Imports xPxP.ENUMS
Imports Emgu.CV
Imports Emgu.CV.BitmapExtension
Imports Emgu.CV.Structure
Imports System.Text
Imports OpenTK.audio
Imports OpenTK.audio.OpenAL
Imports OpenTK.Graphics.OpenGL.GL
Imports System.Reflection.Metadata
Imports xPxP.xNetPacket
Imports xPxP.nodes
Imports IPV6Helper
Imports xnet_Video_Factory
Imports xnet_Audio_Factory
Imports System.Net
Imports xnet_SIP_Factory
Imports xnet_dbase_service
Imports System.Runtime.Loader
Imports System.Text.Json
Imports xnethost.serversocks
Imports Newtonsoft.Json

Module Program
    'Private WithEvents Iservice As IServicePlugin
    Dim hostmode = False
    Dim hostchannel As Integer
    Dim hostx As XHost

    Private canceltoken = False
    'Dim plugins As New List(Of IServicePlugin)
    Private WithEvents Iservice As IServicePlugin

    Private Sub loadPlugins()
        For Each dll In Directory.GetFiles(Filepaths.pluginpath, "*.dll")
            Dim alc = New AssemblyLoadContext(dll)
            Dim asbly = alc.LoadFromAssemblyPath(dll)
            Dim plugin = Activator.CreateInstance(asbly.GetTypes()(0))
            Iservice = plugin
        Next
    End Sub


    Private Function stringtobool(s As String) As Boolean
        If s.ToLower = "true" Then Return True
        If s.ToLower = "false" Then Return False
    End Function

    Public Function RemoveWhitespace(ByVal input As String) As String
        Return New String(input.ToCharArray().Where(Function(c) Not Char.IsWhiteSpace(c)).ToArray())
    End Function


    Public Sub getconfig()
        Dim fileName = "settings.txt"
        Const BufferSize As Int32 = 128

        Using fileStream = File.OpenRead(fileName)

            Using streamReader = New StreamReader(fileStream, Encoding.UTF8, True, BufferSize)
                Dim lines = File.ReadLines(fileName)

                For Each line In lines
                    Dim wline = RemoveWhitespace(line)
                    Dim Slines = wline.Split("=")
                    Dim sname = Slines(0)
                    Dim svalue = Slines(1)

                    Select Case sname
                        Case "dbasepath"
                            Filepaths.dbasepath = svalue

                        Case "filepath"
                            Filepaths.filepath = svalue

                        Case "pluginpath"
                            Filepaths.pluginpath = svalue
                            Console.WriteLine("Loading Plugins")
                            loadPlugins()
                            Console.WriteLine("Finish loading plugin")
                            Iservice.DoProcess()
                        Case "debugon"
                            If svalue = 1 Then
                                Gdata.debugon = True
                                Console.WriteLine("Debug is Enabled")
                            Else
                                Gdata.debugon = False
                                Console.WriteLine("Debug is Disabled")
                            End If

                        Case "service"
                            Iservice.addservice(svalue)

                        Case "client"
                            Dim c = New XClient
                            Dim sp = Split(svalue, ",")
                            'c.clienttype = DirectCast([Enum].Parse(GetType(SERVICE_TYPE), sp(0)), SERVICE_TYPE)
                            Iservice.addclientservice(svalue)


                        Case "Host"
                            Dim sp = Split(svalue, ",")
                            hostinfo.hosttype = DirectCast([Enum].Parse(GetType(HOST_TYPE), sp(0)), HOST_TYPE)
                            hostinfo.hostid = sp(1)
                            hostinfo.ip = sp(2)
                            hostinfo.port = sp(3)
                            hostinfo.channel = sp(4)
                            hostchannel = sp(4)
                            hostinfo.portmin = sp(5)
                            hostinfo.portmax = sp(6)
                            hostinfo.netlatency = sp(7)
                            hostinfo.packetburst = sp(8)
                            hostinfo.useipv6 = sp(9)
                            hostinfo.mtu = sp(10)
                            hostx = hostinfo
                            starthost(hostinfo)
                            Iservice.addhostservice(svalue)



                    End Select
                Next

            End Using
        End Using
    End Sub

    Public Sub starthost(h As XHost)
        Console.WriteLine($"Starting up Listener at {h.port} and setting channel to {h.channel}")
        'serversock = New SetSocket
        If useipv6 Then serversock.settoipv6()
        'serversock.hostservice = True
        serversock.setspeed(h.netlatency, h.packetburst)
        serversock.setportrange(h.portmin, h.portmax)
        serversock.InitializeReceiver(h.ip, h.port)
        serversock.setmtu(h.mtu)
        Task.Run(Sub() dokilldead())

    End Sub

    'Public Event routehostevent(b As Byte())
    Private Sub dokilldead()
        While canceltoken = False
            serversock.checkfordead()
            Sleep(8000)
        End While
    End Sub

    Private Async Function doit(token As CancellationToken) As Task(Of Integer)
        Do
            If token.IsCancellationRequested Then Return 1
            Task.Delay(5000)
        Loop
    End Function

    Private Async Sub buttercup()
        Console.WriteLine("Starting task")
        Dim st = Now
        Dim cts = New CancellationTokenSource
        cts.CancelAfter(5000)
        Dim token = cts.Token
        Dim tsk = doit(token)
        Await tsk
        Dim et = Now
        Console.WriteLine($"time on task = {(et - st).Seconds}")
    End Sub

    Sub Main(args As String())

        getconfig()

        Thread.Sleep(Timeout.Infinite)
        Console.WriteLine("Neverhitthis")

    End Sub

End Module


