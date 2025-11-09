Imports System.Net
Imports xPxP
Imports xPxP.ENUMS


Module Globals

    'Public serversock As SetSocket
    'Public nodesockets As New nodes
    'Public conxsockets As New nodes

    Public ipv4addy As String
    Public ipv6addy As String
    Public ipaddy As String

    Public useipv6 = False


End Module

Public Class Filepaths
    Public Shared filepath As String
    Public Shared pluginpath As String
    Public Shared dbasepath As String

End Class
Public Class msg
    Public id As Integer
    Public channel As Integer
    Public svctype As SERVICE_TYPE
    Public payload As Byte()
End Class
Public Class serversocks
    Public Shared hostinfo As New XHost
    Public Shared serversock As New SetSocket
End Class

Public Class GDictionaries
    Public Shared Ghosts = New Dictionary(Of Integer, SetSocket)
    Public Shared Gnodes = New Dictionary(Of Integer, nodeclient)

End Class


