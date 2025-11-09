Imports PXP_Factory.RouteHelper
Imports PXP_Factory.pxPackets
Imports PXP_Factory
Imports System.Data
Imports System.Text
Imports System.Threading.Channels

Public Class ClientDeque

    Private dequetimer As System.Timers.Timer

    Private Enum routetype
        pxpque = 1
        messageque = 2
    End Enum


    Private Enum destination
        client = 1
        backbone = 2
    End Enum


    Private Enum PORT_DIRECTION As Integer
        PORTIN = 0
        PORTOUT = 1
    End Enum

    Public Enum Service_Type As Integer
        PXP = 0
        VIDEO = 1
        DATA = 2
        SQL = 3
    End Enum

    Public Enum Frame_Type As Integer
        PXP = 1
        DATA = 2
        SQL = 3
    End Enum

    Private Enum SQL_TYPE As Byte
        QUERY = 1
        NONQUERY = 2
    End Enum


    Sub addsql(o As Object())
        If o Is Nothing Then Exit Sub
        sqlrequests.Enqueue(o)

    End Sub


    Private Function stringtobool(s As String) As Boolean
        If s.ToLower = "true" Then Return True
        If s.ToLower = "false" Then Return False
    End Function

    Private dtsql As New dbtosock
    Private Sub routeclients(o As Object, rt As routetype)
        Dim msg As Byte() = o(1)
        Dim nexthop = getstack(msg)
        msg = popstack(msg)


        For t = 0 To xsocks.Count - 1
            Try
                If nexthop(nexthop.Length - 1) = xsocks(t).uuid And xsocks(t).portdirection = PORT_DIRECTION.PORTOUT And xsocks(t).channelid = o(3) Then
                    If rt = routetype.messageque Then
                        Console.WriteLine("Clients  quickroute to uuid " & xsocks(t).uuid)
                        xsocks(t).messagequeue(msg)
                    End If

                    If rt = routetype.pxpque Then
                        Console.WriteLine("Clients route to uuid " & xsocks(t).uuid)
                        xsocks(t).sendqueue(msg, Frame_Type.PXP)
                    End If
                End If
            Catch ex As Exception
                Debug.Print("Problem sending queue")
            End Try

        Next
    End Sub

    Private Sub multicastclients(o As Object, rt As routetype)
        For t = 0 To xsocks.Count - 1
            Try
                If o(0) <> xsocks(t).uuid And xsocks(t).portdirection = PORT_DIRECTION.PORTOUT And xsocks(t).channelid = o(3) Then
                    'Console.WriteLine("Sending multicast packet to " & xsocks(t).myport & " With Port Direction " & xsocks(t).portdirection)
                    Dim msg = pushstack(o(1), xsocks(t).uuid)
                    Try
                        If rt = routetype.messageque Then
                            ' Console.WriteLine("Clients multicast to port " & s.xsocks(t).myid)
                            xsocks(t).messagequeue(msg)
                        End If

                        If rt = routetype.pxpque Then
                            xsocks(t).sendqueue(msg, Frame_Type.PXP)
                        End If
                    Catch ex As Exception
                        Debug.Print("Problem sending queue")
                    End Try

                End If
            Catch ex As Exception
            End Try

        Next
    End Sub

    Public Sub StartClientDeque()
        dequetimer = New System.Timers.Timer(500)
        AddHandler dequetimer.Elapsed, AddressOf dequeall
        dequetimer.Start()
    End Sub


    Dim locked As Boolean

    Private Sub processquery(request, sqchannel, uuid)
        Dim dtresult As DataTable
        Dim dtderror As Integer
        Dim onerror = False
        Dim result = dtsql.sqlquery(request)

        If TypeOf result Is Integer Then
            If result = -1 Then dtderror = result
            onerror = True
        Else
            dtresult = result
        End If


        Dim msg As Byte()

        If onerror Then
            msg = Encoding.UTF8.GetBytes("-1")
        Else

            Dim stream As New System.IO.MemoryStream()
            Dim formatter As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
            formatter.Serialize(stream, result)
            msg = stream.GetBuffer()
        End If

        Dim newmsg = Sqlreplyquery(msg, sqchannel)

        For t = 0 To xsocks.Count - 1
            If uuid = xsocks(t).uuid And xsocks(t).portdirection = PORT_DIRECTION.PORTOUT Then
                xsocks(t).sendqueue(newmsg, Frame_Type.SQL)
            End If
        Next


    End Sub

    Private Sub processnonquery(request, sqchannel, uuid)
        Dim dtresult As DataTable
        Dim dtderror As Integer
        Dim onerror = False

        Dim result = dtsql.sqlnonquery(request)

        If TypeOf result Is Integer Then
            If result = -1 Then dtderror = result
            onerror = True
        Else
            dtresult = result
        End If


        Dim msg As Byte()

        If onerror Then
            msg = Encoding.UTF8.GetBytes("-1")
        Else

            Dim stream As New System.IO.MemoryStream()
            Dim formatter As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
            formatter.Serialize(stream, result)
            msg = stream.GetBuffer()
        End If

        Dim newmsg = Sqlreplynonquery(msg, sqchannel)
        For t = 0 To xsocks.Count - 1
            If uuid = xsocks(t).uuid And xsocks(t).portdirection = PORT_DIRECTION.PORTOUT Then
                xsocks(t).sendqueue(newmsg, Frame_Type.SQL)
            End If
        Next

    End Sub

    Public Sub dequeall()
        Try
            dequetimer.Stop()

            If sqlrequests.Count > 0 Then
                Dim oo = sqlrequests.Dequeue
                Dim b As Byte() = oo(1)

                Dim msgln As Integer = BitConverter.ToUInt32(b, 4)
                Dim omsg(msgln - 1) As Byte
                Array.Copy(b, 8, omsg, 0, msgln)
                Dim sqchannel As Byte = b(3)
                Dim sqtype As Byte = b(1)


                Dim request = Encoding.UTF8.GetString(omsg)
                'Dim rsplit = Split(request, "█")


                If sqtype = SQL_TYPE.NONQUERY Then
                    processnonquery(request, b(3), oo(3))
                End If
                If sqtype = SQL_TYPE.QUERY Then
                    processquery(request, b(3), oo(3))
                End If
                dequetimer.Start()
                Exit Sub
            End If




            If messagerequests.Count > 0 Then
                Dim o = messagerequests.Dequeue
                If o Is Nothing Then
                    dequetimer.Start()
                    Exit Sub
                End If

                If o(1)(0) = 2 Then
                    multicastclients(o, routetype.messageque)
                Else
                    routeclients(o, routetype.messageque)
                End If
                dequetimer.Start()
                Exit Sub
            End If


            If pxprequests.Count > 0 Then
                Dim o = pxprequests.Dequeue
                If o Is Nothing Then
                    dequetimer.Start()
                    Exit Sub
                End If

                If o(1)(0) = 2 Then
                    multicastclients(o, routetype.pxpque)
                Else
                    routeclients(o, routetype.pxpque)

                End If

            End If
            dequetimer.Start()
        Catch ex As Exception
            Console.WriteLine($"Exception Error Sub DequeAll {ex.Message}")
            dequetimer.Start()
        End Try

    End Sub


End Class

