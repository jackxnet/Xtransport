
Public Module Gdata
    Public data_ready_host As New Concurrent.ConcurrentQueue(Of Byte())
    Public data_ready_node As New Concurrent.ConcurrentQueue(Of return_frame)
    Public mtu = 1500
    Public debugon As Boolean = False
    ' Public xtransporters As New List(Of Transporter)

    Public Class return_frame
        Public id As Integer
        Public b As Byte()

        Public Sub setframe(pid As Integer, pb As Byte())
            id = pid
            b = pb
        End Sub

    End Class


End Module
