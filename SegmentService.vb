Public Class SegmentService

    Public segmentsize As Integer = 10000000
    Private segmentedcount As Integer = -1
    Public tosegment As Queue(Of Byte())
    Private ff As FragFrame

    Public Function segment(b As Byte()) As List(Of Byte())

        Dim aframe = New List(Of Byte())
        Dim sequence = 0
        segmentedcount += 1
        If segmentedcount = 1000000000 Then segmentedcount = 0
        Dim aa = 0
        Dim newframe(segmentsize + 11) As Byte

        'this is frame header on every frame
        Array.Copy(BitConverter.GetBytes(sequence), 0, newframe, 4, 4)
        ' don't add total frags here that is calculated on Aframe.count
        'Array.Copy(BitConverter.GetBytes(Now().ToOADate), 0, newframe, 12, 8)


        Do While aa < b.Length - segmentsize

            Array.Copy(b, aa, newframe, 12, segmentsize)
            aa += segmentsize
            Dim addit(newframe.Length - 1) As Byte
            Array.Copy(newframe, addit, newframe.Length)
            aframe.Add(addit)
            sequence += 1
            ' fragcount += 1

        Loop
        Dim newsize = b.Length - aa
        ReDim newframe(newsize + 11)
        'Array.Copy(BitConverter.GetBytes(Now().ToOADate), 0, newframe, 12, 8)
        Array.Copy(b, aa, newframe, 12, newsize)
        aframe.Add(newframe)
        'Console.WriteLine($"Assembling frame {segmentedcount}")

        ' add total frags here at end now that we have a count
        Dim fragcount As Integer = aframe.Count
        For t = 0 To aframe.Count - 1
            Array.Copy(BitConverter.GetBytes(segmentedcount), aframe(t), 4)
            Array.Copy(BitConverter.GetBytes(t), 0, aframe(t), 4, 4)
            Array.Copy(BitConverter.GetBytes(fragcount), 0, aframe(t), 8, 4)
        Next
        ' Console.WriteLine($"data has been segmented into {aframe.Count} segments")

        Return aframe
    End Function



End Class
