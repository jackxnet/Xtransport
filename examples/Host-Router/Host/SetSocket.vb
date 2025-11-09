Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading.Thread
Imports xPxP


Public Class SetSocket
    Private mysocket As UdpClient
    Private localip As String
    Private localport As Integer
    Private newport = 30000
    Private canceltoken = False
    Private xhost As HostClient


    Private Sub dispatcher_wait()
        While xhost Is Nothing
            Sleep(1)
        End While

        While xhost.xsock Is Nothing
            Sleep(1)
        End While

        While canceltoken = False
            If Not xhost.xsock.data_ready.IsEmpty Then
                Dim b As Byte() = Nothing
                Dim r = xhost.xsock.data_ready.TryDequeue(b)
                If r = True Then
                    Task.Run(Sub() routetraffic(b))
                End If
            End If

            Sleep(1)
        End While


    End Sub

    Private Sub routetraffic(b As Byte())
        For Each host In xhosts
            host.send_que(b)
        Next



    End Sub

    Public Sub InitializeReceiver(ByVal lip As String, ByVal lport As Integer)
        mysocket = New UdpClient
        ' mysocket.ExclusiveAddressUse = False
        localip = lip
        localport = lport
        Dim lendPoint As IPEndPoint = New IPEndPoint(IPAddress.Parse(lip), lport)
        mysocket.Client.Bind(lendPoint)
        Task.Run(Sub() dispatcher_wait())
        Task.Run(Sub() mysocket.BeginReceive(New AsyncCallback(AddressOf Receive), mysocket))

    End Sub


    Public Sub getbyte(b As Byte())

    End Sub




    Public Sub Receive(ByVal ar As IAsyncResult)
        Dim udpClient = TryCast(ar.AsyncState, UdpClient)
        Dim endPoint As IPEndPoint = Nothing
        Dim data = udpClient.EndReceive(ar, endPoint)
        Dim message = Encoding.ASCII.GetString(data)
        Dim messages = Split(message, ":")
        Console.WriteLine(message)
        Dim localid = messages(0)
        Dim channelid = messages(1)
        Dim token = messages(2)

        If token = "REQUESTPORT" Then

            xhost = New xPxP.HostClient
            xhost.setspeed(50, 5000)
            xhost.Set_Factory_Ports(newport, IPAddress.Parse(localip))
            xhost.hostport = newport
            xhost.nodeid = localid
            xhost.channel = channelid
            Console.WriteLine("Starting DXP Service...." & localid & " on Port .. " & newport & " On channel " & channelid)
            xhost.service_start()
            xhosts.Add(xhost)


            Console.WriteLine("NEW REQUEST")

            message = newport.ToString
            data = Encoding.UTF8.GetBytes(message)
            Task.Run(Sub() udpClient.Send(data, data.Length, endPoint))
            newport += 1
            If newport > 50000 Then newport = 30000

        End If

        Task.Run(Sub() udpClient.BeginReceive(AddressOf Receive, udpClient))
    End Sub





End Class
