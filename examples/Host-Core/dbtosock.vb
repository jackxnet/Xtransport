Imports NiftyCore
Imports System.IO
Imports Newtonsoft.Json
Imports System.Data
Imports System.Net
Imports System.Threading

Public Class dbtosock

    Private receivingThread As Thread
    Private listenport As IPEndPoint
    Private sendport As IPEndPoint
    Private ipport As String

    Dim mydb As New Dbase
    Dim dbase As String = "ports"




    Public Function sqlquery(request As String)
        mydb.setconnector(server, "3306", user, password, db)
        Return mydb.doquery(request)

    End Function

    Public Function sqlnonquery(request As String)
        mydb.setconnector(server, "3306", user, password, db)
        Return mydb.dononquery(request)
    End Function












    Public Sub sqlinsert(message As String)
        mydb.setconnector(server, "3306", user, password, db)
        ' Dim result As Integer
        ' Dim result = mydb.
    End Sub



    Public Sub sqlupdate(message As String)
        mydb.setconnector(server, "3306", user, password, db)

    End Sub
    Public Sub sqldelete(message As String)
        mydb.setconnector(server, "3306", user, password, db)

    End Sub

End Class
