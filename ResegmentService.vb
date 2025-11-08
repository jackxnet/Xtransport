Public Class ResegmentService

    Private datasegments As New List(Of Data_Segment)
    Private lastsegmentreceived As Integer
    Private nextsegmentreturned As Integer
    Private bufferpointer As Integer = 0
    Private buffersize As Integer = 5
    Private datalock As New Object
    Public Event return_segmented(b As Byte())


    Private Function checkfornewframe(framenumber As Integer) As Boolean

        Dim found = False
        For Each frame In datasegments
            If framenumber = frame.framenumber Then
                found = True
                Exit For
            End If
        Next
        Return found
    End Function

    Private Function set_frame_data(framenumber As Integer, data As Byte())
        Dim bufindex As Integer = 0
        For t = 0 To buffersize - 1
            If datasegments(t).framenumber = framenumber Then
                ' datasegments(t).datafrags(BitConverter.ToInt32(data, 4)) = data
                datasegments(t).insertdata(data, BitConverter.ToInt32(data, 4))
                datasegments(t).fragseen(BitConverter.ToInt32(data, 4)) = 1
                bufindex = t
                Exit For
            End If
        Next
        Return bufindex
    End Function

    Private Sub checkdfprogress(bufferindex)


        For t = datasegments(bufferindex).lastseen To datasegments(bufferindex).totalfrags - 1
            If datasegments(bufferindex).fragseen(t) = 0 Then
                datasegments(bufferindex).set_lastseen(t)
                Exit Sub
            End If
        Next
        ' Dim d = datasegments(bufferindex)
        ' d.set_finished()
        datasegments(bufferindex).set_finished()
    End Sub


    Public Sub receivefrag(nb)
        SyncLock datalock
            'Console.WriteLine("receivebuffer error")
            Dim fr = BitConverter.ToInt32(nb, 0) 'frame number
            If lastsegmentreceived < fr Then
                lastsegmentreceived += 1
            End If

            If checkfornewframe(fr) = False Then
                Dim totalfrags = BitConverter.ToInt32(nb, 8)
                Dim d As New Data_Segment
                d.inidata(totalfrags, nb)
                datasegments(bufferpointer) = d
                bufferpointer += 1
                If bufferpointer = buffersize Then bufferpointer = 0


            End If
            checkdfprogress(set_frame_data(fr, nb))

            For t = 0 To buffersize - 1
                If nextsegmentreturned = datasegments(t).framenumber Then
                    If datasegments(t).finished = True Then
                        Dim d = datasegments(t)
                        RaiseEvent return_segmented(d.getdata)
                        nextsegmentreturned += 1
                    End If
                    Exit For
                End If

            Next

        End SyncLock

    End Sub






    'Private Sub run_service()
    '    While canceltoken = False
    '        While resegmentbuffer.Count > 0
    '            SyncLock datalock
    '                Dim nb As Byte()
    '                nb = resegmentbuffer.Dequeue ' new byte()
    '                'Console.WriteLine("receivebuffer error")
    '                Dim fr = BitConverter.ToInt32(nb, 0) 'frame number
    '                If lastsegmentreceived < fr Then
    '                    lastsegmentreceived += 1
    '                End If

    '                If checkfornewframe(fr) = False Then
    '                    Dim totalfrags = BitConverter.ToInt32(nb, 8)
    '                    Dim d As New Data_Segment
    '                    d.inidata(totalfrags, nb)
    '                    datasegments(bufferpointer) = d
    '                    bufferpointer += 1
    '                    If bufferpointer = 20 Then bufferpointer = 0


    '                End If
    '                checkdfprogress(set_frame_data(fr, nb))

    '                For t = 0 To buffersize - 1
    '                    If nextsegmentreturned = datasegments(t).framenumber Then
    '                        If datasegments(t).finished = True Then
    '                            Dim d = datasegments(t)
    '                            nextsegmentreturned += 1
    '                            RaiseEvent return_segmented(d.getdata)

    '                        End If
    '                        Exit For
    '                    End If

    '                Next

    '            End SyncLock

    '        End While


    '        Threading.Thread.sleep(1)

    '        ' check if datasegments ready



    '    End While
    'End Sub
    Public Sub service_start()
        Dim d As New Data_Segment
        d.framenumber = -1
        For t = 0 To buffersize
            datasegments.Add(d)
        Next


    End Sub



End Class
