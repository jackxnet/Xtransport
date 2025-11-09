Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Reflection.PortableExecutable
Imports System.Threading

Public Class ReceiveFrameold

    Public totalfrags As Integer = 0
    Public segment As List(Of Byte())
    Public reclock = New Object
    Public seqlist(1) As Integer
    Public currentframe As Integer = -1
    Private af As New FragFrame




    Private Function checkforfrags(f As Integer)
        For t = 0 To f
            If seqlist(t) = 0 Then
                Return 0
            End If
        Next
        Return 1
    End Function

    Public Sub receive(data As Byte())

        Try

            Dim frame = BitConverter.ToInt32(data, 0)
            Dim sequence = BitConverter.ToInt32(data, 4)
            If currentframe < frame Then
                totalfrags = BitConverter.ToInt32(data, 8)
                ReDim seqlist(totalfrags)
                currentframe = frame
            End If
            seqlist(sequence) = 1
            segment(sequence) = data


            If checkforfrags(totalfrags) = 1 And currentframe = frame Then
                Console.WriteLine($"Returning Frame {currentframe}")
                'Debug.WriteLine($"Returning Frame {currentframe}")
                'SyncLock reclock
            End If





        Catch ex As Exception
            Debug.WriteLine(ex.ToString)
            Debug.WriteLine(ex.ToString & " Closing = TRUE")

            ' If Not xsocket Is Nothing Then xsocket.Close() : xsocket.Dispose()

        End Try


    End Sub

    Public Sub start_service()
        Dim thread As New Thread(AddressOf receive)
        thread.Start()

    End Sub

End Class


