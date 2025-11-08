Public Class Data_Segment
    Public framenumber As Integer
    Public fragseen As Integer()
    Public datafrags As List(Of Byte())
    Public totalfrags As Integer
    Public finished As Boolean
    Public lastseen As Integer

    Public Sub insertdata(b As Byte(), index As Integer)
        Dim bpayload(b.Length - 13) As Byte
        Array.Copy(b, 12, bpayload, 0, bpayload.Length)
        datafrags(index) = bpayload

    End Sub


    Public Sub inidata(size As Integer, data As Byte())
        datafrags = New List(Of Byte())
        totalfrags = size
        finished = False
        ReDim fragseen(size - 1)
        fragseen(0) = 1
        lastseen = 0

        framenumber = BitConverter.ToInt32(data, 0)
        For t = 0 To size - 1
            datafrags.Add(Nothing)
        Next
        insertdata(data, 0)

    End Sub

    Public Function getdata() As Byte()
        'Return (From bytes In datafrags From x In bytes Select x).ToArray()
        'Dim datar = (From bytes In datafrags From x In bytes Select x).ToArray()
        'Array.Copy(BitConverter.GetBytes(framenumber), datar, 0)
        Dim frametoa As New List(Of Byte)
        For t = 0 To totalfrags - 1
            frametoa.AddRange(datafrags(t))
        Next
        Return frametoa.ToArray()

    End Function

    Public Sub set_lastseen(v As Integer)
        lastseen = v
    End Sub

    Public Sub set_finished()
        finished = True
    End Sub

End Class
