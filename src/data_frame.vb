Imports System
Imports System.Drawing
Imports System.Buffers
Public Class data_frame

    Public framenumber As Integer
    Public fragseen As Integer()
    Public datafrags As List(Of Byte())
    Public totalfrags As Integer
    Public finished As Boolean
    Public lastseen As Integer
    'Public tdata As bigobject
    Public tdata As Byte()
    Public issent = False


    Public Sub inidata(size As Integer, b As Byte())

        'tdata = New bigobject
        ReDim fragseen(size - 1)
        ReDim tdata(BitConverter.ToInt32(b, 12) - 1)
        totalfrags = size
        finished = False
        lastseen = 0
        issent = False

        Dim fragnum = BitConverter.ToInt32(b, 4)
        framenumber = BitConverter.ToInt32(b, 0)
        fragseen(fragnum) = 1
        'tdata = ArrayPool(Of Byte).Shared.Rent(BitConverter.ToInt32(b, 12) - 1)
        'tdata.inibigo(b)
        insertdata(b, fragnum)

    End Sub


    Public Sub insertdata(b, fragnum)
        System.Buffer.BlockCopy(b, 20, tdata, fragnum * mtu, b.Length - 20)

        ' If tdata IsNot Nothing Then
        'tdata.insertdata(b, fragnum)
        ' End If

    End Sub

    Public Function isfinished() As Boolean
        For t = lastseen To totalfrags - 1
            If fragseen(t) = 0 Then
                lastseen = t
                Return (False)
            End If
        Next
        finished = True
        Return True
    End Function


End Class

Public Class bigobject
    'Implements IDisposable

    Dim bigo As Byte()

    Public Sub inibigo(b)
        bigo = ArrayPool(Of Byte).Shared.Rent(BitConverter.ToInt32(b, 12) - 1)
    End Sub

    Public Sub insertdata(b, fragnum)
        System.Buffer.BlockCopy(b, 20, bigo, fragnum * mtu, b.Length - 20)

    End Sub

    Public Function returnbigo()
        Return bigo
    End Function

    Public Sub Dispose() 'Implements IDisposable.Dispose
        ArrayPool(Of Byte).Shared.Return(bigo)
        '    bigo = Nothing
        'GC.Collect()
    End Sub
End Class