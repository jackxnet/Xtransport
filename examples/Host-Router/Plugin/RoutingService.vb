Imports xnethost
Imports xPxP
Imports System.Threading.Thread
Imports xPxP.ENUMS
Imports System.Xml
Imports xPxP.xNetPacket
Public Class RoutingService
    Implements IServicePlugin
    Private canceltoken As Boolean
    Private usingconx = False
    Private conxhostid
    Private conxlocalid
    Private conxchannel
    Private conxname
    Private xnet As xNetPacket
    Public Sub addhostservice(params) Implements IServicePlugin.addhostservice
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
        Console.WriteLine("Plugin RoutingService Running")
        Task.Run(Sub() dataready_host())
        Task.Run(Sub() dataready_node())
    End Sub

    Private Sub dataready_host()
        While canceltoken = False
            While data_ready_host.Count > 0
                Dim b As Byte() = Nothing
            Dim r = data_ready_host.TryDequeue(b)
                If r = True Then
                    Dim x = xnet.xDecodeRoute(b)
                    Select Case x.routingcode

                        Case ROUTE_CODE.OTHERNET

                        Case ROUTE_CODE.CHANNEL
                            serversocks.serversock.routechannel(x.callerid, x.channelid, b)

                        Case ROUTE_CODE.MULTICAST
                            serversocks.serversock.routemulticast(x.callerid, b)

                        Case ROUTE_CODE.UNICAST
                            serversocks.serversock.routeunicast(x.localid, b)

                    End Select
                End If
            End While
            Sleep(10)
        End While
    End Sub

    Private Sub dataready_node()
        While canceltoken = False
            While data_ready_node.Count > 0
                Dim rf As New return_frame
                Dim r = data_ready_node.TryDequeue(rf)
                If r = True Then
                    Dim x = xnet.xDecodeRoute(rf.b)
                    Console.WriteLine("Got Local Message")
                End If
            End While
            Sleep(10)
        End While
    End Sub

End Class
