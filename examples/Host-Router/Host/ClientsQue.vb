Imports PXP_Factory.pxPackets
Imports PXP_Factory.RouteHelper
Imports Console_Client_Fast_Transfer.ClientsQueDeque

Public Class ClientsQue

    Public Enum Frame_Type As Integer
        PXP = 1
        DATA = 2
        SQL = 3
    End Enum

    Public Shared Sub sqladdque(o As Object())
        If o Is Nothing Then Exit Sub
        sqlrequests.Enqueue(o)
    End Sub


    Public Shared Sub receivepxp(id As Integer, b As Byte(), ft As Integer, uuid As String, channelid As String)
        Dim o As Object() = {id, b, uuid, channelid}
        Debug.Print("Got frame of type " & ft)
        If ft = Frame_Type.PXP Then addque(o)
        If ft = Frame_Type.SQL Then sqladdque(o)

    End Sub


    Public Shared Sub receivemessage(id As Integer, b As Byte(), uuid As String, channelid As String)
        Dim o As Object() = {id, b, uuid, channelid}
        'Console.WriteLine("Got frametype from " & uuid)
        addmessage(o)
    End Sub

    Public Shared Sub addque(o As Object())
        If o Is Nothing Then Exit Sub
        Dim msg As Byte() = Nothing

        If o(1)(0) = 2 Then

            msg = pushstack(o(1), o(0))
        Else
            msg = popstack(o(1))

        End If

        o(1) = msg
        pxprequests.Enqueue(o)
    End Sub

    Public Shared Sub addmessage(o As Object())
        If o Is Nothing Then Exit Sub
        Dim msg As Byte() = Nothing

        If o(1)(0) = 2 Then

            msg = pushstack(o(1), o(0))
        Else
            msg = popstack(o(1))

        End If

        o(1) = msg
        messagerequests.Enqueue(o)
    End Sub




End Class
