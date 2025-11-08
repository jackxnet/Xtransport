Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.Timers

Public Class SendFrame

    Public sendlock = New Object
    Public totalfrags As Integer = 0
    Public seqlist As Integer()
    Public framenumber As Integer
    Public allsent As Boolean = False
    Public data As New List(Of Byte())
    Public lastfrag As Integer

    Public Sub updatefrag(fragnumber)
        'SyncLock sendlock
        seqlist(fragnumber) = 1
        'End SyncLock

    End Sub


    Public Function checkforlastfrags()
        ' SyncLock sendlock
        For t = lastfrag To totalfrags - 1
            If seqlist(t) = 0 Then
                lastfrag = t
                Return t
            End If
        Next
        ' End SyncLock
        Return totalfrags

    End Function

    Public Sub dispose()
        data.Clear()
        data = Nothing
    End Sub
End Class

